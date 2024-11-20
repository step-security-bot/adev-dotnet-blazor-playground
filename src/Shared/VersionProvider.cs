using System.Reflection;

namespace Shared;

public class VersionProvider
{
    public string Version { get; }

    public VersionProvider()
    {
        var assembly = Assembly.GetExecutingAssembly();
        Version = assembly
                      .GetCustomAttribute<
                          AssemblyInformationalVersionAttribute>()
                      ?.InformationalVersion ??
                  assembly.GetName().Version?.ToString() ??
                  "0.0.0-missing-version";
    }
}
