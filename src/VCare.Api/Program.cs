using Patients.Presenstation;
using src.Modules.CareHomes.Infrastructure.Configuration;
using src.Modules.CareHomes.Presentation;
using Staffs.Domain.Entity;
using Staffs.Presentation;
using Staffs.Infrastructure;
using VCare.Api.Endpoints;
using VCare.Api.Services;
using VCare.Modules.CarePlans;
using VCare.Modules.Patients;
using VCare.Modules.Visitation;
using Visitation.Presentation;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;

var builder = WebApplication.CreateBuilder(args);

// Built-in OpenAPI document generation (ASP.NET Core 10).
builder.Services.AddOpenApi();
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddOptions<JwtSettings>().Bind(jwt).
            ValidateOnStart().ValidateDataAnnotations();
var hosting = builder.Configuration.GetSection("Hosting");
builder.Services.AddOptions<Hosting>().Bind(hosting);

// The caller's identity + care home, resolved from the JWT per request.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Each module registers its own DbContext, repositories and services.
// The host stays a thin composition root: it knows modules exist, nothing more.
builder.Services.AddPatientsModule(builder.Configuration);
builder.Services.AddCarePlansModule(builder.Configuration);
builder.Services.AddStaffModule(builder.Configuration);
builder.Services.AddCareHomeModule(builder.Configuration);
builder.Services.AddVisitationModule(builder.Configuration);

// Composes the patient and its care plan from the two modules that own them.
builder.Services.AddScoped<DashboardService>();

// Creates a care home and its first admin together, across CareHomes and Staffs.
builder.Services.AddScoped<CareHomeRegistrationService>();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Each module maps its own endpoint group.
app.AddPatientEndpoints();
app.AddStaffEndpoints();
app.AddAuthEndpoints();
app.AddVisitationEndpoints();
app.AddCareHomeEndpoint();
app.AddCareHomeRegistrationEndpoint();

app.Run();
