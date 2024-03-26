namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(Date))]

    public class ApprovePledgeModel
    {
        [Key]
        public string? TxId { get; set; }

        public long BlockIndex { get; set; }

        public string? BlockHash { get; set; }

        public string? Signer { get; set; }

        public string? PatronAddress { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
