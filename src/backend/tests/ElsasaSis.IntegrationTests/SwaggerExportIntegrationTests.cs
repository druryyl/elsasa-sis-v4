using ElsasaSis.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace ElsasaSis.IntegrationTests;

public sealed class SwaggerExportIntegrationTests : IntegrationTestBase
{
    public SwaggerExportIntegrationTests(IntegrationTestWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ExportSwaggerJsonAsync()
    {
        var swaggerProvider = Factory.Services.GetRequiredService<ISwaggerProvider>();
        var doc = swaggerProvider.GetSwagger("v1");
        Assert.NotNull(doc);

        var client = CreateAnonymousClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        // Locate repo root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var outputPath = Path.Combine(dir.FullName, "src", "frontend", "swagger.json");
        await File.WriteAllTextAsync(outputPath, content);
    }
}
