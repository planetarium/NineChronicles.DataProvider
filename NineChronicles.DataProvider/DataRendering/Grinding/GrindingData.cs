namespace NineChronicles.DataProvider.DataRendering.Grinding
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
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
    using Serilog;

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
            var prevAvatarState = previousStates.GetAvatarState(
                avatarAddress,
                getInventory: true,
                getQuestList: false,
                getWorldInformation: false
            );

            var agentState = previousStates.GetAgentState(signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException("Aborted as the agent state failed to load.");
            }

            var monsterCollectionAddress = MonsterCollectionState.DeriveAddress(
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

            var equipmentList = new List<Equipment>();
            foreach (var equipmentId in equipmentIds)
            {
                if (prevAvatarState.inventory.TryGetNonFungibleItem(equipmentId, out Equipment equipment))
                {
                    equipmentList.Add(equipment);
                }
            }

            var currency = previousStates.GetGoldCurrency();
            var stakedAmount = 0 * currency;
            if (previousStates.TryGetStakeState(signer, out _))
            {
                var stakeAddr = StakeState.DeriveAddress(signer);
                stakedAmount = previousStates.GetBalance(stakeAddr, currency);
            }
            else
            {
                if (previousStates.TryGetLegacyState(monsterCollectionAddress, out Dictionary _))
                {
                    stakedAmount = previousStates.GetBalance(monsterCollectionAddress, currency);
                }
            }

            var grindList = new List<GrindingModel>();
            for (var i = 0; i < equipmentList.Count; i++)
            {
                var equipment = equipmentList[i];
                var crystal = CrystalCalculator.CalculateCrystal(
                    signer,
                    new List<Equipment> { equipment },
                    stakedAmount,
                    false,
                    sheets.GetSheet<CrystalEquipmentGrindingSheet>(),
                    sheets.GetSheet<CrystalMonsterCollectionMultiplierSheet>(),
                    sheets.GetSheet<StakeRegularRewardSheet>()
                );

                var materials = Grinding.CalculateMaterialReward(
                    new List<Equipment> { equipment },
                    sheets.GetSheet<CrystalEquipmentGrindingSheet>(),
                    sheets.GetSheet<MaterialItemSheet>()
                );
                var materialList = materials.Select(kv => $"{kv.Key.Id}:{kv.Value}").ToList();

                grindList.Add(new GrindingModel()
                {
                    Id = $"{actionId}_{i:D3}",
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

            Log.Debug($"{grindList.Count} grinding collected from {signer}");
            return grindList;
        }
    }
}
