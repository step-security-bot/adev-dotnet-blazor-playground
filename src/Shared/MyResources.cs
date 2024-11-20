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
            .AddService(Environment.GetEnvironmentVariable("APPLICATION_NAME") ?? "blazor-playground", null,
                versionProvider.Version)
            .AddEnvironmentVariableDetector()
            .AddAttributes([
                new KeyValuePair<string, object>("dotnet.version", RuntimeInformation.FrameworkDescription),
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
        return ResourceBuilder.CreateEmpty()
            .AddAttributes(resources.SelectMany(r => r.Detect().Attributes))
            .Build();
    }
}
