namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Event;
    using NineChronicles.DataProvider.Store.Models;

    public static class HackAndSlashSweepData
    {
        public static HackAndSlashSweepModel GetHackAndSlashSweepInfo(
            ActionBase.ActionEvaluation<HackAndSlashSweep> ev,
            HackAndSlashSweep hasSweep,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep.avatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(hasSweep.stageId);

            var hasSweepModel = new HackAndSlashSweepModel()
            {
                Id = hasSweep.Id.ToString(),
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = hasSweep.avatarAddress.ToString(),
                WorldId = hasSweep.worldId,
                StageId = hasSweep.stageId,
                ApStoneCount = hasSweep.apStoneCount,
                ActionPoint = hasSweep.actionPoint,
                CostumesCount = hasSweep.costumes.Count,
                EquipmentsCount = hasSweep.equipments.Count,
                Cleared = isClear,
                Mimisbrunnr = hasSweep.stageId > 10000000,
                BlockIndex = ev.BlockIndex,
                Timestamp = blockTime,
            };

            return hasSweepModel;
        }
    }
}
