namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class HackAndSlashData
    {
        public static HackAndSlashModel GetHackAndSlashInfo(
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            Address avatarAddress,
            int stageId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(avatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(stageId);

            var hasModel = new HackAndSlashModel()
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                StageId = stageId,
                Cleared = isClear,
                Mimisbrunnr = stageId > 10000000,
                BlockIndex = blockIndex,
            };

            return hasModel;
        }
    }
}
