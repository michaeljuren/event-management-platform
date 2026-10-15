using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

/// <summary>Both roles viewing and updating their own profile.</summary>
public class ProfileTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Theory]
    [InlineData("Organizer")]
    [InlineData("Participant")]
    public async Task User_CanViewOwnProfile(string role)
    {
        var (client, tokens) = await factory.NewUserClientAsync(role);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.ReadAsync<UserProfileResponse>();
        Assert.Equal(tokens.User.Email, profile.Email);
        Assert.Contains(role, profile.Roles);
    }

    [Theory]
    [InlineData("Organizer")]
    [InlineData("Participant")]
    public async Task User_CanUpdateOwnProfile(string role)
    {
        var (client, _) = await factory.NewUserClientAsync(role);

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Name",
            phoneNumber = "0831112222",
            clubId = 2,
            emergencyContactName = "Next Of Kin",
            emergencyContactPhone = "0839998888"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.ReadAsync<UserProfileResponse>();
        Assert.Equal("Updated", profile.FirstName);
        Assert.Equal("Randburg Harriers", profile.ClubName);

        var reloaded = await (await client.GetAsync("/api/users/me")).ReadAsync<UserProfileResponse>();
        Assert.Equal("Next Of Kin", reloaded.EmergencyContactName);
    }

    [Fact]
    public async Task UpdateProfile_WithUnknownClubOrMissingName_Returns400()
    {
        var (client, _) = await factory.NewUserClientAsync("Participant");

        var unknownClub = await client.PutAsJsonAsync("/api/users/me", new { firstName = "A", lastName = "B", clubId = 9999 });
        var missingName = await client.PutAsJsonAsync("/api/users/me", new { lastName = "B" });

        Assert.Equal(HttpStatusCode.BadRequest, unknownClub.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingName.StatusCode);
    }
}
