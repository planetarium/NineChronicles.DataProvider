namespace NineChronicles.DataProvider.DataRendering
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
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
            ActionBase.ActionEvaluation<Grinding> ev,
            Grinding grinding,
            DateTimeOffset blockTime
        )
        {
            AvatarState prevAvatarState = ev.PreviousStates.GetAvatarStateV2(grinding.AvatarAddress);
            AgentState agentState = ev.PreviousStates.GetAgentState(ev.Signer);
            var previousStates = ev.PreviousStates;
            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
                ev.Signer,
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
            foreach (var equipmentId in grinding.EquipmentIds)
            {
                if (prevAvatarState.inventory.TryGetNonFungibleItem(equipmentId, out Equipment equipment))
                {
                    equipmentList.Add(equipment);
                }
            }

            Currency currency = previousStates.GetGoldCurrency();
            FungibleAssetValue stakedAmount = 0 * currency;
            if (previousStates.TryGetStakeState(ev.Signer, out StakeState stakeState))
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
                ev.Signer,
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
                    Id = grinding.Id.ToString(),
                    AgentAddress = ev.Signer.ToString(),
                    AvatarAddress = grinding.AvatarAddress.ToString(),
                    EquipmentItemId = equipment.ItemId.ToString(),
                    EquipmentId = equipment.Id,
                    EquipmentLevel = equipment.level,
                    Crystal = Convert.ToDecimal(crystal.GetQuantityString()),
                    BlockIndex = ev.BlockIndex,
                    TimeStamp = blockTime,
                });
            }

            return grindList;
        }
    }
}
