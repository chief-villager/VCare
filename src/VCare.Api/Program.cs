using VCare.Modules.CarePlans;
using VCare.Modules.Patients;

var builder = WebApplication.CreateBuilder(args);

// Built-in OpenAPI document generation (ASP.NET Core 10).
builder.Services.AddOpenApi();

// Each module registers its own DbContext, repositories and services.
// The host stays a thin composition root: it knows modules exist, nothing more.
builder.Services
    .AddPatientsModule(builder.Configuration)
    .AddCarePlansModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Each module maps its own endpoint group.
app.MapPatientsEndpoints();

app.Run();
