namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using NineChronicles.DataProvider.Store.Models;

    public static class EventMaterialItemCraftsData
    {
        public static EventMaterialItemCraftsModel GetEventMaterialItemCraftsInfo(
            IWorld previousStates,
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            Dictionary<int, int> materialsToUse,
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            Dictionary<string, int> materialData = new Dictionary<string, int>();
            for (var i = 1; i < 13; i++)
            {
                materialData.Add($"material{i}Id", 0);
                materialData.Add($"material{i}Count", 0);
            }

            int itemNumber = 1;
            foreach (var pair in materialsToUse)
            {
                materialData[$"material{itemNumber}Id"] = pair.Key;
                materialData[$"material{itemNumber}Count"] = pair.Value;
                itemNumber++;
            }

            var eventMaterialItemCraftsModel = new EventMaterialItemCraftsModel()
            {
                Id = actionId.ToString(),
                AgentAddress = signer.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                EventScheduleId = eventScheduleId,
                EventMaterialItemRecipeId = eventMaterialItemRecipeId,
                Material1Id = materialData["material1Id"],
                Material1Count = materialData["material1Count"],
                Material2Id = materialData["material2Id"],
                Material2Count = materialData["material2Count"],
                Material3Id = materialData["material3Id"],
                Material3Count = materialData["material3Count"],
                Material4Id = materialData["material4Id"],
                Material4Count = materialData["material4Count"],
                Material5Id = materialData["material5Id"],
                Material5Count = materialData["material5Count"],
                Material6Id = materialData["material6Id"],
                Material6Count = materialData["material6Count"],
                Material7Id = materialData["material7Id"],
                Material7Count = materialData["material7Count"],
                Material8Id = materialData["material8Id"],
                Material8Count = materialData["material8Count"],
                Material9Id = materialData["material9Id"],
                Material9Count = materialData["material9Count"],
                Material10Id = materialData["material10Id"],
                Material10Count = materialData["material10Count"],
                Material11Id = materialData["material11Id"],
                Material11Count = materialData["material11Count"],
                Material12Id = materialData["material12Id"],
                Material12Count = materialData["material12Count"],
                BlockIndex = blockIndex,
                Date = blockTime,
                Timestamp = blockTime,
            };

            return eventMaterialItemCraftsModel;
        }
    }
}
