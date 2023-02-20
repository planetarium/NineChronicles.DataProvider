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

    public static class HackAndSlashData
    {
        public static HackAndSlashModel GetHackAndSlashInfo(
            ActionBase.ActionEvaluation<HackAndSlash> ev,
            HackAndSlash has,
            DateTimeOffset blockTime
        )
        {
            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(has.AvatarAddress);
            bool isClear = avatarState.stageMap.ContainsKey(has.StageId);

            var hasModel = new HackAndSlashModel()
            {
                Id = has.Id.ToString(),
                AgentAddress = ev.Signer.ToString(),
                AvatarAddress = has.AvatarAddress.ToString(),
                StageId = has.StageId,
                Cleared = isClear,
                Mimisbrunnr = has.StageId > 10000000,
                BlockIndex = ev.BlockIndex,
            };

            return hasModel;
        }
    }
}
