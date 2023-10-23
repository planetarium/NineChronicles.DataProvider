namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;

    public static class ShopHistoryEquipmentData
    {
        public static ShopHistoryEquipmentModel GetShopHistoryEquipmentInfo(
            Address buyerAvatarAddress,
            PurchaseInfo purchaseInfo,
            Equipment equipment,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryEquipmentModel = new ShopHistoryEquipmentModel()
            {
                OrderId = purchaseInfo.OrderId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = equipment.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = equipment.ItemType.ToString(),
                ItemSubType = equipment.ItemSubType.ToString(),
                Id = equipment.Id,
                BuffSkillCount = equipment.BuffSkills.Count,
                ElementalType = equipment.ElementalType.ToString(),
                Grade = equipment.Grade,
                SetId = equipment.SetId,
                SkillsCount = equipment.Skills.Count,
                SpineResourcePath = equipment.SpineResourcePath,
                RequiredBlockIndex = equipment.RequiredBlockIndex,
                NonFungibleId = equipment.NonFungibleId.ToString(),
                TradableId = equipment.ItemId.ToString(),
                UniqueStatType = equipment.UniqueStatType.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryEquipmentModel;
        }

        public static ShopHistoryEquipmentModel GetShopHistoryEquipmentInfoV2(
            Address buyerAvatarAddress,
            PurchaseInfo0 purchaseInfo,
            Equipment equipment,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryEquipmentModel = new ShopHistoryEquipmentModel()
            {
                OrderId = purchaseInfo.productId.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = equipment.ItemId.ToString(),
                SellerAvatarAddress = purchaseInfo.sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(purchaseInfo.price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = equipment.ItemType.ToString(),
                ItemSubType = equipment.ItemSubType.ToString(),
                Id = equipment.Id,
                BuffSkillCount = equipment.BuffSkills.Count,
                ElementalType = equipment.ElementalType.ToString(),
                Grade = equipment.Grade,
                SetId = equipment.SetId,
                SkillsCount = equipment.Skills.Count,
                SpineResourcePath = equipment.SpineResourcePath,
                RequiredBlockIndex = equipment.RequiredBlockIndex,
                NonFungibleId = equipment.NonFungibleId.ToString(),
                TradableId = equipment.ItemId.ToString(),
                UniqueStatType = equipment.UniqueStatType.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryEquipmentModel;
        }

        public static ShopHistoryEquipmentModel GetShopHistoryEquipmentInfoV1(
            Address buyerAvatarAddress,
            Address sellerAvatarAddress,
            Buy7.BuyerResult buyerResult,
            FungibleAssetValue price,
            Equipment equipment,
            int itemCount,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var shopHistoryEquipmentModel = new ShopHistoryEquipmentModel()
            {
                OrderId = buyerResult.id.ToString(),
                TxId = string.Empty,
                BlockIndex = blockIndex,
                BlockHash = string.Empty,
                ItemId = equipment.ItemId.ToString(),
                SellerAvatarAddress = sellerAvatarAddress.ToString(),
                BuyerAvatarAddress = buyerAvatarAddress.ToString(),
                Price = decimal.Parse(price.ToString().Split(" ").FirstOrDefault()!),
                ItemType = equipment.ItemType.ToString(),
                ItemSubType = equipment.ItemSubType.ToString(),
                Id = equipment.Id,
                BuffSkillCount = equipment.BuffSkills.Count,
                ElementalType = equipment.ElementalType.ToString(),
                Grade = equipment.Grade,
                SetId = equipment.SetId,
                SkillsCount = equipment.Skills.Count,
                SpineResourcePath = equipment.SpineResourcePath,
                RequiredBlockIndex = equipment.RequiredBlockIndex,
                NonFungibleId = equipment.NonFungibleId.ToString(),
                TradableId = equipment.ItemId.ToString(),
                UniqueStatType = equipment.UniqueStatType.ToString(),
                ItemCount = itemCount,
                TimeStamp = blockTime,
            };

            return shopHistoryEquipmentModel;
        }
    }
}
