using McpDemo.Api.Data;
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

var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
var otlpEndpoint = otelConfig["OtlpEndpoint"];

Action<OtlpExporterOptions>? configureOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint)
    ? opts =>
    {
        opts.Endpoint = new Uri(otlpEndpoint);
        opts.Protocol = OtlpExportProtocol.HttpProtobuf;
    }
    : null;

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

        if (configureOtlp is not null)
            tracing.AddOtlpExporter(configureOtlp);

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

        if (configureOtlp is not null)
            metrics.AddOtlpExporter(configureOtlp);

        if (builder.Environment.IsDevelopment())
            metrics.AddConsoleExporter();
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;

    if (configureOtlp is not null)
        logging.AddOtlpExporter(configureOtlp);

    if (builder.Environment.IsDevelopment())
        logging.AddConsoleExporter();
});

var app = builder.Build();

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
