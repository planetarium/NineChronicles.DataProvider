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

        public void StoreAvatarList(List<AvatarModel?> avatarList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var avatar in avatarList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Avatars?.Find(avatar!.Address) is null)
                        {
                            ctx.Avatars!.AddRange(avatar!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Avatars!.UpdateRange(avatar!);
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

        public void StoreAgentList(List<AgentModel?> agentList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var agent in agentList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.Agents?.Find(agent!.Address) is null)
                        {
                            ctx.Agents!.AddRange(agent!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.Agents!.UpdateRange(agent!);
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

        public void StoreWorldBossList(List<WorldBossModel> worldBossList)
        {
            try
            {
                var tasks = new List<Task>();
                foreach (var worldBoss in worldBossList)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
                        if (ctx.WorldBosses?.Find(worldBoss!.Id) is null)
                        {
                            ctx.WorldBosses!.AddRange(worldBoss!);
                            ctx.SaveChanges();
                            ctx.Dispose();
                        }
                        else
                        {
                            ctx.Dispose();
                            using NineChroniclesContext? updateCtx = _dbContextFactory.CreateDbContext();
                            updateCtx.WorldBosses!.UpdateRange(worldBoss!);
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

        public void DeleteWorldBoss(Guid id)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.WorldBosses?.Find(id.ToString()) is { } has)
            {
                ctx.Remove(has);
            }

            ctx.SaveChanges();
        }

        public IEnumerable<WorldBossModel> GetWorldBosses(
            Address? avatarAddress = null,
            int? limit = null)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<WorldBossModel> worldBosses = ctx.WorldBosses!;

            if (avatarAddress is { } avatarAddressNotNull)
            {
                worldBosses = worldBosses
                    .Where(has => has.AgentAddress == avatarAddressNotNull.ToString());
            }

            if (limit is { } limitNotNull)
            {
                worldBosses = worldBosses.Take(limitNotNull);
            }

            return worldBosses.ToList();
        }
    }
}
