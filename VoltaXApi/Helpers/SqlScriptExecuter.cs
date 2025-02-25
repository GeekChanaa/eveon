using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.IO;
using VoltaXApi.Data;

namespace VoltaXApi.Helpers
{
    public static class SqlScriptExecuter
    {

        public static async Task ExecuteSqlScript(VoltaXApiDbContext dbContext)
        {
            // Get the existing SQL connection from DbContext
            DbConnection dbConnection = dbContext.Database.GetDbConnection();
            string[] files = { "sql-scripts/countries.sql", "sql-scripts/states.sql", "sql-scripts/cities.sql", "sql-scripts/ocpp-components.sql", "sql-scripts/ocpp-variables.sql", "sql-scripts/ocpp-variable-components.sql" };
            string[] tables = { "Countries", "States", "Cities", "OcppComponents", "OcppVariables", "OcppVariableComponents" };
            // dbContext is the variable of db
            try
            {

                // Open the connection if not already open
                if (dbConnection.State != System.Data.ConnectionState.Open)
                {
                    await dbConnection.OpenAsync();
                }

                for (int i = 0; i < files.Length; i++)
                {
                    string script = await File.ReadAllTextAsync(files[i]);
                    Console.WriteLine($"Populating the database using {files[i]}");

                    // Use the existing connection from DbContext
                    if (dbConnection is SqlConnection sqlConnection)
                    {
                        using (var command = sqlConnection.CreateCommand())
                        {
                            command.CommandTimeout = 6000;

                            // Set IDENTITY_INSERT ON if needed
                            if (tables[i] == "Countries" || tables[i] == "States" || tables[i] == "Cities")
                            {
                                command.CommandText = $"SET IDENTITY_INSERT {tables[i]} ON;";
                                await command.ExecuteNonQueryAsync();
                            }

                            // Execute the script
                            command.CommandText = script;
                            await command.ExecuteNonQueryAsync();

                            // Set IDENTITY_INSERT OFF if needed
                            if (tables[i] == "Countries" || tables[i] == "States" || tables[i] == "Cities")
                            {
                                command.CommandText = $"SET IDENTITY_INSERT {tables[i]} OFF;";
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            finally
            {
                // Close the connection (optional, depends on your DB context's lifespan)
                await dbConnection.CloseAsync();
            }
        }
    }
}