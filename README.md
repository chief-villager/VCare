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
  letters. Allergies are recorded with the reaction and severity, and "no known allergies" is a
  recorded answer, not the default. Time in hospital or on leave is recorded too, as are the NHS
  number (checked against its check digit), GP, room, admission date, a history of weights, and
  anyone holding a lasting power of attorney (kept apart from next of kin). Discharging a resident,
  or recording their death, ends their residency: no dose is expected or can be recorded after it.
- **Manage staff:** carers and admins, each with their own login, profile and documents. New staff
  confirm their email before they can sign in. A role change takes effect at once, and a staff
  member who leaves is deactivated: they can't sign in and every session they had ends. Their name
  stays on everything they recorded.
- **Keep a care plan for each resident:** their diagnoses, and for each one the goal and the
  intervention the staff should carry out. Every edit is a numbered version and the earlier ones
  are kept. An edit made to a version someone else has since replaced is refused, instead of
  silently overwriting their change.
- **Run medication rounds:** record each prescription with its dose and schedule (times of day, days
  of the week, every N days, a single one-off dose, or as needed). VCare works out which doses are
  due today and which are still outstanding. Staff sign each dose off as taken or refused, from an
  hour before it is due. A dose not signed within an hour after its slot is recorded as missed.
  Until 12 hours after the slot, a carer can still record what really happened, with a reason, and
  the entry is flagged late. As-required (PRN) doses are recorded with the dose given and why, and
  the order's minimum interval and 24-hour limit are enforced. Doses that fell due before an order
  was entered into VCare are not counted as missed. The result is a MAR chart (Medication
  Administration Record) for each resident, showing each dose's outcome. Stopped orders keep their
  history. An order can be held and restarted, with who did it and why; doses due while it was held
  are not counted as missed. Besides taken and refused, a carer can record a dose as withheld (with
  the clinical reason), asleep, out of stock or vomited. Doses due while the resident is in
  hospital or on leave are recorded as such, not as missed. A controlled drug needs a second member
  of staff as witness. A new order is checked against the resident's recorded allergies by name.
  All dose times are times in the care home's own time zone.
- **Schedule and record care visits:** book visits for a resident and assign staff to them. Each
  assigned carer checks in when they arrive and checks out when they leave, recording whether
  medication, personal care and feeding were done, with a note for each. The server records the
  check-in and check-out times; the client can't set them.

Every care home only ever sees its own data. The care home and the staff member making a change
come from their login, never from the request, so one home cannot read or write another's records.
The database enforces it too, with row-level security.

Every change to a resident's record, and every time someone opens it, goes in an audit trail: who,
when, and for a change, each value before and after.

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
| Audit | The audit trail: owns its table, records views, serves a resident's history |

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
│       ├── Audit/
│       ├── CareHomes/
│       ├── CarePlans/
│       ├── Medications/
│       ├── Notifications/
│       ├── Patients/
│       ├── Staffs/
│       └── Visitation/
├── tests/                          # one test project per module, VCare.Api.Tests, and
│                                   # VCare.IntegrationTests (SQL Server in Docker)
├── scripts/migrations.sh           # runs a dotnet-ef command for every module
├── Dockerfile                      # Linux image of the API
├── .github/workflows/ci.yml        # build, test, migration check, image build
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
- Every context adds two interceptors (`UseVCareInterceptors`):
  - **Audit:** each change to an entity marked `IAudited` is written to `audit.AuditEntries` in the
    same save, so the change and its audit row commit together. Auditing is opt-in, so tables
    holding password or token hashes are never copied into the trail.
  - **Care home:** each connection tells SQL Server whose data it may touch
    (`sp_set_session_context`). A security policy on every table with a care home column then hides
    other homes' rows and refuses writes into them. A signed-in caller gets their own home. Anything
    with no signed-in caller (sign-in, registration, password reset, background jobs, migrations)
    gets every home, as those flows already work across homes. `AspNetUsers` is left out, because
    sign-in and Identity's duplicate checks must see every account.

**Running SQL by hand:** a connection that hasn't said whose data it may touch sees nothing in the
protected tables. For maintenance, start with `EXEC sp_set_session_context N'AllCareHomes', 1;`.

