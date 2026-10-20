using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Integration;

public class ErrorHandlingTests
{
    [Fact]
    public async Task UnhandledException_ReturnsProblemDetailsWithoutLeakingInternals()
    {
        // Nothing listens on port 1, so the first database call throws.
        await using var factory = new TodoApiFactory(
            "Server=tcp:127.0.0.1,1;Database=Unreachable;Integrated Security=True;Connect Timeout=1;Encrypt=False");
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = "someone@test.local", password = "Passw0rd!" }, ApiClient.Json);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, (await response.ReadAsync<ProblemDetails>()).Status);
        Assert.DoesNotContain("SqlException", body);
        Assert.DoesNotContain("127.0.0.1", body);
    }
}
