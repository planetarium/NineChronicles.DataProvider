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
            DateTimeOffset timestamp,
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
                            Timestamp = timestamp,
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
                                Timestamp = timestamp,
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
                                Timestamp = timestamp,
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.Avatars?.FindAsync(avatar!.Address).Result is null)
                        {
                            await ctx.Avatars!.AddRangeAsync(avatar!);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.Avatars!.UpdateRange(avatar);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.Agents?.FindAsync(agent!.Address).Result is null)
                        {
                            await ctx.Agents!.AddRangeAsync(agent!);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.Agents!.UpdateRange(agent);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.HackAndSlashes?.FindAsync(has!.Id).Result is null)
                        {
                            await ctx.HackAndSlashes!.AddRangeAsync(has!);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.HackAndSlashes!.UpdateRange(has);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.HackAndSlashSweeps?.FindAsync(hasSweep.Id).Result is null)
                        {
                            await ctx.HackAndSlashSweeps!.AddRangeAsync(hasSweep);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.HackAndSlashSweeps!.UpdateRange(hasSweep);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.EventDungeonBattles?.FindAsync(eventDungeonBattle.Id).Result is null)
                        {
                            await ctx.EventDungeonBattles!.AddRangeAsync(eventDungeonBattle);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.EventDungeonBattles!.UpdateRange(eventDungeonBattle);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.EventConsumableItemCrafts?.FindAsync(eventConsumableItemCraft.Id).Result is null)
                        {
                            await ctx.EventConsumableItemCrafts!.AddRangeAsync(eventConsumableItemCraft);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.EventConsumableItemCrafts!.UpdateRange(eventConsumableItemCraft);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.CombinationConsumables?.FindAsync(cc!.Id).Result is null)
                        {
                            try
                            {
                                await ctx.CombinationConsumables!.AddRangeAsync(cc!);
                                await ctx.SaveChangesAsync();
                                await ctx.DisposeAsync();
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e.Message);
                            }
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.CombinationConsumables!.UpdateRange(cc);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.CombinationEquipments?.FindAsync(ce!.Id).Result is null)
                        {
                            try
                            {
                                await ctx.CombinationEquipments!.AddRangeAsync(ce!);
                                await ctx.SaveChangesAsync();
                                await ctx.DisposeAsync();
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e.Message);
                            }
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.CombinationEquipments!.UpdateRange(ce);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.ShopHistoryEquipments?.FindAsync(se!.OrderId).Result is null)
                        {
                            await ctx.ShopHistoryEquipments!.AddRangeAsync(se!);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.ShopHistoryEquipments!.UpdateRange(se);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.ShopHistoryMaterials?.FindAsync(sm!.OrderId).Result is null)
                        {
                            await ctx.ShopHistoryMaterials!.AddRangeAsync(sm!);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.ShopHistoryMaterials!.UpdateRange(sm);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.ShopHistoryConsumables?.FindAsync(sc!.OrderId).Result is null)
                        {
                            await ctx.ShopHistoryConsumables!.AddRangeAsync(sc!);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.ShopHistoryConsumables!.UpdateRange(sc);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
            decimal burntNCG,
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
                    BurntNCG = burntNCG,
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.ItemEnhancements?.FindAsync(ie!.Id).Result is null)
                        {
                            try
                            {
                                await ctx.ItemEnhancements!.AddRangeAsync(ie!);
                                await ctx.SaveChangesAsync();
                                await ctx.DisposeAsync();
                            }
                            catch (Exception e)
                            {
                                Log.Debug(e.Message);
                            }
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.ItemEnhancements!.UpdateRange(ie);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        await ctx.Stakings!.AddRangeAsync(stake);
                        await ctx.SaveChangesAsync();
                        await ctx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.ClaimStakeRewards?.FindAsync(claimStake.Id).Result is null)
                        {
                            await ctx.ClaimStakeRewards!.AddRangeAsync(claimStake);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.ClaimStakeRewards!.UpdateRange(claimStake);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        await ctx.MigrateMonsterCollections!.AddRangeAsync(mmc);
                        await ctx.SaveChangesAsync();
                        await ctx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.Grindings?.FindAsync(grind.Id).Result is null)
                        {
                            await ctx.Grindings!.AddRangeAsync(grind);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.Grindings!.UpdateRange(grind);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.ItemEnhancementFails?.FindAsync(itemEnhancementFail.Id).Result is null)
                        {
                            await ctx.ItemEnhancementFails!.AddRangeAsync(itemEnhancementFail);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.ItemEnhancementFails!.UpdateRange(itemEnhancementFail);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.UnlockEquipmentRecipes?.FindAsync(unlockEquipmentRecipe.Id).Result is null)
                        {
                            await ctx.UnlockEquipmentRecipes!.AddRangeAsync(unlockEquipmentRecipe);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.UnlockEquipmentRecipes!.UpdateRange(unlockEquipmentRecipe);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.UnlockWorlds?.FindAsync(unlockWorld.Id).Result is null)
                        {
                            await ctx.UnlockWorlds!.AddRangeAsync(unlockWorld);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.UnlockWorlds!.UpdateRange(unlockWorld);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.ReplaceCombinationEquipmentMaterials?.FindAsync(replaceCombinationEquipmentMaterial.Id).Result is null)
                        {
                            await ctx.ReplaceCombinationEquipmentMaterials!.AddRangeAsync(replaceCombinationEquipmentMaterial);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.ReplaceCombinationEquipmentMaterials!.UpdateRange(replaceCombinationEquipmentMaterial);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.HasRandomBuffs?.FindAsync(hasRandomBuff.Id).Result is null)
                        {
                            await ctx.HasRandomBuffs!.AddRangeAsync(hasRandomBuff);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.HasRandomBuffs!.UpdateRange(hasRandomBuff);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.HasWithRandomBuffs?.FindAsync(hasWithRandomBuff.Id).Result is null)
                        {
                            await ctx.HasWithRandomBuffs!.AddRangeAsync(hasWithRandomBuff);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.HasWithRandomBuffs!.UpdateRange(hasWithRandomBuff);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.JoinArenas?.FindAsync(joinArena.Id).Result is null)
                        {
                            await ctx.JoinArenas!.AddRangeAsync(joinArena);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.JoinArenas!.UpdateRange(joinArena);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.BattleArenas?.FindAsync(battleArena.Id).Result is null)
                        {
                            await ctx.BattleArenas!.AddRangeAsync(battleArena);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.BattleArenas!.UpdateRange(battleArena);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreRaiderList(List<RaiderModel> raiderList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var raider in raiderList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.Raiders.FirstOrDefaultAsync(r =>
                                r.RaidId == raider.RaidId && r.Address.Equals(raider.Address)).Result is null)
                        {
                            await ctx.Raiders!.AddRangeAsync(raider);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.Raiders.UpdateRange(raider);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreBattleGrandFinaleList(List<BattleGrandFinaleModel> battleGrandFinaleList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var battleGrandFinale in battleGrandFinaleList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.BattleGrandFinales.FindAsync(battleGrandFinale.Id).Result is null)
                        {
                            await ctx.BattleGrandFinales.AddRangeAsync(battleGrandFinale);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.BattleGrandFinales.UpdateRange(battleGrandFinale);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreEventMaterialItemCraftsList(List<EventMaterialItemCraftsModel> eventMaterialItemCraftsList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var eventMaterialItemCrafts in eventMaterialItemCraftsList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.EventMaterialItemCrafts.FindAsync(eventMaterialItemCrafts.Id).Result is null)
                        {
                            await ctx.EventMaterialItemCrafts.AddRangeAsync(eventMaterialItemCrafts);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.EventMaterialItemCrafts.UpdateRange(eventMaterialItemCrafts);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreRuneEnhancementList(List<RuneEnhancementModel> runeEnhancementList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var runeEnhancement in runeEnhancementList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.RuneEnhancements.FindAsync(runeEnhancement.Id).Result is null)
                        {
                            await ctx.RuneEnhancements.AddRangeAsync(runeEnhancement);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.RuneEnhancements.UpdateRange(runeEnhancement);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreRunesAcquiredList(List<RunesAcquiredModel> runesAcquiredList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var runesAcquired in runesAcquiredList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.RunesAcquired.FindAsync(runesAcquired.Id, runesAcquired.TickerType).Result is null)
                        {
                            await ctx.RunesAcquired.AddRangeAsync(runesAcquired);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.RunesAcquired.UpdateRange(runesAcquired);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreUnlockRuneSlotList(List<UnlockRuneSlotModel> unlockRuneSlotList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var unlockRuneSlot in unlockRuneSlotList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.UnlockRuneSlots.FindAsync(unlockRuneSlot.Id).Result is null)
                        {
                            await ctx.UnlockRuneSlots.AddRangeAsync(unlockRuneSlot);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.UnlockRuneSlots.UpdateRange(unlockRuneSlot);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreRapidCombinationList(List<RapidCombinationModel> rapidCombinationList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var rapidCombination in rapidCombinationList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.RapidCombinations.FindAsync(rapidCombination.Id).Result is null)
                        {
                            await ctx.RapidCombinations.AddRangeAsync(rapidCombination);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.RapidCombinations.UpdateRange(rapidCombination);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public List<RaiderModel> GetRaiderList()
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            IQueryable<RaiderModel> raiders = ctx.Raiders!;
            return raiders.ToList();
        }

        public void StoreBlockList(List<BlockModel> blockList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var block in blockList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.Blocks.FindAsync(block.Hash).Result is null)
                        {
                            await ctx.Blocks.AddRangeAsync(block);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.Blocks.UpdateRange(block);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.Transactions?.FindAsync(transaction.TxId).Result is null)
                        {
                            await ctx.Transactions!.AddRangeAsync(transaction);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.Transactions!.UpdateRange(transaction);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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
                .FromSqlRaw("SELECT * FROM CraftRankings ORDER BY Ranking ");

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
                    tasks.Add(Task.Run(async () =>
                    {
                        await using NineChroniclesContext ctx = await _dbContextFactory.CreateDbContextAsync();
                        if (ctx.Equipments?.FindAsync(eq!.ItemId).Result is null)
                        {
                            await ctx.Equipments!.AddRangeAsync(eq!);
                            await ctx.SaveChangesAsync();
                            await ctx.DisposeAsync();
                        }
                        else
                        {
                            await ctx.DisposeAsync();
                            await using NineChroniclesContext updateCtx = await _dbContextFactory.CreateDbContextAsync();
                            updateCtx.Equipments!.UpdateRange(eq);
                            await updateCtx.SaveChangesAsync();
                            await updateCtx.DisposeAsync();
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

        public void StoreRaider(RaiderModel model)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            RaiderModel? prevModel =
                ctx.Raiders.FirstOrDefault(r => r.RaidId == model.RaidId && r.Address.Equals(model.Address));
            if (prevModel is null)
            {
                ctx.Raiders.Add(model);
            }
            else
            {
                prevModel.Cp = model.Cp;
                prevModel.IconId = model.IconId;
                prevModel.HighScore = model.HighScore;
                prevModel.TotalScore = model.TotalScore;
                prevModel.Level = model.Level;
                prevModel.PurchaseCount = model.PurchaseCount;
                ctx.Raiders.Update(prevModel);
            }

            ctx.SaveChanges();
        }

        public void StoreWorldBossMigration(int raidId)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            WorldBossSeasonMigrationModel model = new WorldBossSeasonMigrationModel
            {
                RaidId = raidId,
            };
            ctx.WorldBossSeasonMigrationModels.Add(model);
            ctx.SaveChanges();
        }

        public List<WorldBossRankingModel> GetWorldBossRanking(int raidId, int? queryOffset, int? queryLimit)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            var query = ctx.Set<WorldBossRankingModel>()
                .FromSqlRaw(@"SELECT `AvatarName`, `HighScore`, `TotalScore`, `Cp`, `Level`, `Address`, `IconId`, row_number() over(ORDER BY `TotalScore` DESC) as `Ranking` FROM `Raiders` WHERE `RaidId` = {0}", raidId);
            if (queryOffset.HasValue)
            {
                query = query.Skip(queryOffset.Value);
            }

            if (queryLimit.HasValue)
            {
                query = query.Take(queryLimit.Value);
            }

            return query.ToList();
        }

        public int GetTotalRaiders(int raidId)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            return ctx.Raiders.Count(r => r.RaidId == raidId);
        }

        public long GetTip()
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            return ctx.Blocks.Select(i => i.Index).OrderByDescending(i => i).First();
        }

        public bool MigrationExists(int raidId)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            return ctx.WorldBossSeasonMigrationModels.Any(m => m.RaidId == raidId);
        }

        public void UpsertRaiders(List<RaiderModel> raiderModels)
        {
            using NineChroniclesContext ctx = _dbContextFactory.CreateDbContext();
            foreach (var model in raiderModels)
            {
                RaiderModel? prevModel =
                    ctx.Raiders.FirstOrDefault(r => r.RaidId == model.RaidId && r.Address.Equals(model.Address));
                if (prevModel is null)
                {
                    ctx.Raiders.Add(model);
                }
                else
                {
                    prevModel.Cp = model.Cp;
                    prevModel.IconId = model.IconId;
                    prevModel.HighScore = model.HighScore;
                    prevModel.TotalScore = model.TotalScore;
                    prevModel.Level = model.Level;
                    prevModel.PurchaseCount = model.PurchaseCount;
                    ctx.Raiders.Update(prevModel);
                }
            }

            ctx.SaveChanges();
        }
    }
}
