namespace NineChronicles.DataProvider.DataRendering.Summon
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Rune;
    using Nekoyume.TableData.Summon;
    using NineChronicles.DataProvider.Store.Models.Summon;

    public static class RuneSummonData
    {
        public static RuneSummonModel GetRuneSummonInfo(
            Address signer,
            Address avatarAddress,
            int groupId,
            int summonCount,
            Guid actionId,
            long blockIndex,
            RuneSheet runeSheet,
            SummonSheet summonSheet,
            IRandom random,
            DateTimeOffset blockTime,
            RuneListSheet runeListSheet
        )
        {
            var simulateResult = RuneSummon.SimulateSummon(runeSheet, summonSheet[groupId], summonCount, random, runeListSheet);
            var gainedRunes = new List<FungibleAssetValue>();
            foreach (var pair in simulateResult)
            {
                gainedRunes.Add(pair.Key * pair.Value);
            }

            var result = string.Join(",", gainedRunes.Select(r => r.ToString()));
            return new RuneSummonModel
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                GroupId = groupId,
                SummonCount = summonCount,
                SummonResult = result,
                BlockIndex = blockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }

        public static RuneSummonFailModel GetRuneSummonFailInfo(
            Address signer,
            Address avatarAddress,
            int groupId,
            int summonCount,
            Guid actionId,
            long blockIndex,
            Exception exc,
            DateTimeOffset blockTime
        )
        {
            return new RuneSummonFailModel
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                GroupId = groupId,
                SummonCount = summonCount,
                BlockIndex = blockIndex,
                Exception = exc.ToString(),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
