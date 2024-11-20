using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Debugging;
using Serilog.Exceptions;
using Shared;
using Web.Components;
using _Imports = Web.Client._Imports;

// Startup logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .WriteTo.Console()
    .Enrich.WithProperty("AppSource", "ASP.NET")
    .CreateBootstrapLogger();

WebApplication app;
try
{
    var builder = WebApplication.CreateBuilder(args);
    if (builder.Environment.IsDevelopment())
    {
        SelfLog.Enable(Console.Error);
    }

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddInteractiveWebAssemblyComponents();


    // add all resource detectors as IResourceDetector interface
    builder.Services.AddSingleton<IResourceDetector, MyResources>();
    // inject the resource detectors into the resource collection at one go
    builder.Services.AddSingleton<ResourceCollection>();
    builder.Services.AddSingleton<VersionProvider>();
    builder.Logging.ClearProviders();
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r
            // get all resources in one step
            .AddDetector(s => s.GetRequiredService<ResourceCollection>())
        )
        .WithMetrics(metrics => metrics
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter()
        )
        .WithTracing(tracing => tracing
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter()
        );

    var configurationBuildTime = builder.Configuration;
    builder.Services.AddSerilog((sp, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(configurationBuildTime)
            .WriteTo.Async(c => c.OpenTelemetry(openTelemetrySinkOptions =>
                openTelemetrySinkOptions.ResourceAttributes =
                    new Dictionary<string, object>(
                        sp.GetRequiredService<ResourceCollection>().Detect().Attributes
                    )
            ))
            .Enrich.WithExceptionDetails()
            ;
    });

    app = builder.Build();
}
catch (Exception e)
{
    Log.Fatal(e, "Startup failed");
    throw;
}

var logger = app.Logger;

try
{
    app.UseSerilogRequestLogging();
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseWebAssemblyDebugging();
    }
    else
    {
        app.UseExceptionHandler("/Error", true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();


    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(_Imports).Assembly);

    app.Run();
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
