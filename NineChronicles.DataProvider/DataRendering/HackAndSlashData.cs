namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
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

            var hasModel = new HackAndSlashModel()
            {
                Id = has.Id.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = has.AvatarAddress.ToString(),
                StageId = has.StageId,
                Cleared = isClear,
                Mimisbrunnr = has.StageId > 10000000,
                BlockIndex = blockIndex,
            };

            return hasModel;
        }
    }
}
