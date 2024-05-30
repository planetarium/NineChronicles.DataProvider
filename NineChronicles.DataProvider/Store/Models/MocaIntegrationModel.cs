namespace NineChronicles.DataProvider.Store.Models
{
    public class MocaIntegrationModel
    {
        public int MocaIntegration { get; set; }

        public string Signer { get; set; } = string.Empty;

        public bool Migrated { get; set; }
    }
}
