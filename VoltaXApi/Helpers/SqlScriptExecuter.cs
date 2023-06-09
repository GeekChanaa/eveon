using Microsoft.Data.SqlClient;
using System.IO;

namespace VoltaXApi.Helpers
{
    public static class SqlScriptExecuter
    {

        public static void ExecuteSqlScript(string filePath)
        {
            // Assume that your .sql file is at the root of your project directory
            string script = File.ReadAllText(filePath);
            Console.WriteLine("here we're populating the database");
            using (SqlConnection connection = new SqlConnection("Server=localhost,1433;Database=VoltaX;User=sa;Password=yourStrong(!)Password;TrustServerCertificate=true"))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(script, connection);
                command.ExecuteNonQuery();
            }
        }
    }
}