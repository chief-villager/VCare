using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace VCare.IntegrationTests;

/// <summary>A registered care home with its admin signed in, made through the API.</summary>
public sealed record TestHome(Guid CareHomeId, Guid AdminStaffId, HttpClient Admin)
{
    internal const string Password = "Integration!Passw0rd";

    public async Task<Guid> RegisterResidentAsync(string firstname)
    {
        var response = await Admin.PostAsJsonAsync("/api/patient", new
        {
            firstname, lastname = "Test", gender = "Female", address = "Room 1", dateOfBirth = "1940-02-03",
            phoneNumber = "07000000003", email = $"{firstname.ToLowerInvariant()}@resident.test",
            emergencyContactName = "Next Of Kin", emergencyContactPhoneNumber = "07000000004",
            emergencyContactRelationship = "Son",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}

/// <summary>
/// Care homes made once per run and shared by the tests: registering is rate
/// limited per client address, and every test client has the same one.
/// </summary>
public sealed class TestHomes(SqlServerFixture sql)
{
    private readonly ConcurrentDictionary<string, Lazy<Task<TestHome>>> _homes = new();

    public Task<TestHome> GetAsync(string name) =>
        _homes.GetOrAdd(name, n => new Lazy<Task<TestHome>>(() => RegisterAsync(n))).Value;

    private async Task<TestHome> RegisterAsync(string name)
    {
        var key = new string([.. name.Where(char.IsLetterOrDigit)]).ToLowerInvariant();
        using var anonymous = sql.Api.CreateClient();
        var registered = await anonymous.PostAsJsonAsync("/api/carehome", new
        {
            careHome = new { name, address = "1 High Street", email = $"office.{key}@home.test", timeZoneId = "Europe/London" },
            admin = new
            {
                firstName = "Ada", lastName = "Admin", email = $"admin.{key}@home.test", phoneNumber = "07000000001",
                address = "1 High Street", userName = $"admin.{key}", password = TestHome.Password,
            },
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var body = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var adminId = body.GetProperty("adminStaffId").GetGuid();

        // The verification email goes nowhere in tests; confirm the address directly.
        await using (var connection = new SqlConnection(sql.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new SqlCommand("UPDATE Auth.AspNetUsers SET EmailConfirmed = 1 WHERE UserName = @user", connection);
            command.Parameters.AddWithValue("@user", $"admin.{key}");
            await command.ExecuteNonQueryAsync();
        }

        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { userName = $"admin.{key}", password = TestHome.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        var admin = sql.Api.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var careHomeId = body.TryGetProperty("careHomeId", out var id) ? id.GetGuid() : Guid.Empty;
        return new TestHome(careHomeId, adminId, admin);
    }
}
