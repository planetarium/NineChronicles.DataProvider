namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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
                    ctx.Avatars!.AddRange(
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
                        updateCtx.Avatars!.UpdateRange(
                            new AvatarModel()
                            {
                                Address = address.ToString(),
                                AgentAddress = agentAddress.ToString(),
                                Name = name,
                            }
                        );
                    }
                    else
                    {
                        updateCtx.Avatars!.UpdateRange(
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
                    }

                    updateCtx.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreAvatarList(List<AvatarModel> avatarList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var avatar in avatarList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Avatars?.Find(avatar.Address) is null)
                        {
                            ctx.Avatars!.AddRange(avatar);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Avatars!.UpdateRange(avatar);
                            updateCtx.SaveChanges();
                            updateCtx.Dispose();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreAgent(Address address)
        {
            try
            {
                using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                if (ctx.Agents?.Find(address.ToString()) is null)
                {
                    ctx.Agents!.AddRange(
                        new AgentModel()
                        {
                            Address = address.ToString(),
                        }
                    );

                    ctx.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreAgentList(List<AgentModel> agentList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var agent in agentList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Agents?.Find(agent.Address) is null)
                        {
                            ctx.Agents!.AddRange(agent);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Agents!.UpdateRange(agent);
                            updateCtx.SaveChanges();
                            updateCtx.Dispose();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
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
            try
            {
                using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                ctx.HackAndSlashes!.AddRange(
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
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreHackAndSlashList(List<HackAndSlashModel> hasList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var has in hasList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.HackAndSlashes?.Find(has.Id) is null)
                        {
                            ctx.HackAndSlashes!.AddRange(has);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.HackAndSlashes!.UpdateRange(has);
                            updateCtx.SaveChanges();
                            updateCtx.Dispose();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
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
            IQueryable<StageRankingModel>? query = null;
            if (!isMimisbrunnr)
            {
                query = ctx.Set<StageRankingModel>()
                    .FromSqlRaw("SELECT * FROM data_provider.StageRanking ORDER BY Ranking ");
            }
            else
            {
                query = ctx.Set<StageRankingModel>()
                    .FromSqlRaw("SELECT * FROM data_provider.StageRankingMimisbrunnr ORDER BY Ranking ");
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

            if (ctx.CraftRankings?.FindAsync(avatarAddress.ToString()).Result is { } rankingData)
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

            ctx.SaveChangesAsync();
        }

        public void StoreCombinationConsumableList(List<CombinationConsumableModel> ccList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var cc in ccList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.CombinationConsumables?.Find(cc.Id) is null)
                        {
                            ctx.CombinationConsumables!.AddRange(cc);
                            if (ctx.CraftRankings?.Find(cc.AvatarAddress) is { } rankingData)
                            {
                                rankingData.CraftCount += 1;
                                rankingData.BlockIndex = cc.BlockIndex;
                            }
                            else
                            {
                                ctx.CraftRankings!.Add(
                                    new CraftRankingInputModel()
                                    {
                                        AgentAddress = cc.AgentAddress,
                                        AvatarAddress = cc.AvatarAddress,
                                        CraftCount = 1,
                                        BlockIndex = cc.BlockIndex,
                                    });
                            }

                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.CombinationConsumables!.UpdateRange(cc);
                            if (updateCtx.CraftRankings?.Find(cc.AvatarAddress) is { } rankingData)
                            {
                                rankingData.CraftCount += 1;
                                rankingData.BlockIndex = cc.BlockIndex;
                            }
                            else
                            {
                                updateCtx.CraftRankings!.Add(
                                    new CraftRankingInputModel()
                                    {
                                        AgentAddress = cc.AgentAddress,
                                        AvatarAddress = cc.AvatarAddress,
                                        CraftCount = 1,
                                        BlockIndex = cc.BlockIndex,
                                    });
                            }

                            updateCtx.SaveChanges();
                            updateCtx.Dispose();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
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

            if (ctx.CraftRankings?.FindAsync(avatarAddress.ToString()).Result is { } rankingData)
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

            ctx.SaveChangesAsync();
        }

        public void StoreCombinationEquipmentList(List<CombinationEquipmentModel> ceList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var ce in ceList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.CombinationEquipments?.Find(ce.Id) is null)
                        {
                            ctx.CombinationEquipments!.AddRange(ce);
                            if (ctx.CraftRankings?.Find(ce.AvatarAddress) is { } rankingData)
                            {
                                rankingData.CraftCount += 1;
                                rankingData.BlockIndex = ce.BlockIndex;
                            }
                            else
                            {
                                ctx.CraftRankings!.Add(
                                    new CraftRankingInputModel()
                                    {
                                        AgentAddress = ce.AgentAddress,
                                        AvatarAddress = ce.AvatarAddress,
                                        CraftCount = 1,
                                        BlockIndex = ce.BlockIndex,
                                    });
                            }

                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.CombinationEquipments!.UpdateRange(ce);
                            if (updateCtx.CraftRankings?.Find(ce.AvatarAddress) is { } rankingData)
                            {
                                rankingData.CraftCount += 1;
                                rankingData.BlockIndex = ce.BlockIndex;
                            }
                            else
                            {
                                updateCtx.CraftRankings!.Add(
                                    new CraftRankingInputModel()
                                    {
                                        AgentAddress = ce.AgentAddress,
                                        AvatarAddress = ce.AvatarAddress,
                                        CraftCount = 1,
                                        BlockIndex = ce.BlockIndex,
                                    });
                            }

                            updateCtx.SaveChanges();
                            updateCtx.Dispose();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
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

            if (ctx.CraftRankings?.FindAsync(avatarAddress.ToString()).Result is { } rankingData)
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

            ctx.SaveChangesAsync();
        }

        public void StoreItemEnhancementList(List<ItemEnhancementModel> ieList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var ie in ieList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ItemEnhancements?.Find(ie.Id) is null)
                        {
                            ctx.ItemEnhancements!.AddRange(ie);
                            if (ctx.CraftRankings?.Find(ie.AvatarAddress) is { } rankingData)
                            {
                                rankingData.CraftCount += 1;
                                rankingData.BlockIndex = ie.BlockIndex;
                            }
                            else
                            {
                                ctx.CraftRankings!.Add(
                                    new CraftRankingInputModel()
                                    {
                                        AgentAddress = ie.AgentAddress,
                                        AvatarAddress = ie.AvatarAddress,
                                        CraftCount = 1,
                                        BlockIndex = ie.BlockIndex,
                                    });
                            }

                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ItemEnhancements!.UpdateRange(ie);
                            if (updateCtx.CraftRankings?.Find(ie.AvatarAddress) is { } rankingData)
                            {
                                rankingData.CraftCount += 1;
                                rankingData.BlockIndex = ie.BlockIndex;
                            }
                            else
                            {
                                updateCtx.CraftRankings!.Add(
                                    new CraftRankingInputModel()
                                    {
                                        AgentAddress = ie.AgentAddress,
                                        AvatarAddress = ie.AvatarAddress,
                                        CraftCount = 1,
                                        BlockIndex = ie.BlockIndex,
                                    });
                            }

                            updateCtx.SaveChanges();
                            updateCtx.Dispose();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
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

            ctx.SaveChanges();
        }

        public void ProcessEquipmentList(List<EquipmentModel> eqList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var eq in eqList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Equipments?.Find(eq.ItemId) is null)
                        {
                            ctx.Equipments!.AddRange(eq);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Equipments!.UpdateRange(eq);
                            updateCtx.SaveChanges();
                            updateCtx.Dispose();
                        }
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
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
                    .FromSqlRaw($"SELECT * FROM EquipmentRanking{itemSubTypeNotNull} ORDER BY Ranking ");
            }
            else
            {
                query = ctx.Set<EquipmentRankingModel>()
                    .FromSqlRaw("SELECT * FROM EquipmentRanking ORDER BY Ranking ");
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
