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
            bool isMimisbrunnr,
            long blockIndex)
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
                    BlockIndex = blockIndex,
                }
            );
            ctx.SaveChanges();
        }

        public void DeleteHackAndSlash(string id)
        {
            using NineChroniclesContext? ctx = _dbContextFactory.CreateDbContext();
            if (ctx.HackAndSlashes!.Find(id) is { } has)
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

            if (limit is { } limitNotNull)
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
            var query = ctx.Set<StageRankingModel>()
                .FromSqlRaw("SELECT `h`.`AvatarAddress`, `h`.`AgentAddress`, MAX(`h`.`StageId`) " +
                            "AS `ClearedStageId`, (SELECT `a`.`Name` " +
                            "FROM `Avatars` AS `a` " +
                            "WHERE `a`.`Address` = `h`.`AvatarAddress` " +
                            "LIMIT 1) AS `Name`, MIN(`h`.`BlockIndex`) AS `BlockIndex`, " +
                            "row_number() over(ORDER BY MAX(`h`.`StageId`) DESC, MIN(`h`.`BlockIndex`)) Ranking " +
                            "FROM `HackAndSlashes` AS `h` " +
                            $"WHERE (`h`.`Mimisbrunnr` = {isMimisbrunnr}) AND `h`.`Cleared` " +
                            "GROUP BY `h`.`AvatarAddress`");

            if (!(avatarAddress is null))
            {
                query = query.Where(s => s.AvatarAddress == avatarAddress);
            }

            if (limit is { } limitNotNull)
            {
                query = query.Take(limitNotNull);
            }

            return query.ToList();
        }
    }
}
