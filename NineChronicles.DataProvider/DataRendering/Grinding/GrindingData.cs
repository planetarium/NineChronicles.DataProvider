namespace NineChronicles.DataProvider.DataRendering.Grinding
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using NineChronicles.DataProvider.Store.Models.Grinding;

    public static class GrindingData
    {
        public static List<GrindingModel> GetGrindingInfo(
            IWorld previousStates,
            Address signer,
            Address avatarAddress,
            List<Guid> equipmentIds,
            Guid actionId,
            long blockIndex,
            DateTimeOffset blockTime
        )
        {
            AvatarState prevAvatarState = previousStates.GetAvatarState(avatarAddress);
            AgentState? agentState = previousStates.GetAgentState(signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state failed to load.");
            }

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
            if (previousStates.TryGetStakeStateV2(signer, out _))
            {
                var stakeAddr = StakeStateV2.DeriveAddress(signer);
                stakedAmount = previousStates.GetBalance(stakeAddr, currency);
            }
            else
            {
                if (previousStates.TryGetLegacyState(monsterCollectionAddress, out Dictionary _))
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

            var materials = Grinding.CalculateMaterialReward(
                equipmentList,
                sheets.GetSheet<CrystalEquipmentGrindingSheet>(),
                sheets.GetSheet<MaterialItemSheet>()
            );
            var materialList = materials.Select(kv => $"{kv.Key.Id}:{kv.Value}").ToList();

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
                    Materials = string.Join(",", materialList),
                    BlockIndex = blockIndex,
                    Date = DateOnly.FromDateTime(blockTime.DateTime),
                    TimeStamp = blockTime,
                });
            }

            return grindList;
        }
    }
}
