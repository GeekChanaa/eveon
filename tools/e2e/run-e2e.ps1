# EVEON simulator end-to-end matrix (default) or load run against a local API on a throwaway SQLite database.
#
#   tools/e2e/run-e2e.ps1                                  # every preset on OCPP 1.6 and 2.0.1
#   tools/e2e/run-e2e.ps1 --only "Fault" --version 1.6     # presets whose name contains "Fault", 1.6 only
#   tools/e2e/run-e2e.ps1 load --chargers 100 --minutes 2  # load run
#   tools/e2e/run-e2e.ps1 --help                           # every option
#
# The simulator is an EVEON_SIM checkout: --sim <dir>, or $env:EVEON_SIM_DIR, or an EVEON_SIM folder next to this
# repository. Needs the .NET 9 SDK and Node.js 20+. Results land in tools/e2e/out/<run>/ (summary.md, logs, the
# database). Exit code: 0 passed, 1 a check failed, 2 the run could not be set up. See docs/testing.md.
$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'VoltaX.E2E'
dotnet build -c Release $project -nologo -v quiet
if ($LASTEXITCODE -ne 0) { exit 2 }
dotnet (Join-Path $project 'bin/Release/net9.0/VoltaX.E2E.dll') @args
exit $LASTEXITCODE
