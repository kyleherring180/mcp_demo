using McpDemo.Api.Data;
using McpDemo.Api.Services;
using McpDemo.Api.Telemetry;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "MCP Demo - Product Catalog API", Version = "v1" });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<ProductMetrics>();
builder.Services.AddScoped<ProductReportService>();

var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
var otlpEndpoint = otelConfig["OtlpEndpoint"];

// Separate exporter actions with explicit signal paths
Action<OtlpExporterOptions>? configureOtlpTraces = !string.IsNullOrWhiteSpace(otlpEndpoint)
    ? opts =>
    {
        opts.Endpoint = new Uri($"{otlpEndpoint}/v1/traces");
        opts.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
    : null;

Action<OtlpExporterOptions>? configureOtlpMetrics = !string.IsNullOrWhiteSpace(otlpEndpoint)
    ? opts =>
    {
        opts.Endpoint = new Uri($"{otlpEndpoint}/v1/metrics");
        opts.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
    : null;

Action<OtlpExporterOptions>? configureOtlpLogs = !string.IsNullOrWhiteSpace(otlpEndpoint)
    ? opts =>
    {
        opts.Endpoint = new Uri($"{otlpEndpoint}/v1/logs");
        opts.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
    : null;

// Expose OTel SDK internal errors in logs
builder.Logging.AddFilter("OpenTelemetry", LogLevel.Debug);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: otelConfig["ServiceName"] ?? "McpDemo.Api",
        serviceVersion: "1.0.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(opts => opts.RecordException = true)
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        if (configureOtlpTraces is not null)
            tracing.AddOtlpExporter(configureOtlpTraces);

        if (builder.Environment.IsDevelopment())
            tracing.AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(ProductMetrics.MeterName);

        if (configureOtlpMetrics is not null)
            metrics.AddOtlpExporter(configureOtlpMetrics);

        if (builder.Environment.IsDevelopment())
            metrics.AddConsoleExporter();
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;

    if (configureOtlpLogs is not null)
        logging.AddOtlpExporter(configureOtlpLogs);

    if (builder.Environment.IsDevelopment())
        logging.AddConsoleExporter();
});

var app = builder.Build();

// Confirm OTel endpoint at startup
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation(">>> OTel endpoint resolved as: [{Endpoint}]",
    app.Configuration["OpenTelemetry:OtlpEndpoint"]);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API v1");
        options.RoutePrefix = "swagger";
    });

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();