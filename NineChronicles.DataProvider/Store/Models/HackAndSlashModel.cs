namespace NineChronicles.DataProvider.Store.Models
{
    public class HackAndSlashModel
    {
        public string Agent_Address { get; set; } = null!;

        public string Avatar_Address { get; set; } = null!;

        public int Stage_Id { get; set; }

        public bool Cleared { get; set; }
    }
}
