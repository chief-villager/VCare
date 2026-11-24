using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VCare.SharedKernel.Abstractions;
using Xunit;

namespace VCare.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class ResidentRecordTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Admission_details_weights_attorneys_and_a_discharge_are_stored_and_read_back()
    {
        var home = await sql.Homes.GetAsync("Oak House");
        var resident = await home.RegisterResidentAsync("Bola");
        var api = home.Admin;

        Assert.Equal(HttpStatusCode.NoContent, (await api.PutAsJsonAsync($"/api/patient/{resident}/care-details", new
        {
            nhsNumber = "943 476 5919", gpName = "Dr Okafor", gpPractice = "Riverside Surgery", gpPhone = "020 7946 0000",
            room = "12", admittedOn = "2026-01-05",
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.PostAsJsonAsync($"/api/patient/{resident}/weights", new { kilograms = 61.43 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.PostAsJsonAsync($"/api/patient/{resident}/attorneys", new
        {
            name = "Tunde Ade", kind = "HealthAndWelfare", phone = "07700 900456", registeredOn = "2024-06-01",
        })).StatusCode);

        var summary = await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}");
        Assert.Equal("9434765919", summary.GetProperty("nhsNumber").GetString());
        Assert.Equal("12", summary.GetProperty("room").GetString());
        Assert.Equal(61.4m, summary.GetProperty("latestWeightKg").GetDecimal());
        Assert.Equal(0, summary.GetProperty("residency").GetInt32()); // Current
        var attorney = Assert.Single((await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}/attorneys")).EnumerateArray());
        Assert.Equal("Tunde Ade", attorney.GetProperty("name").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await api.PostAsJsonAsync($"/api/patient/{resident}/discharge", new { destination = "Home with family" })).StatusCode);
        summary = await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}");
        Assert.Equal(1, summary.GetProperty("residency").GetInt32()); // Discharged
        var leftAt = summary.GetProperty("leftAt").GetDateTime();

        // What Medications reads: the resident is away from the discharge on, for good.
        using (var scope = sql.Api.Services.CreateScope())
        {
            var absences = scope.ServiceProvider.GetRequiredService<IResidentAbsences>();
            var away = await absences.OverlappingAsync([resident], leftAt, leftAt.AddDays(30), CancellationToken.None);
            var left = Assert.Single(away);
            Assert.Equal(AbsenceKind.Discharged, left.Kind);
            Assert.Null(left.EndedAt);
        }

        Assert.Equal(HttpStatusCode.NoContent, (await api.PostAsJsonAsync($"/api/patient/{resident}/readmit", new { at = (DateTime?)null })).StatusCode);
        summary = await api.GetFromJsonAsync<JsonElement>($"/api/patient/{resident}");
        Assert.Equal(0, summary.GetProperty("residency").GetInt32());
    }

    [Fact]
    public async Task The_resident_list_leaves_out_what_only_the_audited_record_shows()
    {
        // The list is for finding a resident. Their NHS number, GP and weight are
        // read one resident at a time, and each of those reads goes in their trail.
        var home = await sql.Homes.GetAsync("Oak House");
        var resident = await home.RegisterResidentAsync("Lola");
        Assert.Equal(HttpStatusCode.NoContent, (await home.Admin.PutAsJsonAsync($"/api/patient/{resident}/care-details", new
        {
            nhsNumber = "4857773457", gpName = "Dr Okafor", gpPractice = "Riverside Surgery", gpPhone = "020 7946 0000",
            room = "7", admittedOn = "2026-02-01",
        })).StatusCode);

        var list = (await home.Admin.GetFromJsonAsync<JsonElement>("/api/patient")).EnumerateArray();

        var listed = Assert.Single(list, r => r.GetProperty("id").GetGuid() == resident);
        Assert.Equal("7", listed.GetProperty("room").GetString());
        Assert.Equal(0, listed.GetProperty("residency").GetInt32());
        Assert.False(listed.TryGetProperty("nhsNumber", out _));
        Assert.False(listed.TryGetProperty("gpName", out _));
        Assert.False(listed.TryGetProperty("latestWeightKg", out _));
    }

    [Fact]
    public async Task Two_residents_of_one_home_cannot_share_an_nhs_number()
    {
        var home = await sql.Homes.GetAsync("Oak House");
        var first = await home.RegisterResidentAsync("Kemi");
        var second = await home.RegisterResidentAsync("Funmi");
        var details = new { nhsNumber = "4010232137", gpName = (string?)null, gpPractice = (string?)null, gpPhone = (string?)null, room = (string?)null, admittedOn = (string?)null };

        var kept = await home.Admin.PutAsJsonAsync($"/api/patient/{first}/care-details", details);
        var refused = await home.Admin.PutAsJsonAsync($"/api/patient/{second}/care-details", details);

        Assert.Equal(HttpStatusCode.NoContent, kept.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal("Patients.NhsNumberInUse", (await refused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }
}
