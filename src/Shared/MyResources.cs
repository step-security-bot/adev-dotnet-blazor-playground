using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace Shared;

public class MyResources(IHostEnvironment environment, VersionProvider versionProvider)
    : IResourceDetector
{
    // https://opentelemetry.io/docs/specs/otel/semantic-conventions/
    public Resource Detect()
    {
        var resources = ResourceBuilder.CreateDefault()
            .AddService(Environment.GetEnvironmentVariable("APPLICATION_NAME") ?? "blazor-playground", null, versionProvider.Version)
            .AddEnvironmentVariableDetector()
            .AddAttributes([
                new ("dotnet.version", RuntimeInformation.FrameworkDescription),
                new ("deployment.environment.name", environment.EnvironmentName),
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
