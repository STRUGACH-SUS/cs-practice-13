using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Tests;

public class Fixture : IAsyncLifetime
{
    public Factory Api { get; } = new();

    public async Task InitializeAsync()
    {
        await using var scope = Api.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await using var scope = Api.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }
}