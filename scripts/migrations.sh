#!/usr/bin/env bash
# Runs one EF Core command for every module's DbContext, in dependency order.
#
#   scripts/migrations.sh update   # apply all pending migrations (database update)
#   scripts/migrations.sh check    # fail if any module's model has changes with no migration
#
# Needs ConnectionStrings__Default (or appsettings.json in src/VCare.Api) and the
# dotnet-ef tool: dotnet tool install --global dotnet-ef --version 10.0.5
# Extra arguments are passed through, e.g. --no-build or --configuration Release.
set -euo pipefail

cd "$(dirname "$0")/.."

action="${1:-}"
shift || true

# project folder : context. Audit first: every other module writes to its table.
# Then CareHomes and Staffs: everything else hangs off them.
modules=(
  "src/Modules/Audit:AuditDbContext"
  "src/Modules/CareHomes:CareHomeDbContext"
  "src/Modules/Staffs:StaffDbContext"
  "src/Modules/Patients:PatientsDbContext"
  "src/Modules/CarePlans:CarePlanDbContext"
  "src/Modules/Medications:MedicationDbContext"
  "src/Modules/Visitation:VisitationDbContext"
)

for entry in "${modules[@]}"; do
  project="${entry%%:*}"
  context="${entry##*:}"
  echo "==> ${context}"
  case "$action" in
    update)
      dotnet ef database update --project "$project" --startup-project src/VCare.Api --context "$context" "$@"
      ;;
    check)
      dotnet ef migrations has-pending-model-changes --project "$project" --startup-project src/VCare.Api --context "$context" "$@"
      ;;
    *)
      echo "usage: $0 update|check [dotnet-ef options]" >&2
      exit 2
      ;;
  esac
done