## Running it

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, a container, or a full instance)
- Docker, for the integration tests (they start their own SQL Server)
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
| `Hosting:BaseUrl` | Public URL of the API, including `https://`. Used in email verification links |
| `Hosting:ResetPasswordPageUrl` | Front-end page that asks for a new password. Password reset emails link here with `email` and `token` in the query string |
| `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`, `Smtp:FromAddress`, `Smtp:FromName`, `Smtp:UseSsl` | Outgoing email |
| `Storage:BaseUrl`, `Storage:BucketName` | S3 storage for documents and images |
| `Jwt:MaxSessionHours` (optional, default 12) | The longest a sign-in lasts, however often it is refreshed |
| `BackgroundJobs:Enabled` (optional, default true) | Runs the missed-dose job's timer. With several instances, each pass takes a database lock, so only one instance does it; set this to false for an instance that should never run jobs |
| `OTEL_EXPORTER_OTLP_ENDPOINT` (optional) | Sends traces over OTLP to this collector. Without it, nothing leaves the process |
| `OTEL_SERVICE_NAME` (optional, default `vcare-api`) | The service name traces carry |

Each care home has a time zone (an IANA name such as `Europe/London`), given when it registers
and changed with `PUT /api/carehome/me`. Dose windows, missed doses and absences all use it. The
migration that added it set existing care homes to `UTC`, so set the right zone for each one.

### Build and run

```bash
dotnet restore
dotnet build
dotnet run --project src/VCare.Api
```

The OpenAPI document is served in Development at `https://localhost:7443/openapi/v1.json`, without
signing in.

### Docker

```bash
docker build --tag vcare-api .
docker run -p 8080:8080 -e ConnectionStrings__Default="..." -e Jwt__Key="..." ... vcare-api
```

The container serves plain HTTP on port 8080; put TLS in front of it. Settings come from
environment variables (`Section__Key`). Behind a proxy or load balancer, configure forwarded
headers, so the sign-in rate limits see each client's address rather than the proxy's.

### Tests

```bash
dotnet test
```

`tests/VCare.IntegrationTests` starts SQL Server 2022 in Docker (Testcontainers), migrates every
module through the app, and tests what an in-memory database can't: row-level security, the job
lock, the audit trail, and tracing of SQL.

### Tracing

Every request, outgoing HTTP call (S3) and SQL command gets a span, and so does each pass of the
missed-dose job, with how many doses it recorded. Health checks aren't traced. Set
`OTEL_EXPORTER_OTLP_ENDPOINT` to send them to a collector.

### Health checks

- `GET /health/live`: the process is running.
- `GET /health/ready`: the database can be reached. Returns 503 when it can't.

Both are open, for a load balancer or orchestrator.

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

Run `database update` once for each module's context when setting up a new database, or run all
of them in order with:

```bash
scripts/migrations.sh update
```

`scripts/migrations.sh check` fails if any module's model has changed without a migration; CI runs
it. Install the tool once if needed with `dotnet tool install --global dotnet-ef --version 10.0.5`.

## API overview

Registration, the `/api/auth` routes and the health checks are open. Registration and the auth
routes are rate limited per client address, and five wrong passwords lock an account for 15
minutes. The other routes work on the caller's care home, which comes from their login token. A
token stops working as soon as its holder is deactivated, changes role or resets their password.

| Area | Routes |
|---|---|
| Registration | `POST /api/carehome` (register a care home and its admin) |
| Auth | `/api/auth/login`, `/logout`, `/refresh`, `/verify-email`, `/verify-email/resend`, `/forgot-password`, `/Reset_password` |
| Care home | `PUT /api/carehome/me` |
| Staff | `/api/staff`, plus `/{staffId}/document`, `/{staffId}/profile-image`, `/{staffId}/deactivate` and `/{staffId}/reactivate` |
| Patients | `/api/patient`, plus `/{patientId}/document` and `/{patientId}/profile-image` |
| Resident record | `/api/patient/{patientId}/care-details` (NHS number, GP, room, admission date), `/weights`, `/attorneys`, `/attorneys/{attorneyId}/remove`, `/discharge`, `/death`, `/readmit` |
| Audit trail | `GET /api/patient/{patientId}/audit` (admins), newest first; `?before=` and `?take=` to page |
| Allergies | `/api/patient/{patientId}/allergies`, `/allergies/none`, `/allergies/{allergyId}/remove` |
| Absences | `/api/patient/{patientId}/absences` (start), `/absences/end` |
| Care plans | `/api/patient/{patientId}/careplan`, `/careplan/versions`, `/careplan/versions/{number}`. An update sends the `version` it was made to |
| Medications | `/api/patient/{patientId}/medication`: orders, `/{orderId}/status`, `/due`, `/outstanding`, `/{orderId}/administrations/{scheduleId}`, `/{orderId}/prn-administrations` |
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
