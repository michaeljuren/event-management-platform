using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.Data.Seed;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests.Infrastructure;

/// <summary>Helpers for registering, logging in and calling the API as a given user.</summary>
public static class ApiClient
{
    public const string ValidPassword = "Str0ngPassw0rd";

    public const string SeedAdminEmail = "admin@raceday.co.za";
    public const string SeedOrganizerEmail = "sarah.vdm@raceday.co.za";      // organises event 1
    public const string SeedOtherOrganizerEmail = "thabo.nkosi@raceday.co.za"; // organises event 2
    public const string SeedParticipantEmail = "lindiwe.d@example.co.za";

    public static string UniqueEmail(string prefix = "user") => $"{prefix}.{Guid.NewGuid():N}@test.co.za";

    public static object RegistrationBody(string email, string role, string password = ValidPassword, string gender = "Female",
        string? dateOfBirth = "1995-06-15") => new
    {
        firstName = "Test",
        lastName = "User",
        email,
        password,
        dateOfBirth,
        gender,
        phoneNumber = "0821234567",
        role
    };

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, object body) =>
        client.PostAsJsonAsync("/api/auth/register", body);

    public static Task<HttpResponseMessage> LoginAsync(this HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    /// <summary>Logs in and returns the token response, failing the test if login does not succeed.</summary>
    public static async Task<TokenResponse> LoginOrFailAsync(this HttpClient client, string email, string password)
    {
        var response = await client.LoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    /// <summary>A new client authenticated as a freshly registered user with the given role.</summary>
    public static async Task<(HttpClient Client, TokenResponse Tokens)> NewUserClientAsync(
        this RaceDayApiFactory factory, string role, string gender = "Female", string dateOfBirth = "1995-06-15")
    {
        var client = factory.CreateClient();
        var email = UniqueEmail(role.ToLowerInvariant());
        var register = await client.RegisterAsync(RegistrationBody(email, role, gender: gender, dateOfBirth: dateOfBirth));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var tokens = await client.LoginOrFailAsync(email, ValidPassword);
        client.UseToken(tokens.AccessToken);
        return (client, tokens);
    }

    /// <summary>A new client authenticated as one of the seeded accounts.</summary>
    public static async Task<HttpClient> SeedUserClientAsync(this RaceDayApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var tokens = await client.LoginOrFailAsync(email, SeedData.DemoPassword);
        client.UseToken(tokens.AccessToken);
        return client;
    }

    public static void UseToken(this HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>())!;
}
