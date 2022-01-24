namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Microsoft.EntityFrameworkCore;
    using Nekoyume.Model.Item;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;

    public class MySqlStore
    {
        private readonly IDbContextFactory<NineChroniclesContext> _dbContextFactory;

        public MySqlStore(IDbContextFactory<NineChroniclesContext> contextFactory)
        {
            _dbContextFactory = contextFactory;
        }

        public void StoreAvatar(
            Address address,
            Address agentAddress,
            string name,
            int? avatarLevel,
            int? titleId,
            int? armorId,
            int? cp)
        {
            try
            {
                using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                if (ctx.Avatars?.Find(address.ToString()) is null)
                {
                    ctx.Avatars!.Add(
                        new AvatarModel()
                        {
                            Address = address.ToString(),
                            AgentAddress = agentAddress.ToString(),
                            Name = name,
                            AvatarLevel = avatarLevel,
                            TitleId = titleId,
                            ArmorId = armorId,
                            Cp = cp,
                        }
                    );
                    ctx.SaveChanges();
                }
                else
                {
                    ctx.Dispose();
                    using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                    if (avatarLevel == null && titleId == null && armorId == null && cp == null)
                    {
                        updateCtx.Avatars!.Update(
                            new AvatarModel()
                            {
                                Address = address.ToString(),
                                AgentAddress = agentAddress.ToString(),
                                Name = name,
                            }
                        );
                        updateCtx.SaveChanges();
                    }
                    else
                    {
                        updateCtx.Avatars!.Update(
                            new AvatarModel()
                            {
                                Address = address.ToString(),
                                AgentAddress = agentAddress.ToString(),
                                Name = name,
                                AvatarLevel = avatarLevel,
                                TitleId = titleId,
                                ArmorId = armorId,
                                Cp = cp,
                            }
                        );
                        updateCtx.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreAgent(Address address)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.Agents?.Find(address.ToString()) is null)
            {
                ctx.Agents!.Add(
                    new AgentModel()
                    {
                        Address = address.ToString(),
                    }
                );
            }

            ctx.SaveChanges();
        }

        public void StoreHackAndSlash(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            int stageId,
            bool cleared,
            bool isMimisbrunnr,
            long blockIndex)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            ctx.HackAndSlashes!.Add(
                new HackAndSlashModel()
                {
                    Id = id.ToString(),
                    AgentAddress = agentAddress.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    StageId = stageId,
                    Cleared = cleared,
                    Mimisbrunnr = isMimisbrunnr,
                    BlockIndex = blockIndex,
                }
            );
            ctx.SaveChanges();
        }

        public void DeleteHackAndSlash(Guid id)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.HackAndSlashes?.Find(id.ToString()) is { } has)
            {
                ctx.Remove(has);
            }

            ctx.SaveChanges();
        }

        public IEnumerable<HackAndSlashModel> GetHackAndSlash(
            Address? agentAddress = null,
            int? limit = null)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<HackAndSlashModel> hackAndSlashes = ctx.HackAndSlashes!;

            if (agentAddress is { } agentAddressNotNull)
            {
                hackAndSlashes = hackAndSlashes
                    .Where(has => has.AgentAddress == agentAddressNotNull.ToString());
            }

            if (limit is { } limitNotNull)
            {
                hackAndSlashes = hackAndSlashes.Take(limitNotNull);
            }

            return hackAndSlashes.ToList();
        }

        public IEnumerable<StageRankingModel> GetStageRanking(
            Address? avatarAddress = null,
            int? limit = null,
            bool isMimisbrunnr = false)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            var query = ctx.Set<StageRankingModel>()
                .FromSqlRaw("SELECT `h`.`AvatarAddress`, `h`.`AgentAddress`, MAX(`h`.`StageId`) " +
                            "AS `ClearedStageId`, " +
                            "(SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `Name`, " +
                            "(SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `AvatarLevel`, " +
                            "(SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `TitleId`, " +
                            "(SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `ArmorId`, " +
                            "(SELECT `a`.`Cp` FROM `Avatars` AS `a` WHERE `a`.`Address` = `h`.`AvatarAddress` LIMIT 1) AS `Cp`, " +
                            "MIN(`h`.`BlockIndex`) AS `BlockIndex`, " +
                            "row_number() over(ORDER BY MAX(`h`.`StageId`) DESC, MIN(`h`.`BlockIndex`)) Ranking " +
                            "FROM `HackAndSlashes` AS `h` " +
                            $"WHERE (`h`.`Mimisbrunnr` = {isMimisbrunnr}) AND `h`.`Cleared` " +
                            "GROUP BY `h`.`AvatarAddress`");

            if (avatarAddress is { } avatarAddressNotNull)
            {
                query = query.Where(s => s.AvatarAddress == avatarAddressNotNull.ToString());
            }

            if (limit is { } limitNotNull)
            {
                query = query.Take(limitNotNull);
            }

            return query.ToList();
        }

        public void StoreCombinationConsumable(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            int recipeId,
            int slotIndex,
            long blockIndex)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            ctx.CombinationConsumables!.Add(
                new CombinationConsumableModel()
                {
                    Id = id.ToString(),
                    AgentAddress = agentAddress.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    RecipeId = recipeId,
                    SlotIndex = slotIndex,
                    BlockIndex = blockIndex,
                }
            );

            if (ctx.CraftRankings?.Find(avatarAddress.ToString()) is { } rankingData)
            {
                rankingData.CraftCount += 1;
                rankingData.BlockIndex = blockIndex;
            }
            else
            {
                ctx.CraftRankings!.Add(
                    new CraftRankingInputModel()
                    {
                        AgentAddress = agentAddress.ToString(),
                        AvatarAddress = avatarAddress.ToString(),
                        CraftCount = 1,
                        BlockIndex = blockIndex,
                    });
            }

            ctx.SaveChanges();
        }

        public void DeleteCombinationConsumable(Guid id)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();

            var consumableData = ctx.CombinationConsumables?.Find(id.ToString());
            if (consumableData is { } combinationConsumable)
            {
                if (ctx.CraftRankings?.Find(consumableData?.AvatarAddress) is { } rankingData)
                {
                    rankingData.CraftCount -= 1;
                }

                ctx.Remove(combinationConsumable);
            }

            ctx.SaveChanges();
        }

        public void StoreCombinationEquipment(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            int recipeId,
            int slotIndex,
            int? subRecipeId,
            long blockIndex)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            ctx.CombinationEquipments!.Add(
                new CombinationEquipmentModel()
                {
                    Id = id.ToString(),
                    AgentAddress = agentAddress.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    RecipeId = recipeId,
                    SlotIndex = slotIndex,
                    SubRecipeId = subRecipeId ?? 0,
                    BlockIndex = blockIndex,
                }
            );

            if (ctx.CraftRankings?.Find(avatarAddress.ToString()) is { } rankingData)
            {
                rankingData.CraftCount += 1;
                rankingData.BlockIndex = blockIndex;
            }
            else
            {
                ctx.CraftRankings!.Add(
                    new CraftRankingInputModel()
                    {
                        AgentAddress = agentAddress.ToString(),
                        AvatarAddress = avatarAddress.ToString(),
                        CraftCount = 1,
                        BlockIndex = blockIndex,
                    });
            }

            ctx.SaveChanges();
        }

        public void DeleteCombinationEquipment(Guid id)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            var equipmentData = ctx.CombinationEquipments?.Find(id.ToString());
            if (equipmentData is { } combinationEquipment)
            {
                if (ctx.CraftRankings?.Find(equipmentData?.AvatarAddress) is { } rankingData)
                {
                    rankingData.CraftCount -= 1;
                }

                ctx.Remove(combinationEquipment);
            }

            ctx.SaveChanges();
        }

        public void StoreItemEnhancement(
            Guid id,
            Address agentAddress,
            Address avatarAddress,
            Guid itemId,
            Guid materialId,
            int slotIndex,
            long blockIndex)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            ctx.ItemEnhancements!.Add(
                new ItemEnhancementModel()
                {
                    Id = id.ToString(),
                    AgentAddress = agentAddress.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    ItemId = itemId.ToString(),
                    MaterialId = materialId.ToString(),
                    SlotIndex = slotIndex,
                    BlockIndex = blockIndex,
                }
            );

            if (ctx.CraftRankings?.Find(avatarAddress.ToString()) is { } rankingData)
            {
                rankingData.CraftCount += 1;
                rankingData.BlockIndex = blockIndex;
            }
            else
            {
                ctx.CraftRankings!.Add(
                    new CraftRankingInputModel()
                    {
                        AgentAddress = agentAddress.ToString(),
                        AvatarAddress = avatarAddress.ToString(),
                        CraftCount = 1,
                        BlockIndex = blockIndex,
                    });
            }

            ctx.SaveChanges();
        }

        public void DeleteItemEnhancement(Guid id)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            var itemData = ctx.ItemEnhancements?.Find(id.ToString());
            if (itemData is { } itemEnhancement)
            {
                if (ctx.CraftRankings?.Find(itemData?.AvatarAddress) is { } rankingData)
                {
                    rankingData.CraftCount -= 1;
                }

                ctx.Remove(itemEnhancement);
            }

            ctx.SaveChanges();
        }

        public IEnumerable<CraftRankingOutputModel> GetCraftRanking(
            Address? avatarAddress = null,
            int? limit = null)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            var query = ctx.Set<CraftRankingOutputModel>()
                .FromSqlRaw("SELECT `h`.`AvatarAddress`, `AgentAddress`, `CraftCount`, `BlockIndex`, " +
                            "(SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`, " +
                            "(SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`, " +
                            "(SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`, " +
                            "(SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`, " +
                            "(SELECT `a`.`Cp` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Cp`, " +
                            "row_number() over(ORDER BY `CraftCount` DESC, `h`.`BlockIndex`) `Ranking` " +
                            "FROM `CraftRankings` AS `h` ");

            if (avatarAddress is { } avatarAddressNotNull)
            {
                query = query.Where(s => s.AvatarAddress == avatarAddressNotNull.ToString());
            }

            if (limit is { } limitNotNull)
            {
                query = query.Take(limitNotNull);
            }

            return query.ToList();
        }

        public void ProcessEquipment(
            Guid itemId,
            Address agentAddress,
            Address avatarAddress,
            int equipmentId,
            int cp,
            int level,
            ItemSubType itemSubType)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.Equipments?.Find(itemId.ToString()) is { } equipmentData)
            {
                equipmentData.AgentAddress = agentAddress.ToString();
                equipmentData.AvatarAddress = avatarAddress.ToString();
                equipmentData.Cp = cp;
                equipmentData.Level = level;
            }
            else
            {
                ctx.Equipments!.Add(
                    new EquipmentModel()
                    {
                        ItemId = itemId.ToString(),
                        AgentAddress = agentAddress.ToString(),
                        AvatarAddress = avatarAddress.ToString(),
                        EquipmentId = equipmentId,
                        Cp = cp,
                        Level = level,
                        ItemSubType = itemSubType.ToString(),
                    });
            }

            ctx.SaveChanges();
        }

        public IEnumerable<EquipmentRankingModel> GetEquipmentRanking(
            Address? avatarAddress = null,
            string? itemSubType = null,
            int? limit = null)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            IQueryable<EquipmentRankingModel>? query = null;

            if (itemSubType is { } itemSubTypeNotNull)
            {
                query = ctx.Set<EquipmentRankingModel>()
                    .FromSqlRaw("SELECT `ItemId`, `AvatarAddress`, `AgentAddress`, `EquipmentId`, `Cp`, `Level`, " +
                                 "`ItemSubType`, " +
                                 "(SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`, " +
                                 "(SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`, " +
                                 "(SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`, " +
                                 "(SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`, " +
                                 "ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) " +
                                 $"Ranking FROM `Equipments` where `ItemSubType` = \"{itemSubTypeNotNull}\" ");
            }
            else
            {
                query = ctx.Set<EquipmentRankingModel>()
                    .FromSqlRaw("SELECT `ItemId`, `AvatarAddress`, `AgentAddress`, `EquipmentId`, `Cp`, `Level`, " +
                                "`ItemSubType`, " +
                                "(SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`, " +
                                "(SELECT `a`.`AvatarLevel` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `AvatarLevel`, " +
                                "(SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`, " +
                                "(SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`, " +
                                "ROW_NUMBER() OVER(ORDER BY `Cp` DESC, `Level` DESC) " +
                                "Ranking FROM `Equipments` ");
            }

            if (avatarAddress is { } avatarAddressNotNull)
            {
                query = query.Where(s => s.AvatarAddress == avatarAddressNotNull.ToString());
            }

            if (limit is { } limitNotNull)
            {
                query = query.Take(limitNotNull);
            }

            return query.ToList();
        }

        public IEnumerable<AbilityRankingModel> GetAbilityRanking(
            Address? avatarAddress = null,
            int? limit = null)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            var query = ctx.Set<AbilityRankingModel>()
                .FromSqlRaw("SELECT `Address` as `AvatarAddress`, `AgentAddress`, `Name`, `TitleId`, `AvatarLevel`, `ArmorId`, `Cp`, " +
                            "row_number() over(ORDER BY `Cp` DESC) `Ranking` " +
                            "FROM `Avatars` ");

            if (avatarAddress is { } avatarAddressNotNull)
            {
                query = query.Where(s => s.AvatarAddress == avatarAddressNotNull.ToString());
            }

            if (limit is { } limitNotNull)
            {
                query = query.Take(limitNotNull);
            }

            return query.ToList();
        }
    }
}
