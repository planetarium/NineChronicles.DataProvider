namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class ShopHistoryCostumeModel
    {
        [Key]
        public string? OrderId { get; set; }

        public string? TxId { get; set; }

        public long BlockIndex { get; set; }

        public string? BlockHash { get; set; }

        public string? ItemId { get; set; }

        public string? SellerAvatarAddress { get; set; }

        public string? BuyerAvatarAddress { get; set; }

        public decimal Price { get; set; }

        public string? ItemType { get; set; }

        public string? ItemSubType { get; set; }

        public int Id { get; set; }

        public string? ElementalType { get; set; }

        public int Grade { get; set; }

        public bool Equipped { get; set; }

        public string? SpineResourcePath { get; set; }

        public long RequiredBlockIndex { get; set; }

        public string? NonFungibleId { get; set; }

        public string? TradableId { get; set; }

        public int ItemCount { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
