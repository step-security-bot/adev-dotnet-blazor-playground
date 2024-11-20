using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Serilog;
using Serilog.Debugging;
using Serilog.Exceptions;

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
    builder.Logging.ClearProviders();
    var configurationBuildTime = builder.Configuration;
    builder.Services.AddSerilog((sp, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(configurationBuildTime)
            // .WriteTo.Async(c => c.OpenTelemetry(openTelemetrySinkOptions =>
            //     openTelemetrySinkOptions.ResourceAttributes =
            //         new Dictionary<string, object>(
            //             sp.GetRequiredService<ResourceCollection>().Detect().Attributes
            //         )
            // ))
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
