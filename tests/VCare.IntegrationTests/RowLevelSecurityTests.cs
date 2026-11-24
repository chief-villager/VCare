using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using VCare.SharedKernel.Abstractions;
using Xunit;

namespace VCare.IntegrationTests;

// The query filters keep homes apart in the app. Row-level security does it
// again in the database, for whatever gets past them: a query that skips the
// filters, raw SQL, a bug.
[Collection(SqlServerCollection.Name)]
public class RowLevelSecurityTests(SqlServerFixture sql)
{
    private async Task<(TestHome Oak, Guid OakResident, TestHome Elm, Guid ElmResident)> TwoHomesAsync()
    {
        var oak = await sql.Homes.GetAsync("Oak House");
        var elm = await sql.Homes.GetAsync("Elm House");
        return (oak, await oak.RegisterResidentAsync("Olive"), elm, await elm.RegisterResidentAsync("Ellen"));
    }

    private async Task<SqlConnection> OpenAsAsync(Guid? careHome)
    {
        var connection = new SqlConnection(sql.ConnectionString);
        await connection.OpenAsync();
        if (careHome is { } home)
        {
            await using var set = new SqlCommand("EXEC sp_set_session_context N'CareHomeId', @home, @read_only = 1;", connection);
            set.Parameters.AddWithValue("@home", home);
            await set.ExecuteNonQueryAsync();
        }
        return connection;
    }

    private static async Task<int> CountResidentsAsync(SqlConnection connection, params Guid[] ids)
    {
        await using var count = new SqlCommand(
            $"SELECT COUNT(*) FROM patients.Patients WHERE Id IN ({string.Join(",", ids.Select((_, i) => $"@id{i}"))})", connection);
        for (var i = 0; i < ids.Length; i++)
        {
            count.Parameters.AddWithValue($"@id{i}", ids[i]);
        }
        return (int)(await count.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task A_connection_for_one_home_sees_only_that_homes_residents()
    {
        var (oak, oakResident, _, elmResident) = await TwoHomesAsync();

        await using var connection = await OpenAsAsync(oak.CareHomeId);

        Assert.Equal(1, await CountResidentsAsync(connection, oakResident, elmResident));
    }

    [Fact]
    public async Task A_connection_that_names_no_care_home_sees_nothing()
    {
        var (_, oakResident, _, elmResident) = await TwoHomesAsync();

        await using var connection = await OpenAsAsync(null);

        Assert.Equal(0, await CountResidentsAsync(connection, oakResident, elmResident));
    }

    [Fact]
    public async Task A_resident_cannot_be_moved_into_another_home()
    {
        var (oak, oakResident, elm, _) = await TwoHomesAsync();
        await using var connection = await OpenAsAsync(oak.CareHomeId);
        await using var move = new SqlCommand("UPDATE patients.Patients SET CareHomeId = @elm WHERE Id = @id", connection);
        move.Parameters.AddWithValue("@elm", elm.CareHomeId);
        move.Parameters.AddWithValue("@id", oakResident);

        var refused = await Assert.ThrowsAsync<SqlException>(() => move.ExecuteNonQueryAsync());

        Assert.Equal(33504, refused.Number); // blocked by the security policy
    }

    [Fact]
    public async Task Another_homes_resident_cannot_be_changed_even_by_id()
    {
        var (oak, _, _, elmResident) = await TwoHomesAsync();
        await using var connection = await OpenAsAsync(oak.CareHomeId);
        await using var change = new SqlCommand("UPDATE patients.Patients SET Room = N'Hijacked' WHERE Id = @id", connection);
        change.Parameters.AddWithValue("@id", elmResident);

        Assert.Equal(0, await change.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task A_reused_pooled_connection_does_not_keep_the_last_callers_home()
    {
        var (oak, oakResident, elm, elmResident) = await TwoHomesAsync();

        await using (var first = await OpenAsAsync(oak.CareHomeId))
        {
            Assert.Equal(1, await CountResidentsAsync(first, oakResident));
        }
        // Same connection string, so the pool hands back the same session, reset.
        await using var second = await OpenAsAsync(elm.CareHomeId);

        Assert.Equal(0, await CountResidentsAsync(second, oakResident));
        Assert.Equal(1, await CountResidentsAsync(second, elmResident));
    }

    [Fact]
    public async Task A_query_that_skips_the_apps_filters_still_stays_in_the_callers_home()
    {
        // The absences contract reads residents with IgnoreQueryFilters, because the
        // missed-dose job has no caller. Asked by a signed-in caller about another
        // home's resident, only the database stands in the way.
        var (oak, _, elm, elmResident) = await TwoHomesAsync();
        Assert.Equal(HttpStatusCode.NoContent,
            (await elm.Admin.PostAsJsonAsync($"/api/patient/{elmResident}/discharge", new { destination = "Home" })).StatusCode);
        var window = (From: DateTime.Now.AddDays(-2), To: DateTime.Now.AddDays(2));

        var asOak = await AbsencesAsync(Caller(oak), elmResident, window);
        var asTheJob = await AbsencesAsync(null, elmResident, window);

        Assert.Empty(asOak);
        Assert.Single(asTheJob);
    }

    [Fact]
    public async Task A_signed_in_admin_can_register_another_care_home()
    {
        // Registration is open to anyone, so a token sent with it must not pin the
        // connection to the sender's home: the new home and its admin are another's.
        var oak = await sql.Homes.GetAsync("Oak House");

        var registered = await oak.Admin.PostAsJsonAsync("/api/carehome", new
        {
            careHome = new { name = "Ash House", address = "2 High Street", email = "office.ashhouse@home.test", timeZoneId = "Europe/London" },
            admin = new
            {
                firstName = "Ash", lastName = "Admin", email = "admin.ashhouse@home.test", phoneNumber = "07000000002",
                address = "2 High Street", userName = "admin.ashhouse", password = TestHome.Password,
            },
        });

        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
    }

    private static ClaimsPrincipal Caller(TestHome home) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, home.AdminStaffId.ToString()),
            new Claim("care_home_id", home.CareHomeId.ToString()),
        ], "Test"));

    private async Task<IReadOnlyList<ResidentAbsence>> AbsencesAsync(ClaimsPrincipal? caller, Guid resident, (DateTime From, DateTime To) window)
    {
        using var scope = sql.Api.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = caller is null ? null : new DefaultHttpContext { User = caller };
        try
        {
            return await scope.ServiceProvider.GetRequiredService<IResidentAbsences>()
                .OverlappingAsync([resident], window.From, window.To, CancellationToken.None);
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }
}
