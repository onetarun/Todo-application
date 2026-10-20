using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Dtos.Auth;
using TodoApi.Tests.Infrastructure;

namespace TodoApi.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class AuthEndpointsTests(ApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    private Task<HttpResponseMessage> PostAsync(string path, object body) =>
        _client.PostAsJsonAsync(path, body, ApiClient.Json);

    [Fact]
    public async Task Register_ReturnsCreatedWithTokensAndLocation()
    {
        var response = await PostAsync(
            "/api/auth/register", new { name = "New User", email = ApiClient.UniqueEmail(), password = "Passw0rd!" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/auth/me", response.Headers.Location?.AbsolutePath);
        var body = await response.ReadAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = ApiClient.UniqueEmail();
        await ApiClient.RegisterAsync(_client, email);

        var response = await PostAsync(
            "/api/auth/register", new { name = "Again", email = email.ToUpperInvariant(), password = "Passw0rd!" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Register_ConcurrentDuplicates_OneCreatedRestConflict()
    {
        var email = ApiClient.UniqueEmail();
        var attempts = Enumerable.Range(0, 8).Select(_ => fixture.Factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new { name = "Racer", email, password = "Passw0rd!" }, ApiClient.Json));

        var statuses = (await Task.WhenAll(attempts)).Select(r => r.StatusCode).ToList();

        Assert.Single(statuses, HttpStatusCode.Created);
        Assert.All(statuses.Where(s => s != HttpStatusCode.Created), s => Assert.Equal(HttpStatusCode.Conflict, s));
    }

    [Fact]
    public async Task Register_InvalidInput_ReturnsValidationProblem()
    {
        var response = await PostAsync("/api/auth/register", new { name = "", email = "not-an-email", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("Name", problem.Errors.Keys);
        Assert.Contains("Email", problem.Errors.Keys);
        Assert.Contains("Password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorizedProblem()
    {
        var registered = await ApiClient.RegisterAsync(_client);

        var response = await PostAsync("/api/auth/login", new { email = registered.User.Email, password = "wrong-pass" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid email or password.", (await response.ReadAsync<ProblemDetails>()).Detail);
    }

    [Fact]
    public async Task Me_RequiresValidToken()
    {
        var registered = await ApiClient.RegisterAsync(_client);

        var anonymous = await _client.GetAsync("/api/auth/me");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);
        var authenticated = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
        Assert.Equal(registered.User, await authenticated.ReadAsync<UserDto>());
    }

    [Fact]
    public async Task RefreshAndLogout_Flow()
    {
        var registered = await ApiClient.RegisterAsync(_client);

        var refresh = await PostAsync("/api/auth/refresh", new { refreshToken = registered.RefreshToken });
        var rotated = await refresh.ReadAsync<AuthResponse>();
        var logout = await PostAsync("/api/auth/logout", new { refreshToken = rotated.RefreshToken });
        var afterLogout = await PostAsync("/api/auth/refresh", new { refreshToken = rotated.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
