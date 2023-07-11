using Hangfire;
using Hangfire.MySql;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.IO;

namespace NineChronicles.DataProvider.Batch
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            string connectionString = configuration.GetConnectionString("MysqlConnectionString");

            GlobalConfiguration.Configuration.UseStorage(new MySqlStorage(connectionString));

            var server = new BackgroundJobServer();

            RecurringJob.AddOrUpdate(() => UpdateDatabaseTables(connectionString), Cron.Minutely); // Or set your own schedule

            Console.WriteLine("Hangfire Server started. Press any key to exit...");
            Console.ReadKey();

            server.Dispose();
        }

        public static void UpdateDatabaseTables(string connectionString)
        {
            string selectQuery = "SELECT * FROM temp_table";

            string updateQuery1 = "UPDATE table1 SET ...";
            string updateQuery2 = "UPDATE table2 SET ...";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                MySqlCommand selectCommand = new MySqlCommand(selectQuery, connection);
                var reader = selectCommand.ExecuteReader();

                while (reader.Read())
                {
                    // Logic based on selectQuery's result

                    reader.Close();

                    MySqlCommand updateCommand1 = new MySqlCommand(updateQuery1, connection);
                    MySqlCommand updateCommand2 = new MySqlCommand(updateQuery2, connection);

                    updateCommand1.ExecuteNonQuery();
                    updateCommand2.ExecuteNonQuery();
                }
            }
        }
    }
}
