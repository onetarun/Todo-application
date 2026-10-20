using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoApi.Tests.Infrastructure;

// "Testing" environment: no development seed data and no user secrets, so every setting the app needs is supplied here.
public class TodoApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TodoDb", connectionString);
        builder.UseSetting("Jwt:SigningKey", ApiFixture.TestSigningKey);
    }
}
