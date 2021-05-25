namespace NineChronicles.DataProvider.Store
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;
    using NineChronicles.DataProvider.Store.Models;

    public class MySqlStore
    {
        private readonly IDbContextFactory<NineChroniclesContext> _dbContextFactory;

        public MySqlStore(IDbContextFactory<NineChroniclesContext> contextFactory)
        {
            _dbContextFactory = contextFactory;
        }

        public void StoreAvatar(
            string address,
            string agentAddress,
            string name)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.Avatars?.Find(address) is null)
            {
                ctx.Avatars!.Add(
                    new AvatarModel()
                    {
                        Address = address,
                        AgentAddress = agentAddress,
                        Name = name,
                    }
                );
            }

            ctx.SaveChanges();
        }

        public void StoreAgent(string address)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.Agents?.Find(address) is null)
            {
                ctx.Agents!.Add(
                    new AgentModel()
                    {
                        Address = address,
                    }
                );
            }

            ctx.SaveChanges();
        }

        public void StoreHackAndSlash(
            string id,
            string agentAddress,
            string avatarAddress,
            int stageId,
            bool cleared,
            bool isMimisbrunnr)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            ctx.HackAndSlashes!.Add(
                new HackAndSlashModel()
                {
                    Id = id,
                    AgentAddress = agentAddress,
                    AvatarAddress = avatarAddress,
                    StageId = stageId,
                    Cleared = cleared,
                    Mimisbrunnr = isMimisbrunnr,
                }
            );
            ctx.SaveChanges();
        }

        public void DeleteHackAndSlash(string id)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.HackAndSlashes!.Find(id) is HackAndSlashModel has)
            {
                ctx.Remove(has);
            }

            ctx.SaveChanges();
        }

        public IEnumerable<HackAndSlashModel> GetHackAndSlash(
            string? agentAddress = null,
            int? limit = null)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<HackAndSlashModel> hackAndSlashes = ctx.HackAndSlashes!;

            if (agentAddress is { } agentAddressNotNull)
            {
                hackAndSlashes = hackAndSlashes.Where(has => has.AgentAddress == agentAddressNotNull);
            }

            if (limit is int limitNotNull)
            {
                hackAndSlashes = hackAndSlashes.Take(limitNotNull);
            }

            return hackAndSlashes.ToList();
        }

        public IEnumerable<StageRankingModel> GetStageRanking(
            string? avatarAddress = null,
            int? limit = null,
            bool isMimisbrunnr = false)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            IEnumerable<StageRankingModel>? query = avatarAddress != null ?
                ctx.Set<HackAndSlashModel>()
                    .AsQueryable()
                    .Where(has => has.Mimisbrunnr == isMimisbrunnr)
                    .Where(has => has.Cleared)
                    .Where(has => has.AvatarAddress == avatarAddress)
                    .Select(g => new StageRankingModel()
                    {
                        AvatarAddress = g.AvatarAddress!,
                        ClearedStageId = g.StageId,
                        Name = ctx.Avatars!.AsQueryable().Where(a => a.Address! == g.AvatarAddress).Select(a => a.Name!)
                            .Single(),
                    })
                    .OrderByDescending(r => r.ClearedStageId).Take(1) :
                ctx.Set<HackAndSlashModel>()
                    .AsQueryable()
                    .Where(has => has.Mimisbrunnr == isMimisbrunnr)
                    .Where(has => has.Cleared)
                    .GroupBy(has => has.AvatarAddress)
                    .Select(g => new StageRankingModel()
                    {
                        AvatarAddress = g.Key!,
                        ClearedStageId = g.Max(x => x.StageId),
                        Name = ctx.Avatars!.AsQueryable().Where(a => a.Address! == g.Key).Select(a => a.Name!).Single(),
                    })
                    .OrderByDescending(r => r.ClearedStageId);

            if (limit is int limitNotNull && avatarAddress is null)
            {
                query = query.Take(limitNotNull);
            }

            var queryList = query.ToList();
            if (queryList.Count > 0)
            {
                int rank = 1;
                for (int i = 0; i < queryList.Count; i++)
                {
                    var stageRankingModel = queryList[i];
                    stageRankingModel.Ranking = rank;
                    queryList[i] = stageRankingModel;
                    rank += 1;
                }
            }

            return queryList;
        }
    }
}
