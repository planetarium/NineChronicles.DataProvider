namespace NineChronicles.DataProvider.Store.Models
{
    public struct HackAndSlashModel
    {
        public string Agent_Address { get; set; }

        public string Avatar_Address { get; set; }

        public int Stage_Id { get; set; }

        public bool Cleared { get; set; }

        public string Block_Hash { get; set; }
    }
}
