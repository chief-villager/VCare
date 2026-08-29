# VCare

A modular monolith on **ASP.NET Core 10 / .NET 10 (C# 14)** with **EF Core 10** and **SQL Server**.

VCare ships as one process backed by **one database**, but internally it is split into independent
modules. Each module is a domain entity's full **N-tier vertical slice** (Domain, Application,
Infrastructure, Presentation) and owns **its own `DbContext` mapped to its own schema**. Modules never
reach into each other's tables or types; the host is a thin composition root that only wires modules in.

## Layout

```
VCare/
├── VCare.slnx
├── Directory.Build.props           # shared TFM (net10.0), C# 14, nullable, warnings-as-errors
├── Directory.Packages.props        # central package versions
├── src/
│   ├── VCare.Api/                  # host / composition root
│   ├── VCare.SharedKernel/         # Entity, AggregateRoot, Result, Error, abstractions
│   └── Modules/
│       ├── Patients/               # own context -> schema "patients"
│       └── Appointments/           # own context -> schema "appointments"
└── tests/
    └── VCare.Modules.Patients.Tests/
```

Each module folder is a single project with the tiers as folders:

```
Modules/Patients/
├── Domain/            Entities, ValueObjects, Events, Errors
├── Application/       Abstractions, Commands, Queries, Dtos, Services
├── Infrastructure/    Persistence (DbContext, Configurations, Migrations), Repositories
├── Presentation/      minimal-API endpoint group
└── PatientsModule.cs  Add<Module>Module + Map<Module>Endpoints
```

## One database, one context per module

- All contexts read `ConnectionStrings:Default` — a single SQL Server database.
- Each context calls `HasDefaultSchema("<module>")`, so tables live under `patients.*`, `appointments.*`.
- Each context has its own migrations history table (`<schema>.__EFMigrationsHistory`), so modules
  migrate independently and never collide.

## Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, a container, or a full instance). Update `ConnectionStrings:Default` in
  `src/VCare.Api/appsettings.json`.

## Build and run

```bash
dotnet restore
dotnet build
dotnet run --project src/VCare.Api
```

OpenAPI (Development only): `https://localhost:7443/openapi/v1.json`.

## Migrations (one ledger per module)

The EF tools live in the host (`VCare.Api`), but migrations belong to each module project. Always
target the context explicitly:

```bash
# Patients
dotnet ef migrations add InitialCreate \
  --project src/Modules/Patients/VCare.Modules.Patients.csproj \
  --startup-project src/VCare.Api/VCare.Api.csproj \
  --context PatientsDbContext \
  --output-dir Infrastructure/Persistence/Migrations

dotnet ef database update \
  --project src/Modules/Patients/VCare.Modules.Patients.csproj \
  --startup-project src/VCare.Api/VCare.Api.csproj \
  --context PatientsDbContext

# Appointments (same commands, swap the project + context)
dotnet ef migrations add InitialCreate \
  --project src/Modules/Appointments/VCare.Modules.Appointments.csproj \
  --startup-project src/VCare.Api/VCare.Api.csproj \
  --context AppointmentsDbContext \
  --output-dir Infrastructure/Persistence/Migrations

dotnet ef database update \
  --project src/Modules/Appointments/VCare.Modules.Appointments.csproj \
  --startup-project src/VCare.Api/VCare.Api.csproj \
  --context AppointmentsDbContext
```

Install the tool once if needed: `dotnet tool install --global dotnet-ef`.

## Adding a new module

1. `dotnet new classlib -n VCare.Modules.<Plural> -o src/Modules/<Plural>` and recreate the tier folders.
2. Reference `VCare.SharedKernel`, add `FrameworkReference Microsoft.AspNetCore.App` and the EF Core
   SqlServer package.
3. Write the aggregate (private setters, factory + behaviour methods), the `<Plural>DbContext` with
   `Schema` + `HasDefaultSchema`, an `IEntityTypeConfiguration<T>`, the repository, the service, the
   endpoints, and the `<Plural>Module` extension class.
4. Reference the module from `VCare.Api` and add two lines in `Program.cs`:
   `.Add<Plural>Module(builder.Configuration)` and `app.Map<Plural>Endpoints();`.
5. Add the module's first migration (commands above).

## Endpoints included

- `POST /api/patients` / `GET /api/patients/{id}`
- `POST /api/appointments` / `GET /api/appointments/{id}`

## Notes

- Package versions are pinned to `10.0.0` in `Directory.Packages.props`; bump to the latest 10.0.x patch.
- Test package versions (xUnit, test SDK) reflect recent releases; update to the newest compatible ones.
