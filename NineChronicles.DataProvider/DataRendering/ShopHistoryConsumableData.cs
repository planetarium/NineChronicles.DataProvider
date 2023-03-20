namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryConsumableData
    {
        public static ShopHistoryConsumableModel GetShopHistoryConsumableInfo(
            Address buyerAvatarAddress,
            PurchaseInfo purchaseInfo,
            Consumable consumable,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryConsumableModel = new ShopHistoryConsumableModel()
            {
                OrderId = purchaseInfo.OrderId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = consumable.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = consumable.ItemType.ToString(),
                ItemSubType = consumable.ItemSubType.ToString(),
                Id = consumable.Id,
                BuffSkillCount = consumable.BuffSkills.Count,
                ElementalType = consumable.ElementalType.ToString(),
                Grade = consumable.Grade,
                SkillsCount = consumable.Skills.Count,
                RequiredBlockIndex = consumable.RequiredBlockIndex,
                NonFungibleId = consumable.NonFungibleId.ToString(),
                TradableId = consumable.TradableId.ToString(),
                MainStat = consumable.MainStat.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryConsumableModel;
        }

        public static ShopHistoryConsumableModel GetShopHistoryConsumableInfoV2(
            Address buyerAvatarAddress,
            PurchaseInfo0 purchaseInfo,
            Consumable consumable,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryConsumableModel = new ShopHistoryConsumableModel()
            {
                OrderId = purchaseInfo.productId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = consumable.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = consumable.ItemType.ToString(),
                ItemSubType = consumable.ItemSubType.ToString(),
                Id = consumable.Id,
                BuffSkillCount = consumable.BuffSkills.Count,
                ElementalType = consumable.ElementalType.ToString(),
                Grade = consumable.Grade,
                SkillsCount = consumable.Skills.Count,
                RequiredBlockIndex = consumable.RequiredBlockIndex,
                NonFungibleId = consumable.NonFungibleId.ToString(),
                TradableId = consumable.TradableId.ToString(),
                MainStat = consumable.MainStat.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryConsumableModel;
        }

        public static ShopHistoryConsumableModel GetShopHistoryConsumableInfoV1(
            Address buyerAvatarAddress,
            Address sellerAvatarAddress,
            Buy7.BuyerResult buyerResult,
            FungibleAssetValue price,
            Consumable consumable,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryConsumableModel = new ShopHistoryConsumableModel()
            {
                OrderId = buyerResult.id.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = consumable.ItemId.ToString(),
                SellerAvatarAddress = sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = consumable.ItemType.ToString(),
                ItemSubType = consumable.ItemSubType.ToString(),
                Id = consumable.Id,
                BuffSkillCount = consumable.BuffSkills.Count,
                ElementalType = consumable.ElementalType.ToString(),
                Grade = consumable.Grade,
                SkillsCount = consumable.Skills.Count,
                RequiredBlockIndex = consumable.RequiredBlockIndex,
                NonFungibleId = consumable.NonFungibleId.ToString(),
                TradableId = consumable.TradableId.ToString(),
                MainStat = consumable.MainStat.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryConsumableModel;
        }
    }
}
