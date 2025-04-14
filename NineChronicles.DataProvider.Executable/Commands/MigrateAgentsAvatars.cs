namespace NineChronicles.DataProvider.Executable.Commands
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Cocona;
    using MySqlConnector;

    public partial class MigrateAgentsAvatars
    {
        public async Task Migration(
            [Option("source-server")] string sourceServer,
            [Option("source-port")] uint sourcePort,
            [Option("source-username")] string sourceUsername,
            [Option("source-password")] string sourcePassword,
            [Option("source-database")] string sourceDatabase,
            [Option("target-server")] string targetServer,
            [Option("target-port")] uint targetPort,
            [Option("target-username")] string targetUsername,
            [Option("target-password")] string targetPassword,
            [Option("target-database")] string targetDatabase)
        {
            string sourceConnStr = new MySqlConnectionStringBuilder
            {
                Server = sourceServer,
                Port = sourcePort,
                UserID = sourceUsername,
                Password = sourcePassword,
                Database = sourceDatabase,
                AllowLoadLocalInfile = true,
                AllowUserVariables = true,
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true,
                ConnectionTimeout = 36000,
                DefaultCommandTimeout = 36000,
            }.ToString();

            string targetConnStr = new MySqlConnectionStringBuilder
            {
                Server = targetServer,
                Port = targetPort,
                UserID = targetUsername,
                Password = targetPassword,
                Database = targetDatabase,
                AllowLoadLocalInfile = true,
                AllowUserVariables = true,
                AllowZeroDateTime = true,
                ConvertZeroDateTime = true,
                ConnectionTimeout = 36000,
                DefaultCommandTimeout = 36000,
            }.ToString();

            string avatarsFile = Path.GetTempFileName();
            string agentsFile = Path.GetTempFileName();

            // Export Agents
            await using (var writer = new StreamWriter(agentsFile))
            await using (var sourceConn = new MySqlConnection(sourceConnStr))
            {
                await sourceConn.OpenAsync();
                var cmd = new MySqlCommand("SELECT Address FROM data_provider.Agents", sourceConn);
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string address = reader.GetString("Address");
                    await writer.WriteLineAsync(address);
                }

                await reader.CloseAsync();
            }

            // Export Avatars
            await using (var writer = new StreamWriter(avatarsFile))
            await using (var sourceConn = new MySqlConnection(sourceConnStr))
            {
                await sourceConn.OpenAsync();
                var cmd = new MySqlCommand("SELECT Address, AgentAddress, Name, AvatarLevel, TitleId, ArmorId, CP, Timestamp FROM data_provider.Avatars", sourceConn);
                var reader = await cmd.ExecuteReaderAsync();

                int line = 0;
                while (await reader.ReadAsync())
                {
                    var values = new string[]
                    {
                        reader.GetString(0), // Address
                        reader.GetString(1), // AgentAddress
                        reader.GetString(2).Replace("\"", "\"\""), // Name (escaped, in case needed)
                        reader.IsDBNull(3) ? "0" : reader.GetInt32(3).ToString(), // AvatarLevel
                        reader.IsDBNull(4) ? "0" : reader.GetInt32(4).ToString(), // TitleId
                        reader.IsDBNull(5) ? "0" : reader.GetInt32(5).ToString(), // ArmorId
                        reader.IsDBNull(6) ? "0" : reader.GetInt32(6).ToString(), // CP
                        reader.IsDBNull(7) ? "1970-01-01 00:00:00.000000" : reader.GetDateTime(7).ToString("yyyy-MM-dd HH:mm:ss.ffffff") // Timestamp (DATETIME(6))
                    };

                    string lineText = string.Join(";", values);
                    Console.WriteLine($"[{++line}] {lineText}");
                    await writer.WriteLineAsync(lineText);
                }

                await reader.CloseAsync();
            }

            // Bulk Insert Agents
            using (var targetConn = new MySqlConnection(targetConnStr))
            {
                var loader = new MySqlBulkLoader(targetConn)
                {
                    TableName = $"{targetDatabase}.Agents",
                    FileName = agentsFile,
                    Timeout = 0,
                    LineTerminator = "\n",
                    FieldTerminator = "\t",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore
                };
                loader.Columns.Add("Address");

                Console.WriteLine("Inserting Agents...");
                loader.Load();
                Console.WriteLine("Agents insert complete.");
            }

            // Bulk Insert Avatars
            using (var targetConn = new MySqlConnection(targetConnStr))
            {
                var loader = new MySqlBulkLoader(targetConn)
                {
                    TableName = $"{targetDatabase}.Avatars",
                    FileName = avatarsFile,
                    Timeout = 0,
                    LineTerminator = "\n",
                    FieldTerminator = ";",
                    Local = true,
                    ConflictOption = MySqlBulkLoaderConflictOption.Ignore
                };
                loader.Columns.AddRange(new[]
                {
                    "Address", "AgentAddress", "Name", "AvatarLevel", "TitleId", "ArmorId", "CP", "Timestamp"
                });
                loader.FieldTerminator = ";";
                loader.FieldQuotationCharacter = '"'; // Optional, helps with escaping longtext
                loader.FieldQuotationOptional = true;

                Console.WriteLine("Inserting Avatars...");
                loader.Load();
                Console.WriteLine("Avatars insert complete.");
            }

            Console.WriteLine("Migration complete!");
        }
    }
}
