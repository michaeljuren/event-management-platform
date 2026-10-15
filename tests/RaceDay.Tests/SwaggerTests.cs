using System.Net;
using System.Text.Json;
using RaceDay.Tests.Infrastructure;

namespace RaceDay.Tests;

public class SwaggerTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task SwaggerDocument_ListsEndpointsAndBearerScheme()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = doc.RootElement.GetProperty("paths");
        foreach (var path in new[]
                 {
                     "/api/auth/register", "/api/auth/login", "/api/auth/refresh-token", "/api/users/me",
                     "/api/events", "/api/events/{id}", "/api/events/{eventId}/categories",
                     "/api/categories/{categoryId}/entries", "/api/events/{eventId}/entries",
                     "/api/entries/{entryId}/result", "/api/users/me/performance-history"
                 })
        {
            Assert.True(paths.TryGetProperty(path, out _), $"Swagger document is missing {path}");
        }

        Assert.True(doc.RootElement.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task SwaggerUi_IsServed()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
