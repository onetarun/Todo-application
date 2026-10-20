using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TodoApi.Dtos.Auth;

namespace TodoApi.Tests.Infrastructure;

public static class ApiClient
{
    // Matches the API: camelCase properties, enums as names.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string UniqueEmail() => $"{Guid.NewGuid():N}@test.local";

    public static async Task<AuthResponse> RegisterAsync(HttpClient client, string? email = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { name = "Test User", email = email ?? UniqueEmail(), password = "Passw0rd!" },
            Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }

    // A client signed in as a brand-new user, so each test owns its data.
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this TodoApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;
}
