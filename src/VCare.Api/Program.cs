using Patients.Presenstation;
using src.Modules.CareHomes.Infrastructure.Configuration;
using src.Modules.CareHomes.Presentation;
using Staffs.Domain.Entity;
using Staffs.Presentation;
using Staffs.Infrastructure;
using VCare.Api.Endpoints;
using VCare.Api.Services;
using Medications;
using Medications.Presentation;
using VCare.Modules.CarePlans;
using VCare.Modules.Patients;
using VCare.Modules.Visitation;
using Visitation.Presentation;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Services;
using Notifications;
using Scalar.AspNetCore;
using CarePlans.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Amazon;
using Amazon.S3;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using VCare.Api.Health;
using VCare.Modules.Audit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddValidation();

// One JSON object per log line outside Development, so a log store can search by
// field (care home, status code, elapsed time) instead of by free text.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

// An unhandled exception becomes a plain problem+json 500. Without this the
// default response in Production is an empty 500, and in Development a stack trace.
builder.Services.AddProblemDetails();

// Traces: a span for each request, each outgoing HTTP call (S3) and each SQL
// command, plus VCare's own (VCare.*, such as the missed-dose job), so one slow or
// failing request can be followed end to end. Spans leave the process only when
// OTEL_EXPORTER_OTLP_ENDPOINT is set; OTEL_SERVICE_NAME names the service.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "vcare-api"))
    .WithTracing(tracing =>
    {
        tracing.AddSource("VCare.*")
            // A load balancer calls the health checks every few seconds.
            .AddAspNetCoreInstrumentation(o => o.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            tracing.AddOtlpExporter();
        }
    });

// Live: the process is up. Ready: it can reach the database. Both are anonymous
// so a load balancer or orchestrator can call them.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// Built-in OpenAPI document generation (ASP.NET Core 10).
builder.Services.AddOpenApi();
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddOptions<JwtSettings>().Bind(jwt).
            ValidateOnStart().ValidateDataAnnotations();
var hosting = builder.Configuration.GetSection("Hosting");
builder.Services.AddOptions<Hosting>().Bind(hosting).
            ValidateOnStart().ValidateDataAnnotations();

// The caller's identity + care home, resolved from the JWT per request.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Walks an aggregate's raised events to their handlers. Scoped, so the
// handlers it resolves share the request's DbContexts.
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
// One S3 client for the app's lifetime. It is thread-safe and keeps the connection
// pool and credentials; the container disposes it on shutdown.
builder.Services.AddSingleton<IAmazonS3>(sp =>
    new AmazonS3Client(RegionEndpoint.GetBySystemName(
        sp.GetRequiredService<IOptions<StorageEntity>>().Value.Region)));
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Background jobs take a database lock per pass, so with several instances of
// the API only one does each pass.
builder.Services.AddSingleton<IJobLock>(sp =>
    new SqlServerJobLock(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!));

// Each module registers its own DbContext, repositories and services.
// The host stays a thin composition root: it knows modules exist, nothing more.
// Audit first: it registers the interceptor every module's DbContext adds.
builder.Services.AddAuditModule(builder.Configuration);
builder.Services.AddPatientsModule(builder.Configuration);
builder.Services.AddCarePlansModule(builder.Configuration);
builder.Services.AddMedicationsModule(builder.Configuration);
builder.Services.AddStaffModule(builder.Configuration);
builder.Services.AddCareHomeModule(builder.Configuration);
builder.Services.AddVisitationModule(builder.Configuration);

builder.Services.AddOptions<StorageEntity>().Bind(builder.Configuration.GetSection("Storage")).ValidateDataAnnotations();

// Notifications sends email directly over SMTP. A failed send is logged and the
// staff member asks for the link again; there is no queue.
builder.Services.AddNotificationModule(builder.Configuration);

// Creates a care home and its first admin together, across CareHomes and Staffs.
builder.Services.AddScoped<CareHomeRegistrationService>();
var requireAuthPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(requireAuthPolicy)
    .AddPolicy("Admin_Role", p => p.Combine(requireAuthPolicy).RequireRole("Admin"))
    .AddPolicy("Carer_Role", p => p.Combine(requireAuthPolicy).RequireRole("Carer"))
    .AddPolicy("Admin_Or_Carer", policy => policy.RequireRole("Admin", "Carer"));
    
var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();

// Browsers must take the declared content type at its word, not guess from the
// bytes: a stored file is never sniffed into something that runs.
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// The sign-in and email-sending routes carry their own limits (see AuthRateLimits).
app.UseRateLimiter();

// Each successful read of a resident's record goes in the audit trail.
app.UseResidentRecordAudit();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

// Each module maps its own endpoint group.
app.AddPatientEndpoints();
app.AddStaffEndpoints();
app.AddAuthEndpoints();
app.AddVisitationEndpoints();
app.AddMedicationEndpoint();
app.AddMarchartEndpoint();
app.AddCareHomeEndpoint();
app.AddCarePlanEndpoint();
app.AddCareHomeRegistrationEndpoint();
app.AddAuditEndpoints();

// The fallback policy requires a signed-in caller everywhere, which also covered
// the API description: a developer had to have a token to read how to get one.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.Run();
