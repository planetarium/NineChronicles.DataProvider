namespace NineChronicles.DataProvider.DataRendering.Summon
{
    using System.Linq;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using NineChronicles.DataProvider.Store.Models.Summon;

    public static class CostumeSummonData
    {
        public static NineChronicles.DataProvider.Store.Models.Summon.CostumeSummonModel GetSummonInfo(
            Address signer,
            long blockIndex,
            System.DateTimeOffset blockTime,
            Nekoyume.TableData.CostumeItemSheet sheet,
            Nekoyume.TableData.Summon.SummonSheet.Row row,
            Libplanet.Action.IRandom random,
            CostumeSummon action
        )
        {
            var costumeData = new System.Collections.Generic.Dictionary<int, int>();
            var costumes = CostumeSummon.SimulateSummon(
                $"{signer}:{action.AvatarAddress}", sheet, row, action.SummonCount, random
            );

            foreach (var c in costumes)
            {
                if (costumeData.ContainsKey(c.Id))
                {
                    costumeData[c.Id]++;
                }
                else
                {
                    costumeData[c.Id] = 1;
                }
            }

            var summonList = costumeData.Select(d => $"{d.Key}:{d.Value}").ToList();

            return new CostumeSummonModel
            {
                Id = action.Id.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = action.AvatarAddress.ToString(),
                GroupId = action.GroupId,
                SummonCount = action.SummonCount,
                SummonResult = string.Join(",", summonList),
                BlockIndex = blockIndex,
                Date = System.DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
