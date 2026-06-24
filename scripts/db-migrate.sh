#!/usr/bin/env bash
#
# Apply EF Core migrations to a target database locally.
#
# Wraps `dotnet ef database update` for the NotesApp solution. Use it to migrate a
# local or remote (e.g. Neon) database from your machine. The GitHub workflow
# .github/workflows/db-migrate.yml does the same thing in CI.
#
# Connection string resolution (first match wins):
#   1. --connection "<conn>"
#   2. $ConnectionStrings__Default
#   3. local Compose default (localhost)
# For Neon, use the DIRECT (non-pooled, no "-pooler") endpoint with "SSL Mode=Require".
#
# Usage:
#   ./scripts/db-migrate.sh                          # latest, local DB
#   ./scripts/db-migrate.sh --target 0               # revert ALL migrations
#   ./scripts/db-migrate.sh --connection "Host=ep-xxx.neon.tech;Database=notesdb;Username=...;Password=...;SSL Mode=Require"
#
set -euo pipefail

CONNECTION="${ConnectionStrings__Default:-}"
TARGET=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    -c|--connection) CONNECTION="$2"; shift 2 ;;
    -t|--target)     TARGET="$2"; shift 2 ;;
    -h|--help)       sed -n '2,20p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "$CONNECTION" ]]; then
  CONNECTION="Host=localhost;Port=5432;Database=notesdb;Username=notes;Password=notes_dev_password"
  echo "No connection string supplied; using local default." >&2
fi

# The design-time factory (NotesDbContextFactory) reads this env var.
export ConnectionStrings__Default="$CONNECTION"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_DIR="$(cd "$SCRIPT_DIR/../api" && pwd)"

cd "$API_DIR"

echo "Applying migrations..."
if [[ -n "$TARGET" ]]; then
  dotnet ef database update "$TARGET" \
    --project src/NotesApp.Infrastructure \
    --startup-project src/NotesApp.Api
else
  dotnet ef database update \
    --project src/NotesApp.Infrastructure \
    --startup-project src/NotesApp.Api
fi
