namespace NineChronicles.DataProvider.DataRendering.Summon
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models.Summon;

    public static class CostumeSummonData
    {
        public static NineChronicles.DataProvider.Store.Models.Summon.CostumeSummonModel GetSummonInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            long blockIndex,
            System.DateTimeOffset blockTime,
            CostumeSummon action
        )
        {
            var prevCostume = previousStates.GetAvatarState(action.AvatarAddress).inventory.Equipments
                .Where(e => e.ItemSubType == ItemSubType.FullCostume).Select(e => e.ItemId);
            var gainedCostume = string.Join(",", outputStates.GetAvatarState(action.AvatarAddress).inventory.Equipments
                .Where(e => e.ItemSubType == ItemSubType.FullCostume && !prevCostume.Contains(e.ItemId))
                .Select(e => e.Id));

            return new CostumeSummonModel
            {
                Id = action.Id.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = action.AvatarAddress.ToString(),
                GroupId = action.GroupId,
                SummonCount = action.SummonCount,
                SummonResult = gainedCostume,
                BlockIndex = blockIndex,
                Date = System.DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }
    }
}
