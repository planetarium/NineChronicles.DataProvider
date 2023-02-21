namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class HasWithRandomBuffData
    {
        public static HasWithRandomBuffModel GetHasWithRandomBuffInfo(
            HackAndSlash has,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(has.AvatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(has.StageId);

            var hasModel = new HasWithRandomBuffModel()
            {
                Id = has.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                AvatarAddress = has.AvatarAddress.ToString(),
                StageId = has.StageId,
                BuffId = (int)has.StageBuffId!,
                Cleared = isClear,
                TimeStamp = blockTime,
            };

            return hasModel;
        }
    }
}
