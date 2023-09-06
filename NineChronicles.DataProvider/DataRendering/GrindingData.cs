namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using NineChronicles.DataProvider.Store.Models;

    public static class GrindingData
    {
        public static List<GrindingModel> GetGrindingInfo(
            IAccount previousStates,
            IAccount outputStates,
            Address signer,
            Address avatarAddress,
            List<Guid> equipmentIds,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState prevAvatarState = previousStates.GetAvatarStateV2(avatarAddress);
            AgentState agentState = previousStates.GetAgentState(signer);
            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                signer,
                agentState.MonsterCollectionRound
            );
            Dictionary<Type, (Address, ISheet)> sheets = previousStates.GetSheets(sheetTypes: new[]
            {
                typeof(CrystalEquipmentGrindingSheet),
                typeof(CrystalMonsterCollectionMultiplierSheet),
                typeof(MaterialItemSheet),
                typeof(StakeRegularRewardSheet),
            });

            List<Equipment> equipmentList = new List<Equipment>();
            foreach (var equipmentId in equipmentIds)
            {
                if (prevAvatarState.inventory.TryGetNonFungibleItem(equipmentId, out Equipment equipment))
                {
                    equipmentList.Add(equipment);
                }
            }

            Currency currency = previousStates.GetGoldCurrency();
            FungibleAssetValue stakedAmount = 0 * currency;
            if (previousStates.TryGetStakeState(signer, out StakeState stakeState))
            {
                stakedAmount = previousStates.GetBalance(stakeState.address, currency);
            }
            else
            {
                if (previousStates.TryGetState(monsterCollectionAddress, out Dictionary _))
                {
                    stakedAmount = previousStates.GetBalance(monsterCollectionAddress, currency);
                }
            }

            FungibleAssetValue crystal = CrystalCalculator.CalculateCrystal(
                signer,
                equipmentList,
                stakedAmount,
                false,
                sheets.GetSheet<CrystalEquipmentGrindingSheet>(),
                sheets.GetSheet<CrystalMonsterCollectionMultiplierSheet>(),
                sheets.GetSheet<StakeRegularRewardSheet>()
            );

            var grindList = new List<GrindingModel>();
            foreach (var equipment in equipmentList)
            {
                grindList.Add(new GrindingModel()
                {
                    Id = actionId.ToString(),
                    AgentAddress = signer.ToString(),
                    AvatarAddress = avatarAddress.ToString(),
                    EquipmentItemId = equipment.ItemId.ToString(),
                    EquipmentId = equipment.Id,
                    EquipmentLevel = equipment.level,
                    Crystal = Convert.ToDecimal(crystal.GetQuantityString()),
                    BlockIndex = blockIndex,
                    TimeStamp = blockTime,
                });
            }

            return grindList;
        }
    }
}
