namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Summon;
    using NineChronicles.DataProvider.Store.Models;

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
            IRandom random
        )
        {
            var simulateResult = RuneSummon.SimulateSummon(runeSheet, summonSheet[groupId], summonCount, random);
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
            };
        }

        public static RuneSummonFailModel GetRuneSummonFailInfo(
            Address signer,
            Address avatarAddress,
            int groupId,
            int summonCount,
            Guid actionId,
            long blockIndex,
            Exception exc
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
            };
        }
    }
}
