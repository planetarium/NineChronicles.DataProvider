namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Date))]

    public class RequestPledgeModel
    {
        [Key]
        public string? TxId { get; set; }

        public long BlockIndex { get; set; }

        public string? BlockHash { get; set; }

        public string? TxSigner { get; set; }

        public string? PledgeAgentAddress { get; set; }

        public int RefillMead { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
