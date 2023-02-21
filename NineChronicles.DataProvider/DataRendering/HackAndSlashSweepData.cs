namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using NineChronicles.DataProvider.Store.Models;

    public static class HackAndSlashSweepData
    {
        public static HackAndSlashSweepModel GetHackAndSlashSweepInfo(
            HackAndSlashSweep hasSweep,
            IAccountStateDelta previousStates,
            IAccountStateDelta outputStates,
            Address signer,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(hasSweep.avatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(hasSweep.stageId);

            var hasSweepModel = new HackAndSlashSweepModel()
            {
                Id = hasSweep.Id.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = hasSweep.avatarAddress.ToString(),
                WorldId = hasSweep.worldId,
                StageId = hasSweep.stageId,
                ApStoneCount = hasSweep.apStoneCount,
                ActionPoint = hasSweep.actionPoint,
                CostumesCount = hasSweep.costumes.Count,
                EquipmentsCount = hasSweep.equipments.Count,
                Cleared = isClear,
                Mimisbrunnr = hasSweep.stageId > 10000000,
                BlockIndex = blockIndex,
                Timestamp = blockTime,
            };

            return hasSweepModel;
        }
    }
}
