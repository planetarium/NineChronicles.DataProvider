namespace NineChronicles.DataProvider.Batch
{
    using System;
    using System.IO;
    using System.Transactions;
    using Hangfire;
    using Hangfire.MySql;
    using Microsoft.Extensions.Configuration;

    public static class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            string connectionString = configuration.GetConnectionString("HangfireMysqlConnectionString");
            var options = new MySqlStorageOptions
            {
                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                DashboardJobListLimit = 50000,
                TransactionTimeout = TimeSpan.FromMinutes(1),
                TablesPrefix = "Hangfire",
            };

            GlobalConfiguration.Configuration.UseStorage(new MySqlStorage(connectionString, options));

            var server = new BackgroundJobServer();

            RecurringJob.AddOrUpdate("TempID", () => UpdateDatabaseTables(connectionString), Cron.Minutely); // Or set your own schedule

            Console.WriteLine("Hangfire Server started. Press any key to exit...");
            Console.ReadKey();

            server.Dispose();
        }

        public static void UpdateDatabaseTables(string connectionString)
        {
            Console.WriteLine("Job");
        }
    }
}
