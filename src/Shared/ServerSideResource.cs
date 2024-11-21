using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace Shared;

public class ServerSideResource(IHostEnvironment environment, VersionProvider versionProvider)
    : IResourceDetector
{
    // https://opentelemetry.io/docs/specs/otel/semantic-conventions/
    public Resource Detect()
    {
        var resources = ResourceBuilder.CreateDefault()
            .AddService(Instrumentation.ServerServiceName, null,
                versionProvider.Version)
            .AddEnvironmentVariableDetector()
            .AddAttributes([
                new KeyValuePair<string, object>("dotnet.version", RuntimeInformation.FrameworkDescription),
                new KeyValuePair<string, object>("dotnet.rid", RuntimeInformation.RuntimeIdentifier),
                new KeyValuePair<string, object>("deployment.environment.name", environment.EnvironmentName),
            ])
            .Build();
        return resources;
    }
}

public class ResourceCollection(IEnumerable<IResourceDetector> resources) : IResourceDetector
{
    public Resource Detect()
    {
        return new Resource(resources.SelectMany(r => r.Detect().Attributes));
    }
}
