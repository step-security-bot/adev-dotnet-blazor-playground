using System.Diagnostics;

namespace Shared;

public static class Instrumentation
{
    public const string ServerServiceName = "blazor.playground.backend";
    public const string ClientServiceName = "blazor.playground.frontend";
}

public static class ServerInstrumentation
{
    public static ActivitySource ActivitySource { get; } = new (Instrumentation.ServerServiceName);
}

public static class ClientInstrumentation
{
    public static ActivitySource ActivitySource { get; } = new (Instrumentation.ClientServiceName);
}
