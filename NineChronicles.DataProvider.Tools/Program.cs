namespace NineChronicles.DataProvider.Tools
{
    using Cocona;
    using NineChronicles.DataProvider.Tools.SubCommand;

    [HasSubCommands(typeof(MySqlMigration), Description = "Manage MySql store.")]
    internal class Program
    {
        private static void Main(string[] args) => CoconaLiteApp.Run<Program>(args);

        private void Help()
        {
            Main(new[] { "--help" });
        }
    }
}
