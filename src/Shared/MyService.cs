using Microsoft.Extensions.Logging;

namespace Shared;

public interface IMyService
{
    Task<string?> GetData(CancellationToken cancellationToken);
}

public class ServerSideService(ILogger<ServerSideService> logger) : IMyService
{
    public async Task<string?> GetData(CancellationToken cancellationToken)
    {
        using var activity = ServerInstrumentation.ActivitySource.StartActivity();
        logger.LogInformation("Hello from ServerSideService");
        await Task.Delay(1000, cancellationToken);
        return "Hello from MyService";
    }
}

public class ClientSideService(HttpClient httpClient, ILogger<ClientSideService> logger) : IMyService
{
    public async Task<string?> GetData(CancellationToken cancellationToken)
    {
        using var activity = ClientInstrumentation.ActivitySource.StartActivity();
        logger.LogInformation("Hello from ClientSideService");
        return await httpClient.GetStringAsync("api/data", cancellationToken);
    }
}
