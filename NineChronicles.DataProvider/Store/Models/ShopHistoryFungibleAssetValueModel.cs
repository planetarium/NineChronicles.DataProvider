namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class ShopHistoryFungibleAssetValueModel
    {
        [Key]
        public string? OrderId { get; set; }

        public string? TxId { get; set; }

        public long BlockIndex { get; set; }

        public string? BlockHash { get; set; }

        public string? SellerAvatarAddress { get; set; }

        public string? BuyerAvatarAddress { get; set; }

        public decimal Price { get; set; }

        public decimal Quantity { get; set; }

        public string? Ticker { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
