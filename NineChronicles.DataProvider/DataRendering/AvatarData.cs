namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Extensions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;

    public static class AvatarData
    {
        public static AvatarModel GetAvatarInfo(
            IAccountStateDelta outputStates,
            Address signer,
            Address avatarAddress,
            List<RuneSlotInfo> runeInfos,
            DateTimeOffset blockTime)
        {
            AvatarState avatarState = outputStates.GetAvatarStateV2(avatarAddress);
            var sheets = outputStates.GetSheets(
                sheetTypes: new[]
                {
                    typeof(CharacterSheet),
                    typeof(CostumeStatSheet),
                    typeof(RuneListSheet),
                    typeof(RuneOptionSheet),
                });

            var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            var itemSlotState = outputStates.TryGetState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            var equipmentInventory = avatarState.inventory.Equipments;
            var equipmentList = itemSlotState.Equipments
                .Select(guid => equipmentInventory.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();

            var costumeInventory = avatarState.inventory.Costumes;
            var costumeList = itemSlotState.Costumes
                .Select(guid => costumeInventory.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            var runeStates = new List<RuneState>();
            foreach (var address in runeInfos.Select(info => RuneState.DeriveAddress(avatarAddress, info.RuneId)))
            {
                if (outputStates.TryGetState(address, out List rawRuneState))
                {
                    runeStates.Add(new RuneState(rawRuneState));
                }
            }

            foreach (var runeState in runeStates)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                runeOptions.Add(option);
            }

            var characterSheet = sheets.GetSheet<CharacterSheet>();
            if (!characterSheet.TryGetValue(avatarState.characterId, out var characterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var avatarLevel = avatarState.level;
            var avatarArmorId = avatarState.GetArmorId();
            Costume? avatarTitleCostume;
            try
            {
                avatarTitleCostume =
                    avatarState.inventory.Costumes.FirstOrDefault(costume =>
                        costume.ItemSubType == ItemSubType.Title &&
                        costume.equipped);
            }
            catch (Exception)
            {
                avatarTitleCostume = null;
            }

            int? avatarTitleId = null;
            if (avatarTitleCostume != null)
            {
                avatarTitleId = avatarTitleCostume.Id;
            }

            var avatarCp = CPHelper.TotalCP(
                equipmentList,
                costumeList,
                runeOptions,
                avatarState.level,
                characterRow,
                costumeStatSheet);
            string avatarName = avatarState.name;

            Log.Debug(
                "AvatarName: {0}, AvatarLevel: {1}, ArmorId: {2}, TitleId: {3}, CP: {4}",
                avatarName,
                avatarLevel,
                avatarArmorId,
                avatarTitleId,
                avatarCp);

            var avatarModel = new AvatarModel()
            {
                Address = avatarAddress.ToString(),
                AgentAddress = signer.ToString(),
                Name = avatarName,
                AvatarLevel = avatarLevel,
                TitleId = avatarTitleId,
                ArmorId = avatarArmorId,
                Cp = avatarCp,
                Timestamp = blockTime,
            };

            return avatarModel;
        }
    }
}
