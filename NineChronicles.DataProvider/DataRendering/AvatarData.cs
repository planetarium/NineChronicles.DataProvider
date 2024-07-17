namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Rune;
    using NineChronicles.DataProvider.Store.Models;
    using Serilog;

    public static class AvatarData
    {
        public static AvatarModel GetAvatarInfo(
            IWorld outputStates,
            Address signer,
            Address avatarAddress,
            DateTimeOffset blockTime,
            BattleType battleType)
        {
            var start = DateTimeOffset.UtcNow;
            var subStart = DateTimeOffset.UtcNow;
            AvatarState avatarState = outputStates.GetAvatarState(avatarAddress);
            var collectionExist = outputStates.TryGetCollectionState(avatarAddress, out var collectionState);
            var sheetTypes = new List<Type>
            {
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
                typeof(RuneListSheet),
                typeof(RuneOptionSheet),
                typeof(CollectionSheet),
                typeof(RuneLevelBonusSheet),
            };
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            var sheets = outputStates.GetSheets(
                sheetTypes: sheetTypes);

            var subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo GetSheets Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);

            subStart = DateTimeOffset.UtcNow;
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            var itemSlotState = outputStates.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Adventure);
            var equipmentList = SetEquipments(avatarState, itemSlotState, battleType);
            var costumeList = SetCostumes(avatarState, itemSlotState, battleType);

            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo ItemSlotState Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);

            subStart = DateTimeOffset.UtcNow;
            var runeStates = outputStates.GetRuneState(avatarAddress, out _);
            var runeAddresses = RuneSlotState.DeriveAddress(avatarAddress, battleType);
            var runeSlotState = outputStates.TryGetLegacyState(runeAddresses, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Adventure);
            var runeSlotStates = new List<RuneSlotState>();
            runeSlotStates.Add(runeSlotState);
            var runes = SetRunes(runeSlotStates, battleType);

            var equippedRuneStates = new List<RuneState>();
            var runeIds = runes[battleType].GetRuneSlot()
                .Where(slot => slot.RuneId.HasValue)
                .Select(slot => slot.RuneId!.Value);

            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo RuneSlotState Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);

            subStart = DateTimeOffset.UtcNow;
            foreach (var runeId in runeIds)
            {
                var runeState = runeStates.Runes!.FirstOrDefault(x => x.Value.RuneId == runeId);
                if (runeStates.Runes != null)
                {
                    equippedRuneStates.Add(runeState.Value);
                }
            }

            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            foreach (var runeState in equippedRuneStates)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    continue;
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    continue;
                }

                runeOptions.Add(option);
            }

            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo RuneOptions Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);

            subStart = DateTimeOffset.UtcNow;
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

            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                foreach (var id in collectionState.Ids)
                {
                    var row = collectionSheet[id];
                    collectionModifiers.AddRange(row.StatModifiers);
                }
            }

            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo AvatarTitle Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);

            subStart = DateTimeOffset.UtcNow;
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                outputStates.GetRuneState(avatarAddress, out _),
                sheets.GetSheet<RuneListSheet>(),
                sheets.GetSheet<RuneLevelBonusSheet>());
            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo RuneHelper Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);

            subStart = DateTimeOffset.UtcNow;
            var avatarCp = CPHelper.TotalCP(
                equipmentList[battleType],
                costumeList[battleType],
                runeOptions,
                avatarState.level,
                characterRow,
                costumeStatSheet,
                collectionModifiers,
                runeLevelBonus);
            subEnd = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarInfo CPHelper Address: {0} Time Taken: {1} ms.",
                avatarAddress,
                (subEnd - subStart).Milliseconds);

            string avatarName = avatarState.name;

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

            var end = DateTimeOffset.UtcNow;
            Log.Debug(
                "[DataProvider] AvatarName: {0}, AvatarLevel: {1}, ArmorId: {2}, TitleId: {3}, CP: {4}, Time Taken: {5} ms.",
                avatarName,
                avatarLevel,
                avatarArmorId,
                avatarTitleId,
                avatarCp,
                (end - start).Milliseconds);
            return avatarModel;
        }

        private static Dictionary<BattleType, List<Equipment>> SetEquipments(
            AvatarState avatarState,
            ItemSlotState itemSlotStates,
            BattleType battleType)
        {
            Dictionary<BattleType, List<Equipment>> equipments = new ();
            equipments.Add(BattleType.Adventure, new List<Equipment>());
            equipments.Add(BattleType.Arena, new List<Equipment>());
            equipments.Add(BattleType.Raid, new List<Equipment>());
            var equipmentList = itemSlotStates.Equipments
                .Select(guid =>
                    avatarState.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            equipments[battleType] = equipmentList!;

            return equipments;
        }

        private static Dictionary<BattleType, List<Costume>> SetCostumes(
            AvatarState avatarState,
            ItemSlotState itemSlotStates,
            BattleType battleType)
        {
            Dictionary<BattleType, List<Costume>> costumes = new ();
            costumes.Add(BattleType.Adventure, new List<Costume>());
            costumes.Add(BattleType.Arena, new List<Costume>());
            costumes.Add(BattleType.Raid, new List<Costume>());
            var costumeList = itemSlotStates.Costumes
                .Select(guid =>
                    avatarState.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                .Where(item => item != null).ToList();
            costumes[battleType] = costumeList!;

            return costumes;
        }

        private static Dictionary<BattleType, RuneSlotState> SetRunes(
            List<RuneSlotState> runeSlotStates,
            BattleType battleType)
        {
            Dictionary<BattleType, RuneSlotState> runes = new ();
            runes.Add(BattleType.Adventure, new RuneSlotState(BattleType.Adventure));
            runes.Add(BattleType.Arena, new RuneSlotState(BattleType.Arena));
            runes.Add(BattleType.Raid, new RuneSlotState(BattleType.Raid));
            foreach (var state in runeSlotStates)
            {
                runes[battleType] = state;
            }

            return runes;
        }
    }
}
