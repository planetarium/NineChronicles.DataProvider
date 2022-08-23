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
                using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
                    ctx.Dispose();
                }
                else
                {
                    ctx.Dispose();
                    using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
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
                    updateCtx.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreAvatarList(List<AvatarModel?> avatarList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var avatar in avatarList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Avatars?.Find(avatar!.Address) is null)
                        {
                            ctx.Avatars!.AddRange(avatar!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
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
                using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                if (ctx.Agents?.Find(address.ToString()) is null)
                {
                    ctx.Agents!.AddRange(
                        new AgentModel()
                        {
                            Address = address.ToString(),
                        }
                    );

                    ctx.SaveChanges();
                    ctx.Dispose();
                }
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreAgentList(List<AgentModel?> agentList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var agent in agentList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Agents?.Find(agent!.Address) is null)
                        {
                            ctx.Agents!.AddRange(agent!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
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
                using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
                ctx.Dispose();
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreHackAndSlashList(List<HackAndSlashModel?> hasList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var has in hasList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.HackAndSlashes?.Find(has!.Id) is null)
                        {
                            ctx.HackAndSlashes!.AddRange(has!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
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

        public void StoreHackAndSlashSweepList(List<HackAndSlashSweepModel> hasSweepList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var hasSweep in hasSweepList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.HackAndSlashSweeps?.Find(hasSweep.Id) is null)
                        {
                            ctx.HackAndSlashSweeps!.AddRange(hasSweep);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.HackAndSlashSweeps!.UpdateRange(hasSweep);
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

        public void StoreEventDungeonBattleList(List<EventDungeonBattleModel> eventDungeonBattleList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var eventDungeonBattle in eventDungeonBattleList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.EventDungeonBattles?.Find(eventDungeonBattle.Id) is null)
                        {
                            ctx.EventDungeonBattles!.AddRange(eventDungeonBattle);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.EventDungeonBattles!.UpdateRange(eventDungeonBattle);
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

        public void StoreEventConsumableItemCraftsList(List<EventConsumableItemCraftsModel> eventConsumableItemCraftsList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var eventConsumableItemCraft in eventConsumableItemCraftsList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.EventConsumableItemCrafts?.Find(eventConsumableItemCraft.Id) is null)
                        {
                            ctx.EventConsumableItemCrafts!.AddRange(eventConsumableItemCraft);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.EventConsumableItemCrafts!.UpdateRange(eventConsumableItemCraft);
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            if (ctx.HackAndSlashes?.Find(id.ToString()) is { } has)
            {
                ctx.Remove(has);
            }

            ctx.SaveChanges();
        }

        public IEnumerable<AgentModel> GetAgents(Address? agentAddress = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<AgentModel> agents = ctx.Agents!;

            if (agentAddress is { } agentAddressNotNull)
            {
                agents = agents
                    .Where(agent => agent.Address == agentAddressNotNull.ToString());
            }

            return agents.ToList();
        }

        public int GetAgentCount()
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<AgentModel> agents = ctx.Agents!;
            var agentCount = agents.Count();
            return agentCount;
        }

        public IEnumerable<AvatarModel> GetAvatars(Address? avatarAddress = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<AvatarModel> avatars = ctx.Avatars!;

            if (avatarAddress is { } avatarAddressNotNull)
            {
                avatars = avatars
                    .Where(avatar => avatar.Address == avatarAddressNotNull.ToString());
            }

            return avatars.ToList();
        }

        public int GetAvatarCount()
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<AvatarModel> avatars = ctx.Avatars!;
            var avatarCount = avatars.Count();
            return avatarCount;
        }

        public IEnumerable<ShopEquipmentModel> GetShopEquipments(Address? sellerAvatarAddress = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<ShopEquipmentModel> shopEquipments = ctx.ShopEquipments!;

            if (sellerAvatarAddress is { } sellerAvatarAddressNotNull)
            {
                shopEquipments = shopEquipments
                    .Where(shopEquipment => shopEquipment.SellerAvatarAddress == sellerAvatarAddressNotNull.ToString());
            }

            return shopEquipments.ToList();
        }

        public int GetShopEquipmentCount()
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<ShopEquipmentModel> shopEquipments = ctx.ShopEquipments!;
            var shopEquipmentCount = shopEquipments.Count();
            return shopEquipmentCount;
        }

        public IEnumerable<ShopConsumableModel> GetShopConsumables(Address? sellerAvatarAddress = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<ShopConsumableModel> shopConsumables = ctx.ShopConsumables!;

            if (sellerAvatarAddress is { } sellerAvatarAddressNotNull)
            {
                shopConsumables = shopConsumables
                    .Where(shopConsumable => shopConsumable.SellerAvatarAddress == sellerAvatarAddressNotNull.ToString());
            }

            return shopConsumables.ToList();
        }

        public int GetShopConsumableCount()
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<ShopConsumableModel> shopConsumables = ctx.ShopConsumables!;
            var shopConsumableCount = shopConsumables.Count();
            return shopConsumableCount;
        }

        public IEnumerable<ShopCostumeModel> GetShopCostumes(
            Address? sellerAvatarAddress = null,
            string? itemSubType = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<ShopCostumeModel> shopCostumes = ctx.ShopCostumes!;

            if (sellerAvatarAddress is { } sellerAvatarAddressNotNull)
            {
                shopCostumes = shopCostumes
                    .Where(shopCostume => shopCostume.SellerAvatarAddress == sellerAvatarAddressNotNull.ToString());
            }

            if (itemSubType is { } itemSubTypeNotNull)
            {
                shopCostumes = shopCostumes
                    .Where(shopCostume => shopCostume.ItemSubType == itemSubType);
            }

            return shopCostumes.ToList();
        }

        public int GetShopCostumeCount()
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<ShopCostumeModel> shopCostumes = ctx.ShopCostumes!;
            var shopCostumeCount = shopCostumes.Count();
            return shopCostumeCount;
        }

        public IEnumerable<ShopMaterialModel> GetShopMaterials(Address? sellerAvatarAddress = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<ShopMaterialModel> shopMaterials = ctx.ShopMaterials!;

            if (sellerAvatarAddress is { } sellerAvatarAddressNotNull)
            {
                shopMaterials = shopMaterials
                    .Where(shopMaterial => shopMaterial.SellerAvatarAddress == sellerAvatarAddressNotNull.ToString());
            }

            return shopMaterials.ToList();
        }

        public int GetShopMaterialCount()
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<ShopMaterialModel> shopMaterials = ctx.ShopMaterials!;
            var shopMaterialCount = shopMaterials.Count();
            return shopMaterialCount;
        }

        public IEnumerable<HackAndSlashModel> GetHackAndSlash(
            Address? agentAddress = null,
            int? limit = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
            ctx.Dispose();
        }

        public void StoreCombinationConsumableList(List<CombinationConsumableModel?> ccList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var cc in ccList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.CombinationConsumables?.Find(cc!.Id) is null)
                        {
                            try
                            {
                                ctx.CombinationConsumables!.AddRange(cc!);
                                ctx.SaveChanges();
                                ctx.Dispose();
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e.Message);
                            }
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.CombinationConsumables!.UpdateRange(cc);
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();

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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
            ctx.Dispose();
        }

        public void StoreCombinationEquipmentList(List<CombinationEquipmentModel?> ceList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var ce in ceList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.CombinationEquipments?.Find(ce!.Id) is null)
                        {
                            try
                            {
                                ctx.CombinationEquipments!.AddRange(ce!);
                                ctx.SaveChanges();
                                ctx.Dispose();
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e.Message);
                            }
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.CombinationEquipments!.UpdateRange(ce);
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

        public void StoreShopHistoryEquipmentList(List<ShopHistoryEquipmentModel?> seList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var se in seList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ShopHistoryEquipments?.Find(se!.OrderId) is null)
                        {
                            ctx.ShopHistoryEquipments!.AddRange(se!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ShopHistoryEquipments!.UpdateRange(se);
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

        public void StoreShopHistoryCostumeList(List<ShopHistoryCostumeModel?> sctList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var sct in sctList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ShopHistoryCostumes?.Find(sct!.OrderId) is null)
                        {
                            ctx.ShopHistoryCostumes!.AddRange(sct!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ShopHistoryCostumes!.UpdateRange(sct);
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

        public void StoreShopHistoryMaterialList(List<ShopHistoryMaterialModel?> smList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var sm in smList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ShopHistoryMaterials?.Find(sm!.OrderId) is null)
                        {
                            ctx.ShopHistoryMaterials!.AddRange(sm!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ShopHistoryMaterials!.UpdateRange(sm);
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

        public void StoreShopHistoryConsumableList(List<ShopHistoryConsumableModel?> scList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var sc in scList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ShopHistoryConsumables?.Find(sc!.OrderId) is null)
                        {
                            ctx.ShopHistoryConsumables!.AddRange(sc!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ShopHistoryConsumables!.UpdateRange(sc);
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
            ctx.Dispose();
        }

        public void StoreItemEnhancementList(List<ItemEnhancementModel?> ieList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var ie in ieList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ItemEnhancements?.Find(ie!.Id) is null)
                        {
                            try
                            {
                                ctx.ItemEnhancements!.AddRange(ie!);
                                ctx.SaveChanges();
                                ctx.Dispose();
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e.Message);
                            }
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ItemEnhancements!.UpdateRange(ie);
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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

        public void StoreStakingList(List<StakeModel> stakeList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var stake in stakeList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        ctx.Stakings!.AddRange(stake);
                        ctx.SaveChanges();
                        ctx.Dispose();
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreClaimStakeRewardList(List<ClaimStakeRewardModel> claimStakeList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var claimStake in claimStakeList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ClaimStakeRewards?.Find(claimStake!.Id) is null)
                        {
                            ctx.ClaimStakeRewards!.AddRange(claimStake);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ClaimStakeRewards!.UpdateRange(claimStake);
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

        public void StoreMigrateMonsterCollectionList(
            List<MigrateMonsterCollectionModel> mmcList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var mmc in mmcList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        ctx.MigrateMonsterCollections!.AddRange(mmc);
                        ctx.SaveChanges();
                        ctx.Dispose();
                    }));
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        public void StoreGrindList(List<GrindingModel> grindList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var grind in grindList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Grindings?.Find(grind!.Id) is null)
                        {
                            ctx.Grindings!.AddRange(grind);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Grindings!.UpdateRange(grind);
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

        public void StoreItemEnhancementFailList(List<ItemEnhancementFailModel> itemEnhancementFailList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var itemEnhancementFail in itemEnhancementFailList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ItemEnhancementFails?.Find(itemEnhancementFail!.Id) is null)
                        {
                            ctx.ItemEnhancementFails!.AddRange(itemEnhancementFail);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ItemEnhancementFails!.UpdateRange(itemEnhancementFail);
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

        public void StoreUnlockEquipmentRecipeList(List<UnlockEquipmentRecipeModel> unlockEquipmentRecipeList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var unlockEquipmentRecipe in unlockEquipmentRecipeList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.UnlockEquipmentRecipes?.Find(unlockEquipmentRecipe.Id) is null)
                        {
                            ctx.UnlockEquipmentRecipes!.AddRange(unlockEquipmentRecipe);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.UnlockEquipmentRecipes!.UpdateRange(unlockEquipmentRecipe);
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

        public void StoreUnlockWorldList(List<UnlockWorldModel> unlockWorldList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var unlockWorld in unlockWorldList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.UnlockWorlds?.Find(unlockWorld.Id) is null)
                        {
                            ctx.UnlockWorlds!.AddRange(unlockWorld);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.UnlockWorlds!.UpdateRange(unlockWorld);
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

        public void StoreReplaceCombinationEquipmentMaterialList(
            List<ReplaceCombinationEquipmentMaterialModel> replaceCombinationEquipmentMaterialList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var replaceCombinationEquipmentMaterial in replaceCombinationEquipmentMaterialList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.ReplaceCombinationEquipmentMaterials?.Find(replaceCombinationEquipmentMaterial.Id) is null)
                        {
                            ctx.ReplaceCombinationEquipmentMaterials!.AddRange(replaceCombinationEquipmentMaterial);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.ReplaceCombinationEquipmentMaterials!.UpdateRange(replaceCombinationEquipmentMaterial);
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

        public void StoreHasRandomBuffList(List<HasRandomBuffModel> hasRandomBuffList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var hasRandomBuff in hasRandomBuffList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.HasRandomBuffs?.Find(hasRandomBuff.Id) is null)
                        {
                            ctx.HasRandomBuffs!.AddRange(hasRandomBuff);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.HasRandomBuffs!.UpdateRange(hasRandomBuff);
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

        public void StoreHasWithRandomBuffList(List<HasWithRandomBuffModel> hasWithRandomBuffList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var hasWithRandomBuff in hasWithRandomBuffList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.HasWithRandomBuffs?.Find(hasWithRandomBuff.Id) is null)
                        {
                            ctx.HasWithRandomBuffs!.AddRange(hasWithRandomBuff);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.HasWithRandomBuffs!.UpdateRange(hasWithRandomBuff);
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

        public void StoreJoinArenaList(List<JoinArenaModel> joinArenaList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var joinArena in joinArenaList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.JoinArenas?.Find(joinArena.Id) is null)
                        {
                            ctx.JoinArenas!.AddRange(joinArena);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.JoinArenas!.UpdateRange(joinArena);
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

        public void StoreBattleArenaList(List<BattleArenaModel> battleArenaList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var battleArena in battleArenaList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        var i = ctx.BattleArenas?.Find(battleArena.Id);
                        if (i is null)
                        {
                            ctx.BattleArenas!.AddRange(battleArena);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.BattleArenas!.UpdateRange(battleArena);
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

        public void StoreBlockList(List<BlockModel> blockList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var block in blockList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Blocks?.Find(block.Hash) is null)
                        {
                            ctx.Blocks!.AddRange(block);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Blocks!.UpdateRange(block);
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

        public void StoreTransactionList(List<TransactionModel> transactionList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var transaction in transactionList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Transactions?.Find(transaction.TxId) is null)
                        {
                            ctx.Transactions!.AddRange(transaction);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Transactions!.UpdateRange(transaction);
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

        public IEnumerable<CraftRankingOutputModel> GetCraftRanking(
            Address? avatarAddress = null,
            int? limit = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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

        public IEnumerable<BattleArenaRankingModel> GetBattleArenaRanking(
            int championshipId,
            int round,
            string rankingType = "Score",
            int? limit = null,
            int? offset = null,
            Address? avatarAddress = null)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<BattleArenaRankingModel>? query = null;
            if (rankingType == "Medal" || rankingType == "medal" || rankingType == "m")
            {
                query = ctx.Set<BattleArenaRankingModel>()
                    .FromSqlRaw($@"SELECT `h`.`AvatarAddress`, `AgentAddress`, `AvatarLevel`, `BlockIndex`, `ChampionshipId`, 
                        `Round`, `ArenaType`, `Score`, `WinCount`, `MedalCount`, `LossCount`, `Ticket`, `PurchasedTicketCount`, 
                        `TicketResetCount`, `EntranceFee`, `TicketPrice`, `AdditionalTicketPrice`, `RequiredMedalCount`, 
                        `StartBlockIndex`, `EndBlockIndex`, `Timestamp`, 
                        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`, 
                        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`, 
                        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`, 
                        (SELECT `a`.`Cp` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Cp`, 
                        rank() over(ORDER BY `MedalCount` DESC) `Ranking` FROM `BattleArenaRanking_{championshipId}_{round}` AS `h` ");
            }
            else
            {
                query = ctx.Set<BattleArenaRankingModel>()
                    .FromSqlRaw($@"SELECT `h`.`AvatarAddress`, `AgentAddress`, `AvatarLevel`, `BlockIndex`, `ChampionshipId`, 
                        `Round`, `ArenaType`, `Score`, `WinCount`, `MedalCount`, `LossCount`, `Ticket`, `PurchasedTicketCount`, 
                        `TicketResetCount`, `EntranceFee`, `TicketPrice`, `AdditionalTicketPrice`, `RequiredMedalCount`, 
                        `StartBlockIndex`, `EndBlockIndex`, `Timestamp`, 
                        (SELECT `a`.`Name` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Name`, 
                        (SELECT `a`.`TitleId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `TitleId`, 
                        (SELECT `a`.`ArmorId` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `ArmorId`, 
                        (SELECT `a`.`Cp` FROM `Avatars` AS `a` WHERE `a`.`Address` = `AvatarAddress` LIMIT 1) AS `Cp`, 
                        rank() over(ORDER BY `Score` DESC) `Ranking` FROM `BattleArenaRanking_{championshipId}_{round}` AS `h` ");
            }

            if (avatarAddress is { } avatarAddressNotNull)
            {
                query = query.Where(s => s.AvatarAddress == avatarAddressNotNull.ToString());
            }
            else
            {
                if (offset is { } offsetNotNull)
                {
                    query = query.Skip(offsetNotNull);
                }
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();

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
            ctx.Dispose();
        }

        public void ProcessEquipmentList(List<EquipmentModel?> eqList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var eq in eqList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Equipments?.Find(eq!.ItemId) is null)
                        {
                            ctx.Equipments!.AddRange(eq!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Equipments!.UpdateRange(eq!);
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
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

        public IEnumerable<AgentModel> GetDau(string date)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            var query = ctx.Set<AgentModel>()
                .FromSqlRaw($"SELECT Signer as Address FROM `Transactions` WHERE Date = \"{date}\" GROUP BY Signer");

            return query.ToList();
        }
    }
}
