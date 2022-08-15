namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class BlockModel
    {
        public long Index { get; set; }

        [Key]
        public string? Hash { get; set; }

        public string? Miner { get; set; }

        public long Difficulty { get; set; }

        public string? Nonce { get; set; }

        public string? PreviousHash { get; set; }

        public int? ProtocolVersion { get; set; }

        public string? PublicKey { get; set; }

        public string? StateRootHash { get; set; }

        public long? TotalDifficulty { get; set; }

        public int? TxCount { get; set; }

        public string? TxHash { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
