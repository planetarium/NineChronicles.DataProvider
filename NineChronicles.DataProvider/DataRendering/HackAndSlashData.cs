namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class HackAndSlashData
    {
        public static HackAndSlashModel GetHackAndSlashInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int stageId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarState(avatarAddress);
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
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                Timestamp = blockTime,
            };

            return hasModel;
        }
    }
}
