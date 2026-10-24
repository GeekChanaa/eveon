#!/usr/bin/env bash
# EVEON simulator end-to-end matrix (default) or load run against a local API on a throwaway SQLite database.
#
#   tools/e2e/run-e2e.sh                                  # every preset on OCPP 1.6 and 2.0.1
#   tools/e2e/run-e2e.sh --only "Fault" --version 1.6     # presets whose name contains "Fault", 1.6 only
#   tools/e2e/run-e2e.sh load --chargers 100 --minutes 2  # load run
#   tools/e2e/run-e2e.sh --help                           # every option
#
# The simulator is an EVEON_SIM checkout: --sim <dir>, or EVEON_SIM_DIR, or an EVEON_SIM folder next to this
# repository. Needs the .NET 9 SDK and Node.js 20+. Results land in tools/e2e/out/<run>/ (summary.md, logs, the
# database). Exit code: 0 passed, 1 a check failed, 2 the run could not be set up. See docs/testing.md.
set -Eeuo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
dotnet build -c Release "$here/VoltaX.E2E" -nologo -v quiet || exit 2
exec dotnet "$here/VoltaX.E2E/bin/Release/net9.0/VoltaX.E2E.dll" "$@"
