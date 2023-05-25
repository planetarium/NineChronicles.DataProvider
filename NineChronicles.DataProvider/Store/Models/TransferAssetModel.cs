namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class TransferAssetModel
    {
        [Key]
        public string? Id { get; set; }

        public string? TxId { get; set; }

        public long BlockIndex { get; set; }

        public string? BlockHash { get; set; }

        public string? Sender { get; set; }

        public string? Recipient { get; set; }

        public decimal Amount { get; set; }

        public string? TickerType { get; set; }

        public DateOnly Date { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
