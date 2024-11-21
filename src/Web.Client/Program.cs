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
        // .AddOtlpExporter(x =>
        // {
        //     x.Endpoint = new Uri("http://localhost:4318/v1/traces");
        //     x.Protocol = OtlpExportProtocol.HttpProtobuf;
        //     x.HttpClientFactory = () => new HttpClient();
        // })
        .AddConsoleExporter()
        .Build()
    );
    builder.Services.AddSingleton<MeterProvider>(sp => Sdk.CreateMeterProviderBuilder()
        .ConfigureResource(r => r.AddDetector(sp.GetRequiredService<ResourceCollection>()))
        .AddMeter(ClientInstrumentation.ActivitySource.Name)
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        // .AddOtlpExporter(x =>
        // {
        //     x.Endpoint = new Uri("http://localhost:4318/v1/traces");
        //     x.Protocol = OtlpExportProtocol.HttpProtobuf;
        //     x.HttpClientFactory = () => new HttpClient();
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
            // .WriteTo.OpenTelemetry(openTelemetrySinkOptions =>
            // {
            //     openTelemetrySinkOptions.ResourceAttributes =
            //         new Dictionary<string, object>(
            //             sp.GetRequiredService<ResourceCollection>().Detect().Attributes
            //         );
            //     openTelemetrySinkOptions.OnBeginSuppressInstrumentation = OpenTelemetry.SuppressInstrumentationScope.Begin;
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
