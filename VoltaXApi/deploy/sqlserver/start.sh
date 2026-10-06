#!/bin/sh
set -e

# Crash dumps land on the volume and can fill it; keep only what SQL Server needs.
rm -rf /var/opt/mssql/log/core.* /var/opt/mssql/log/*.gdmp /var/opt/mssql/log/*.tbz2 2>/dev/null || true

CONF=/opt/mssql/bin/mssql-conf
# Railway volumes do not support O_DIRECT; trace flag 3979 with forced
# write-through keeps writes durable without it. 1800 aligns log IO to 4K,
# since the volume reports a sector size SQL Server otherwise misaligns.
$CONF set control.writethrough 1
$CONF set control.alternatewritethrough 0
$CONF traceflag 3979 1800 on
# The host reports far more memory than the service gets; size SQL Server to the service.
$CONF set memory.memorylimitmb "${MSSQL_MEMORY_LIMIT_MB:-2048}"
# Small dumps only, so a crash cannot fill the volume.
$CONF set coredump.captureminiandfull false
$CONF set coredump.coredumptype mini

exec /opt/mssql/bin/sqlservr
