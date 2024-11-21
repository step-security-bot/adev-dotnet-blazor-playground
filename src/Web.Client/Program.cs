using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Debugging;
using Serilog.Exceptions;
using Shared;
using Web.Client.Otel;

// Startup logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .WriteTo.BrowserConsole()
    .Enrich.WithProperty("AppSource", "WebAssembly")
    .CreateBootstrapLogger();
WebAssemblyHost app;
try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    if (builder.HostEnvironment.IsDevelopment())
    {
        SelfLog.Enable(Console.Error);
    }

    Log.Warning(
        "Building WebAssembly. This is only a warning because I am testing the bootstrap logger before .Net reads the appsettings.json for Serilog");

    builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
        .AddScoped<IMyService, ClientSideService>()
        .AddSingleton<VersionProvider>();

    builder.Services.AddSingleton<IResourceDetector, ClientSideResource>();
    builder.Services.AddSingleton<ResourceCollection>();

    builder.Services.AddSingleton<TracerProvider>(sp => Sdk.CreateTracerProviderBuilder()
        .ConfigureResource(r => r.AddDetector(sp.GetRequiredService<ResourceCollection>()))
        .AddSource(ClientInstrumentation.ActivitySource.Name)
        .AddHttpClientInstrumentation()
        //      .AddOtlpExporter(x =>
        //      {
        //          x.Endpoint = new Uri("http://localhost:4318/v1/traces");
        //          // GRPC fails because of this https://github.com/open-telemetry/opentelemetry-dotnet/issues/5083
        //          // HTTP fails because of the lack of multi threading
        //          /* System.PlatformNotSupportedException: Cannot wait on monitors on this runtime.
        // at System.Threading.Monitor.ObjWait(Int32 millisecondsTimeout, Object obj)
        // at System.Threading.Monitor.Wait(Object obj, Int32 millisecondsTimeout)
        // at System.Threading.ManualResetEventSlim.Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
        // at System.Threading.Tasks.Task.SpinThenBlockingWait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
        // at System.Threading.Tasks.Task.InternalWaitCore(Int32 millisecondsTimeout, CancellationToken cancellationToken)
        // at System.Threading.Tasks.Task.InternalWait(Int32 millisecondsTimeout, CancellationToken cancellationToken)
        // at OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient.BaseOtlpHttpExportClient`1[[OpenTelemetry.Proto.Collector.Trace.V1.ExportTraceServiceRequest, OpenTelemetry.Exporter.OpenTelemetryProtocol, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7bd6737fe5b67e3c]].SendHttpRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        // at OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.ExportClient.BaseOtlpHttpExportClient`1[[OpenTelemetry.Proto.Collector.Trace.V1.ExportTraceServiceRequest, OpenTelemetry.Exporter.OpenTelemetryProtocol, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7bd6737fe5b67e3c]].SendExportRequest(ExportTraceServiceRequest request, DateTime deadlineUtc, CancellationToken cancellationToken)
        // at OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation.Transmission.OtlpExporterTransmissionHandler`1[[OpenTelemetry.Proto.Collector.Trace.V1.ExportTraceServiceRequest, OpenTelemetry.Exporter.OpenTelemetryProtocol, Version=1.0.0.0, Culture=neutral, PublicKeyToken=7bd6737fe5b67e3c]].TrySubmitRequest(ExportTraceServiceRequest request)
        // */
        //          x.Protocol = OtlpExportProtocol.HttpProtobuf;
        //          x.HttpClientFactory = () => new HttpClient(new HttpClientHandler());
        //          x.ExportProcessorType = ExportProcessorType.Simple;
        //      })
        .AddConsoleExporter()
        .Build()
    );
    builder.Services.AddSingleton<MeterProvider>(sp => Sdk.CreateMeterProviderBuilder()
        .ConfigureResource(r => r.AddDetector(sp.GetRequiredService<ResourceCollection>()))
        .AddMeter(ClientInstrumentation.ActivitySource.Name)
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        /*  System.PlatformNotSupportedException: Operation is not supported on this platform.
   at System.Threading.Thread.ThrowIfNoThreadStart(Boolean internalThread)
   at System.Threading.Thread.Start(Boolean captureContext, Boolean internalThread)
   at System.Threading.Thread.Start() */
        // .AddOtlpExporter(x =>
        // {
        //     x.Endpoint = new Uri("http://localhost:4318/v1/metrics");
        //     x.Protocol = OtlpExportProtocol.HttpProtobuf;
        //     x.HttpClientFactory = () => new HttpClient(new HttpClientHandler());
        //     x.ExportProcessorType = ExportProcessorType.Simple;
        // })
        // This is also a periodic exporter, so it breaks
        // .AddConsoleExporter()
        .Build()
    );


    builder.Logging.ClearProviders();
    var configurationBuildTime = builder.Configuration;
    builder.Services.AddSerilog((sp, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(configurationBuildTime)
            // this also fails, probably because of the batching
            // .WriteTo.OpenTelemetry(openTelemetrySinkOptions =>
            // {
            //     openTelemetrySinkOptions.ResourceAttributes =
            //         new Dictionary<string, object>(
            //             sp.GetRequiredService<ResourceCollection>().Detect().Attributes
            //         );
            //     openTelemetrySinkOptions.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
            //     // openTelemetrySinkOptions.BatchingOptions = null;
            // })
            .Enrich.WithExceptionDetails()
            .WriteTo.Logger(l =>
                l.Filter.ByExcluding("SourceContext = 'Microsoft.Hosting.Lifetime'").WriteTo
                    // https://github.com/serilog/serilog-sinks-browserconsole/issues/20
                    .BrowserConsole(jsRuntime: sp.GetRequiredService<IJSRuntime>()))
            ;
    });
    app = builder.Build();
}
catch (Exception e)
{
    Log.Fatal(e, "Startup failed in WebAssembly");
    await Log.CloseAndFlushAsync();
    throw;
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var tracer = app.Services.GetRequiredService<TracerProvider>();
    var meter = app.Services.GetRequiredService<MeterProvider>();
    logger.LogInformation("Starting WebAssembly application");
    await app.RunAsync();
}
catch (Exception e)
{
    logger.LogCritical(e, "Application has crashed");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
