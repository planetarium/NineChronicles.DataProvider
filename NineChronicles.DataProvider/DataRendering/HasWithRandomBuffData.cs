namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class HasWithRandomBuffData
    {
        public static HasWithRandomBuffModel GetHasWithRandomBuffInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            int stageId,
            int? stageBuffId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(avatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(stageId);

            var hasModel = new HasWithRandomBuffModel()
            {
                Id = actionId.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                StageId = stageId,
                BuffId = (int)stageBuffId!,
                Cleared = isClear,
                TimeStamp = blockTime,
            };

            return hasModel;
        }
    }
}
