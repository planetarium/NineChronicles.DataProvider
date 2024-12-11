namespace NineChronicles.DataProvider.DataRendering.Crafting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models.Crafting;

    public static class SynthesizeData
    {
        public static SynthesizeModel GetSynthesizeInfo(
            IWorld prevState,
            IRandom random,
            Address signer,
            Synthesize action,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            var avatarState = prevState.GetAvatarState(action.AvatarAddress, true, false, false);
            var sheets = prevState.GetSheets(sheetTypes: new[]
            {
                typeof(CostumeItemSheet),
                typeof(EquipmentItemSheet),
                typeof(SynthesizeSheet),
                typeof(SynthesizeWeightSheet),
                typeof(MaterialItemSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(SkillSheet),
            });

            var materialGrade = (Grade)action.MaterialGradeId;
            var materialItemSubType = (ItemSubType)action.MaterialItemSubTypeId;

            var materialItems = SynthesizeSimulator.GetMaterialList(
                action.MaterialIds,
                avatarState,
                blockIndex,
                materialGrade,
                materialItemSubType,
                action.AvatarAddress.ToString()
            );
            var materialInfo = new Dictionary<int, int>();
            foreach (var material in materialItems)
            {
                materialInfo.TryAdd(material.Id, 0);
                materialInfo[material.Id]++;
            }

            var synthesizedItems = SynthesizeSimulator.Simulate(
                new SynthesizeSimulator.InputData
                {
                    Grade = materialGrade,
                    ItemSubType = materialItemSubType,
                    MaterialCount = materialItems.Count,
                    SynthesizeSheet = sheets.GetSheet<SynthesizeSheet>(),
                    SynthesizeWeightSheet = sheets.GetSheet<SynthesizeWeightSheet>(),
                    CostumeItemSheet = sheets.GetSheet<CostumeItemSheet>(),
                    EquipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>(),
                    EquipmentItemRecipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>(),
                    EquipmentItemSubRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                    EquipmentItemOptionSheet = sheets.GetSheet<EquipmentItemOptionSheet>(),
                    SkillSheet = sheets.GetSheet<SkillSheet>(),
                    BlockIndex = blockIndex,
                    RandomObject = random,
                }
            );
            var synthesizedInfo = new Dictionary<int, int>();
            foreach (var synth in synthesizedItems)
            {
                synthesizedInfo.TryAdd(synth.ItemBase.Id, 0);
                synthesizedInfo[synth.ItemBase.Id]++;
            }

            return new SynthesizeModel
            {
                Id = action.Id.ToString(),
                BlockIndex = blockIndex,
                AgentAddress = signer.ToString(),
                Date = DateOnly.FromDateTime(blockTime.DateTime),
                TimeStamp = blockTime.UtcDateTime,
                AvatarAddress = action.AvatarAddress.ToString(),
                MaterialGradeId = action.MaterialGradeId,
                MaterialItemSubTypeId = action.MaterialItemSubTypeId,
                MaterialInfo = string.Join(",", materialInfo.Select(mi => $"{mi.Key}:{mi.Value}").ToList()),
                ResultInfo = string.Join(",", synthesizedInfo.Select(mi => $"{mi.Key}:{mi.Value}").ToList()),
            };
        }
    }
}
