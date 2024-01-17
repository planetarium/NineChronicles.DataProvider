namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using NineChronicles.DataProvider.Store.Models;

    public static class HackAndSlashSweepData
    {
        public static HackAndSlashSweepModel GetHackAndSlashSweepInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int stageId,
            int worldId,
            int apStoneCount,
            int actionPoint,
            int costumesCount,
            int equipmentsCount,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarState(avatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(stageId);

            var hasSweepModel = new HackAndSlashSweepModel()
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                WorldId = worldId,
                StageId = stageId,
                ApStoneCount = apStoneCount,
                ActionPoint = actionPoint,
                CostumesCount = costumesCount,
                EquipmentsCount = equipmentsCount,
                Cleared = isClear,
                Mimisbrunnr = stageId > 10000000,
                BlockIndex = blockIndex,
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                Timestamp = blockTime,
            };

            return hasSweepModel;
        }

        public static HackAndSlashSweepModel GetHackAndSlashSweepInfoV1(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            int stageId,
            int worldId,
            int apStoneCount,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarState(avatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(stageId);

            var hasSweepModel = new HackAndSlashSweepModel()
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                WorldId = worldId,
                StageId = stageId,
                ApStoneCount = apStoneCount,
                ActionPoint = 0,
                CostumesCount = 0,
                EquipmentsCount = 0,
                Cleared = isClear,
                Mimisbrunnr = stageId > 10000000,
                BlockIndex = blockIndex,
                Timestamp = blockTime,
            };

            return hasSweepModel;
        }
    }
}
