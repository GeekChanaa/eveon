using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace VoltaXApi.Migrations;

/// <summary>
/// The first migration (<see cref="recreate"/>) was scaffolded against MySQL: every column carries a
/// MySQL store type ("longtext", "datetime(6)", "tinyint(1)", "double"...), keys use MySQL
/// auto-increment and long names are cut to MySQL's 64 characters. On MySQL / MariaDB it runs
/// exactly as generated.
///
/// On any other provider (SQL Server) its operations are rewritten before they reach the SQL
/// generator, so that the result is the schema the model describes for that provider (what
/// EnsureCreated builds; docs/database.md says how this was checked):
///  - the MySQL store type is dropped, so the provider maps the CLR type itself;
///  - an explicit varchar(n) keeps n as its maximum length, while the 255 that MySQL gives indexed
///    strings without a length becomes SQL Server's 450;
///  - auto-increment keys become identity columns;
///  - unique indexes over nullable columns get the "IS NOT NULL" filter SQL Server needs;
///  - names cut at 64 characters ("...~") get back their full conventional name.
/// </summary>
internal static class MySqlScaffoldedOperations
{
    public const string MySqlProvider = "Pomelo.EntityFrameworkCore.MySql";
    public const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    // Longest string SQL Server can index (900 bytes); what EF gives indexed strings without a length.
    private const int SqlServerKeyLength = 450;

    private static readonly Regex VarChar = new(@"^(?:var)?char\((?<length>\d+)\)$", RegexOptions.IgnoreCase);

    public static bool AppliesAsGenerated(string? activeProvider) => activeProvider == MySqlProvider;

    public static void AdaptToActiveProvider(MigrationBuilder migrationBuilder)
    {
        if (AppliesAsGenerated(migrationBuilder.ActiveProvider))
            return;

        var sqlServer = migrationBuilder.ActiveProvider == SqlServerProvider;
        var nullableColumns = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var operation in migrationBuilder.Operations)
        {
            switch (operation)
            {
                case CreateTableOperation table:
                    RemoveMySqlAnnotations(table);
                    foreach (var column in table.Columns)
                        Adapt(column, sqlServer);
                    foreach (var foreignKey in table.ForeignKeys.Where(fk => IsCut(fk.Name)))
                        foreignKey.Name = $"FK_{table.Name}_{foreignKey.PrincipalTable}_{string.Join("_", foreignKey.Columns)}";
                    nullableColumns[table.Name] = table.Columns.Where(c => c.IsNullable).Select(c => c.Name).ToHashSet();
                    break;
                case AlterDatabaseOperation database:
                    // Only carries the MySQL character set.
                    RemoveMySqlAnnotations(database);
                    break;
                case CreateIndexOperation index:
                    if (IsCut(index.Name))
                        index.Name = $"IX_{index.Table}_{string.Join("_", index.Columns)}";
                    if (sqlServer && index is { IsUnique: true, Filter: null } && nullableColumns.TryGetValue(index.Table, out var nullable))
                    {
                        var filtered = index.Columns.Where(nullable.Contains).Select(c => $"[{c}] IS NOT NULL").ToList();
                        if (filtered.Count > 0)
                            index.Filter = string.Join(" AND ", filtered);
                    }
                    break;
            }
        }
    }

    private static bool IsCut(string name) => name.EndsWith('~');

    private static void Adapt(ColumnOperation column, bool sqlServer)
    {
        if (column.ColumnType is { } type && column.ClrType == typeof(string) && VarChar.Match(type) is { Success: true } match)
            column.MaxLength ??= sqlServer ? SqlServerKeyLength : int.Parse(match.Groups["length"].Value);
        column.ColumnType = null;

        if (column["MySql:ValueGenerationStrategy"] is MySqlValueGenerationStrategy.IdentityColumn)
            column["SqlServer:Identity"] = "1, 1";
        RemoveMySqlAnnotations(column);
    }

    private static void RemoveMySqlAnnotations(MigrationOperation operation)
    {
        foreach (var annotation in operation.GetAnnotations().Where(a => a.Name.StartsWith("MySql:", StringComparison.Ordinal)).ToList())
            operation.RemoveAnnotation(annotation.Name);
    }
}
