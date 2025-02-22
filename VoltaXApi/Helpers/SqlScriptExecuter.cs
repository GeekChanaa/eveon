using Microsoft.Data.SqlClient;
using System.IO;

namespace VoltaXApi.Helpers
{
    public static class SqlScriptExecuter
    {

        public static async Task ExecuteSqlScript()
        {
            string[] files = { "sql-scripts/countries.sql", "sql-scripts/states.sql", "sql-scripts/cities.sql", "sql-scripts/ocpp-components.sql","sql-scripts/ocpp-variables.sql","sql-scripts/ocpp-variable-components.sql" };
            string[] tables = { "Countries", "States", "Cities","OcppComponents","OcppVariables","OcppVariableComponents" };

            using (SqlConnection connection = new SqlConnection("Server=localhost,1433;Database=VoltaX;User=sa;Password=yourStrong(!)Password;TrustServerCertificate=true"))
            {
                connection.Open();

                for (int i = 0; i < files.Length; i++)
                {
                    // Read the script from the file
                    string script = File.ReadAllText(files[i]);
                    Console.WriteLine($"Populating the database using {files[i]}");

                    // Set IDENTITY_INSERT to ON for the table
                    if(tables[i] == "Countries" || tables[i] == "States" || tables[i] == "Cities")
                    using (SqlCommand command = new SqlCommand($"SET IDENTITY_INSERT {tables[i]} ON;", connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    // Execute the script
                    using (SqlCommand command = new SqlCommand(script, connection))
                    {
                        command.CommandTimeout = 6000;
                        await command.ExecuteNonQueryAsync();
                    }

                    // Set IDENTITY_INSERT back to OFF for the table
                    if(tables[i] == "Countries" || tables[i] == "States" || tables[i] == "Cities")
                    using (SqlCommand command = new SqlCommand($"SET IDENTITY_INSERT {tables[i]} OFF;", connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }
    }
}