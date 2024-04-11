namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Model.Item;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class AuraSummonData
    {
        public static AuraSummonModel GetAuraSummonInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int groupId,
            int summonCount,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var prevAura = previousStates.GetAvatarState(avatarAddress).inventory.Equipments
                .Where(e => e.ItemSubType == ItemSubType.Aura).Select(e => e.ItemId);
            var gainedAura = string.Join(",", outputStates.GetAvatarState(avatarAddress).inventory.Equipments
                .Where(e => e.ItemSubType == ItemSubType.Aura && !prevAura.Contains(e.ItemId)).Select(e => e.Id));

            return new AuraSummonModel
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                GroupId = groupId,
                SummonCount = summonCount,
                SummonResult = gainedAura,
                BlockIndex = blockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime,
            };
        }

        public static AuraSummonFailModel GetAuraSummonFailInfo(
            IWorld previousStates,
            IWorld outputStates,
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
            return new AuraSummonFailModel
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
