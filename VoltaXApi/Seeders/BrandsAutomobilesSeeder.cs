using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using VoltaXApi.Data;

namespace VoltaXApi.Helpers
{
    public static class BrandsAutomobilesSeeder
    {
        public static async Task Populate(VoltaXApiDbContext dbContext)
        {
            string[] files = { "sql-scripts/brands.sql", "sql-scripts/automobiles.sql"};
            string[] tables = { "Brands", "Automobiles"};

            DbConnection dbConnection = dbContext.Database.GetDbConnection();
            try
            {

                // Open the connection if not already open
                if (dbConnection.State != System.Data.ConnectionState.Open)
                {
                    await dbConnection.OpenAsync();
                }
                if (dbConnection is SqlConnection sqlConnection)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            // Read the script from the file
                            string script = File.ReadAllText(files[i]);
                            Console.WriteLine($"Populating the database using {files[i]}");

                            // Set IDENTITY_INSERT to ON for the table
                            using (SqlCommand command = new SqlCommand($"SET IDENTITY_INSERT {tables[i]} ON;", sqlConnection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }

                            // Execute the script
                            using (SqlCommand command = new SqlCommand(script, sqlConnection))
                            {
                                command.CommandTimeout = 6000;
                                await command.ExecuteNonQueryAsync();
                            }

                            // Set IDENTITY_INSERT back to OFF for the table
                            using (SqlCommand command = new SqlCommand($"SET IDENTITY_INSERT {tables[i]} OFF;", sqlConnection))
                            {
                                await command.ExecuteNonQueryAsync();
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

