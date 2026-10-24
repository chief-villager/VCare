# VCare

VCare is a backend API for running a care home: the residents, the staff who look after them, and
the day-to-day record of the care they receive.

## The idea

Care homes still run much of their day on paper: care plans in binders, medication charts on
clipboards, visit notes in a logbook. That makes it hard to see at a glance what a resident needs,
what has been done for them today, and what has been missed. VCare puts all of that into one system
that every carer records against as they work.

A care home signs up and gets an admin account. From there it can:

- **Manage residents (patients):** profiles, photos and documents such as consent forms or hospital
  letters.
- **Manage staff:** carers and admins, each with their own login, profile and documents. New staff
  confirm their email before they can sign in.
- **Keep a care plan for each resident:** their diagnoses, and for each one the goal and the
  intervention the staff should carry out.
- **Run medication rounds:** record each prescription with its dose and schedule (times of day, days
  of the week, every N days, or as needed). VCare works out which doses are due today and which are
  still outstanding. Staff sign each dose off as taken or refused. A dose not signed off within an
  hour of its slot is recorded as missed. The result is a MAR chart (Medication Administration
  Record) for each resident.
- **Schedule and record care visits:** book visits for a resident and assign staff to them. Each
  assigned carer checks in when they arrive and checks out when they leave, recording whether
  medication, personal care and feeding were done, with a note for each.

Every care home only ever sees its own data. The care home and the staff member making a change
come from their login, never from the request, so one home cannot read or write another's records.

## Status

VCare is in active development and has no front end yet. It is exercised through the Postman
collection in [`postman/`](postman/).

## How it is built

VCare is a modular monolith on **ASP.NET Core 10 / .NET 10 (C# 14)** with **EF Core 10** and
**SQL Server**.

It ships as one process backed by **one database**, but internally it is split into independent
modules. Each module is a full vertical slice (Domain, Application, Infrastructure, Presentation)
and owns **its own `DbContext` mapped to its own schema**. Modules never reach into each other's
tables or types. The host is a thin composition root that wires the modules together.

| Module | What it owns |
|---|---|
| CareHomes | Care home registration and details |
| Staffs | Staff accounts, roles (Carer, Admin), login, email confirmation, password reset, refresh tokens |
| Patients | Resident profiles |
| CarePlans | Care plans, diagnoses, goals and interventions |
| Medications | Medication orders, dose schedules, administrations, outcome codes, the MAR chart |
| Visitation | Scheduled visits, staff assignment, check-in and check-out records |
| Notifications | Sending email over SMTP |
| Outbox | Messages written in the same transaction as the change that caused them, then delivered in the background, so an email is never lost or sent for a change that rolled back |

Documents and profile images are stored in Amazon S3.

### Layout

```
VCare/
├── VCare.slnx
├── Directory.Build.props           # shared TFM (net10.0), C# 14, nullable, warnings-as-errors
├── Directory.Packages.props        # central package versions
├── src/
│   ├── VCare.Api/                  # host / composition root, auth, care home registration
│   ├── VCare.SharedKernel/         # Entity, AggregateRoot, Result, Error, typed ids
│   └── Modules/
│       ├── CareHomes/
│       ├── CarePlans/
│       ├── Medications/
│       ├── Notifications/
│       ├── Outbox/
│       ├── Patients/
│       ├── Staffs/
│       └── Visitation/
├── tests/
│   ├── VCare.Modules.CarePlans.Tests/
│   └── VCare.Modules.Patients.Tests/
└── postman/                        # API collection and OpenAPI spec
```

Each module is a single project with the tiers as folders:

```
Modules/<Module>/
├── Domain/            Entities, enums, events, errors
├── Application/       Abstractions, DTOs, services
├── Infrastructure/    DbContext, configurations, repositories
├── Migrations/        This module's EF migrations
├── Presentation/      Minimal-API endpoint group
└── <Module>Module.cs  Service registration for the module
```

### One database, one context per module

- All contexts read `ConnectionStrings:Default`, a single SQL Server database.
- Each context sets its own default schema, so each module's tables live under their own schema.
- Each context has its own migrations history table (`<schema>.__EFMigrationsHistory`), so modules
  migrate independently and never collide.

## Running it

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, a container, or a full instance)
- An SMTP server for email. [MailHog](https://github.com/mailhog/MailHog) works for local
  development.
- AWS credentials and an S3 bucket, for documents and profile images

### Configuration

`src/VCare.Api/appsettings.json` is not committed, because it holds secrets. Create it with these
keys:

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | SQL Server connection string |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:DurationInMinutes` | Signing and lifetime of login tokens |
| `Hosting:BaseUrl` | Public URL of the API, used in links sent by email |
| `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:FromAddress`, `Smtp:FromName`, `Smtp:UseSsl` | Outgoing email |
| `Storage:BaseUrl`, `Storage:BucketName` | S3 storage for documents and images |

### Build and run

```bash
dotnet restore
dotnet build
dotnet run --project src/VCare.Api
```

The OpenAPI document is served in Development at `https://localhost:7443/openapi/v1.json`.

### Migrations

The EF tools run from the host (`VCare.Api`), but the migrations belong to each module. Always name
the module's project and context:

```bash
# Example: the Medications module
dotnet ef migrations add <Name> \
  --project src/Modules/Medications \
  --startup-project src/VCare.Api \
  --context MedicationDbContext \
  --output-dir Migrations

dotnet ef database update \
  --project src/Modules/Medications \
  --startup-project src/VCare.Api \
  --context MedicationDbContext
```

Run `database update` once for each module's context when setting up a new database. Install the
tool once if needed with `dotnet tool install --global dotnet-ef`.

## API overview

Registration and the `/api/auth` routes are open. The other routes work on the caller's care home,
which comes from their login token.

| Area | Routes |
|---|---|
| Registration | `POST /api/carehome` (register a care home and its admin) |
| Auth | `/api/auth/login`, `/logout`, `/refresh`, `/confirm-email`, `/forgot-password`, `/Reset_password` |
| Care home | `PUT /api/carehome/me` |
| Staff | `/api/staff`, plus `/{staffId}/document` and `/{staffId}/profile-image` |
| Patients | `/api/patient`, plus `/{patientId}/document` and `/{patientId}/profile-image` |
| Care plans | `/api/patient/{patientId}/careplan` |
| Medications | `/api/patient/{patientId}/medication`: orders, `/{orderId}/status`, `/due`, `/outstanding`, `/{orderId}/administrations/{scheduleId}` |
| MAR chart | `/api/patient/{patientId}/Marchart` |
| Visits | `/api/patient/{patientId}/visit`: schedule, `/{visitId}/assign`, `/check-in`, `/check-out`, `/cancel` |

The full request and response shapes are in the Postman collection and the OpenAPI spec under
[`postman/`](postman/).

## Adding a new module

1. `dotnet new classlib -n VCare.Modules.<Plural> -o src/Modules/<Plural>` and recreate the tier
   folders.
2. Reference `VCare.SharedKernel`, add `FrameworkReference Microsoft.AspNetCore.App` and the EF Core
   SqlServer package.
3. Write the aggregate (private setters, a factory method and behaviour methods), the
   `<Plural>DbContext` with its own schema, an `IEntityTypeConfiguration<T>`, the repository, the
   service, the endpoints, and the `<Plural>Module` registration class.
4. Reference the module from `VCare.Api` and register its services and endpoints in `Program.cs`.
5. Add the module's first migration (commands above).
