namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Lib9c.Renderer;
    using Libplanet;
    using Libplanet.Assets;
    using Microsoft.Extensions.Hosting;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Battle;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Nekoyume.TableData.Event;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless;
    using Serilog;
    using static Lib9c.SerializeKeys;

    public class RenderSubscriber : BackgroundService
    {
        private const int DefaultInsertInterval = 30;
        private readonly int _blockInsertInterval;
        private readonly string _blockIndexFilePath;
        private readonly BlockRenderer _blockRenderer;
        private readonly ActionRenderer _actionRenderer;
        private readonly ExceptionRenderer _exceptionRenderer;
        private readonly NodeStatusRenderer _nodeStatusRenderer;
        private readonly List<AgentModel> _agentList = new List<AgentModel>();
        private readonly List<AvatarModel> _avatarList = new List<AvatarModel>();
        private readonly List<HackAndSlashModel> _hasList = new List<HackAndSlashModel>();
        private readonly List<CombinationConsumableModel> _ccList = new List<CombinationConsumableModel>();
        private readonly List<CombinationEquipmentModel> _ceList = new List<CombinationEquipmentModel>();
        private readonly List<EquipmentModel> _eqList = new List<EquipmentModel>();
        private readonly List<ItemEnhancementModel> _ieList = new List<ItemEnhancementModel>();
        private readonly List<ShopHistoryEquipmentModel> _buyShopEquipmentsList = new List<ShopHistoryEquipmentModel>();
        private readonly List<ShopHistoryCostumeModel> _buyShopCostumesList = new List<ShopHistoryCostumeModel>();
        private readonly List<ShopHistoryMaterialModel> _buyShopMaterialsList = new List<ShopHistoryMaterialModel>();
        private readonly List<ShopHistoryConsumableModel> _buyShopConsumablesList = new List<ShopHistoryConsumableModel>();
        private readonly List<StakeModel> _stakeList = new List<StakeModel>();
        private readonly List<ClaimStakeRewardModel> _claimStakeList = new List<ClaimStakeRewardModel>();
        private readonly List<MigrateMonsterCollectionModel> _mmcList = new List<MigrateMonsterCollectionModel>();
        private readonly List<GrindingModel> _grindList = new List<GrindingModel>();
        private readonly List<ItemEnhancementFailModel> _itemEnhancementFailList = new List<ItemEnhancementFailModel>();
        private readonly List<UnlockEquipmentRecipeModel> _unlockEquipmentRecipeList = new List<UnlockEquipmentRecipeModel>();
        private readonly List<UnlockWorldModel> _unlockWorldList = new List<UnlockWorldModel>();
        private readonly List<ReplaceCombinationEquipmentMaterialModel> _replaceCombinationEquipmentMaterialList = new List<ReplaceCombinationEquipmentMaterialModel>();
        private readonly List<HasRandomBuffModel> _hasRandomBuffList = new List<HasRandomBuffModel>();
        private readonly List<HasWithRandomBuffModel> _hasWithRandomBuffList = new List<HasWithRandomBuffModel>();
        private readonly List<JoinArenaModel> _joinArenaList = new List<JoinArenaModel>();
        private readonly List<BattleArenaModel> _battleArenaList = new List<BattleArenaModel>();
        private readonly List<BlockModel> _blockList = new List<BlockModel>();
        private readonly List<TransactionModel> _transactionList = new List<TransactionModel>();
        private readonly List<HackAndSlashSweepModel> _hasSweepList = new List<HackAndSlashSweepModel>();
        private readonly List<EventDungeonBattleModel> _eventDungeonBattleList = new List<EventDungeonBattleModel>();
        private readonly List<EventConsumableItemCraftsModel> _eventConsumableItemCraftsList = new List<EventConsumableItemCraftsModel>();
        private readonly List<RaiderModel> _raiderList = new List<RaiderModel>();
        private readonly List<BattleGrandFinaleModel> _battleGrandFinaleList = new List<BattleGrandFinaleModel>();
        private readonly List<EventMaterialItemCraftsModel> _eventMaterialItemCraftsList = new List<EventMaterialItemCraftsModel>();
        private readonly List<RuneEnhancementModel> _runeEnhancementList = new List<RuneEnhancementModel>();
        private readonly List<RunesAcquiredModel> _runesAcquiredList = new List<RunesAcquiredModel>();
        private readonly List<UnlockRuneSlotModel> _unlockRuneSlotList = new List<UnlockRuneSlotModel>();
        private readonly List<string> _agents;
        private readonly bool _render;
        private int _renderedBlockCount;
        private DateTimeOffset _blockTimeOffset;
        private Address _miner;

        public RenderSubscriber(
            NineChroniclesNodeService nodeService,
            MySqlStore mySqlStore
        )
        {
            _blockRenderer = nodeService.BlockRenderer;
            _actionRenderer = nodeService.ActionRenderer;
            _exceptionRenderer = nodeService.ExceptionRenderer;
            _nodeStatusRenderer = nodeService.NodeStatusRenderer;
            MySqlStore = mySqlStore;
            _renderedBlockCount = 0;
            _agents = new List<string>();
            _render = Convert.ToBoolean(Environment.GetEnvironmentVariable("NC_Render"));
            string dataPath = Environment.GetEnvironmentVariable("NC_BlockIndexFilePath")
                              ?? Path.GetTempPath();
            if (!Directory.Exists(dataPath))
            {
                dataPath = Path.GetTempPath();
            }

            _blockIndexFilePath = Path.Combine(dataPath, "blockIndex.txt");

            try
            {
                _blockInsertInterval = Convert.ToInt32(Environment.GetEnvironmentVariable("NC_BlockInsertInterval"));
                if (_blockInsertInterval < 1)
                {
                    _blockInsertInterval = DefaultInsertInterval;
                }
            }
            catch (Exception)
            {
                _blockInsertInterval = DefaultInsertInterval;
            }
        }

        internal MySqlStore MySqlStore { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _blockRenderer.BlockSubject.Subscribe(b =>
            {
                if (!_render)
                {
                    return;
                }

                if (_renderedBlockCount == _blockInsertInterval)
                {
                    var start = DateTimeOffset.Now;
                    Log.Debug("Storing Data...");
                    var tasks = new List<Task>
                    {
                        Task.Run(() =>
                        {
                            MySqlStore.StoreAgentList(_agentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreAvatarList(_avatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreHackAndSlashList(_hasList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreCombinationConsumableList(_ccList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreCombinationEquipmentList(_ceList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreItemEnhancementList(_ieList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreShopHistoryEquipmentList(_buyShopEquipmentsList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreShopHistoryCostumeList(_buyShopCostumesList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreShopHistoryMaterialList(_buyShopMaterialsList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreShopHistoryConsumableList(_buyShopConsumablesList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.ProcessEquipmentList(_eqList.GroupBy(i => i.ItemId).Select(i => i.FirstOrDefault()).ToList());
                            MySqlStore.StoreStakingList(_stakeList);
                            MySqlStore.StoreClaimStakeRewardList(_claimStakeList);
                            MySqlStore.StoreMigrateMonsterCollectionList(_mmcList);
                            MySqlStore.StoreGrindList(_grindList);
                            MySqlStore.StoreItemEnhancementFailList(_itemEnhancementFailList);
                            MySqlStore.StoreUnlockEquipmentRecipeList(_unlockEquipmentRecipeList);
                            MySqlStore.StoreUnlockWorldList(_unlockWorldList);
                            MySqlStore.StoreReplaceCombinationEquipmentMaterialList(_replaceCombinationEquipmentMaterialList);
                            MySqlStore.StoreHasRandomBuffList(_hasRandomBuffList);
                            MySqlStore.StoreHasWithRandomBuffList(_hasWithRandomBuffList);
                            MySqlStore.StoreJoinArenaList(_joinArenaList);
                            MySqlStore.StoreBattleArenaList(_battleArenaList);
                            MySqlStore.StoreBlockList(_blockList);
                            MySqlStore.StoreTransactionList(_transactionList);
                            MySqlStore.StoreHackAndSlashSweepList(_hasSweepList);
                            MySqlStore.StoreEventDungeonBattleList(_eventDungeonBattleList);
                            MySqlStore.StoreEventConsumableItemCraftsList(_eventConsumableItemCraftsList);
                            MySqlStore.StoreRaiderList(_raiderList);
                            MySqlStore.StoreBattleGrandFinaleList(_battleGrandFinaleList);
                            MySqlStore.StoreEventMaterialItemCraftsList(_eventMaterialItemCraftsList);
                            MySqlStore.StoreRuneEnhancementList(_runeEnhancementList);
                            MySqlStore.StoreRunesAcquiredList(_runesAcquiredList);
                            MySqlStore.StoreUnlockRuneSlotList(_unlockRuneSlotList);
                        }),
                    };

                    Task.WaitAll(tasks.ToArray());
                    _renderedBlockCount = 0;
                    _agents.Clear();
                    _agentList.Clear();
                    _avatarList.Clear();
                    _hasList.Clear();
                    _ccList.Clear();
                    _ceList.Clear();
                    _ieList.Clear();
                    _buyShopEquipmentsList.Clear();
                    _buyShopCostumesList.Clear();
                    _buyShopMaterialsList.Clear();
                    _buyShopConsumablesList.Clear();
                    _eqList.Clear();
                    _stakeList.Clear();
                    _claimStakeList.Clear();
                    _mmcList.Clear();
                    _grindList.Clear();
                    _itemEnhancementFailList.Clear();
                    _unlockEquipmentRecipeList.Clear();
                    _unlockWorldList.Clear();
                    _replaceCombinationEquipmentMaterialList.Clear();
                    _hasRandomBuffList.Clear();
                    _hasWithRandomBuffList.Clear();
                    _joinArenaList.Clear();
                    _battleArenaList.Clear();
                    _blockList.Clear();
                    _transactionList.Clear();
                    _hasSweepList.Clear();
                    _eventDungeonBattleList.Clear();
                    _eventConsumableItemCraftsList.Clear();
                    _raiderList.Clear();
                    _battleGrandFinaleList.Clear();
                    _eventMaterialItemCraftsList.Clear();
                    _runeEnhancementList.Clear();
                    _runesAcquiredList.Clear();
                    _unlockRuneSlotList.Clear();
                    var end = DateTimeOffset.Now;
                    long blockIndex = b.OldTip.Index;
                    StreamWriter blockIndexFile = new StreamWriter(_blockIndexFilePath);
                    blockIndexFile.Write(blockIndex);
                    blockIndexFile.Flush();
                    blockIndexFile.Close();
                    Log.Debug($"Storing Data Complete. Time Taken: {(end - start).Milliseconds} ms.");
                }

                var block = b.NewTip;
                _blockTimeOffset = block.Timestamp.UtcDateTime;
                _miner = block.Miner;
                _blockList.Add(new BlockModel()
                {
                    Index = block.Index,
                    Hash = block.Hash.ToString(),
                    Miner = block.Miner.ToString(),
                    Difficulty = block.Difficulty,
                    Nonce = block.Nonce.ToString(),
                    PreviousHash = block.PreviousHash.ToString(),
                    ProtocolVersion = block.ProtocolVersion,
                    PublicKey = block.PublicKey!.ToString(),
                    StateRootHash = block.StateRootHash.ToString(),
                    TotalDifficulty = (long)block.TotalDifficulty,
                    TxCount = block.Transactions.Count(),
                    TxHash = block.TxHash.ToString(),
                    TimeStamp = block.Timestamp.UtcDateTime,
                });

                foreach (var transaction in block.Transactions)
                {
                    var actionType = transaction.CustomActions!.Select(action => action.ToString()!.Split('.')
                        .LastOrDefault()?.Replace(">", string.Empty));
                    _transactionList.Add(new TransactionModel()
                    {
                        BlockIndex = block.Index,
                        BlockHash = block.Hash.ToString(),
                        TxId = transaction.Id.ToString(),
                        Signer = transaction.Signer.ToString(),
                        ActionType = actionType.FirstOrDefault(),
                        Nonce = transaction.Nonce,
                        PublicKey = transaction.PublicKey.ToString(),
                        UpdatedAddressesCount = transaction.UpdatedAddresses.Count(),
                        Date = transaction.Timestamp.UtcDateTime,
                        TimeStamp = transaction.Timestamp.UtcDateTime,
                    });
                }

                _renderedBlockCount++;
                Log.Debug($"Rendered Block Count: #{_renderedBlockCount} at Block #{block.Index}");
            });

            _actionRenderer.EveryRender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        try
                        {
                            if (ev.Exception != null)
                            {
                                return;
                            }

                            if (ev.Action is UnlockRuneSlot unlockRuneSlot)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var previousStates = ev.PreviousStates;
                                Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
                                var prevNCGBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var outputNCGBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var burntNCG = prevNCGBalance - outputNCGBalance;
                                _unlockRuneSlotList.Add(new UnlockRuneSlotModel()
                                {
                                    Id = unlockRuneSlot.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = unlockRuneSlot.AvatarAddress.ToString(),
                                    SlotIndex = unlockRuneSlot.SlotIndex,
                                    BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                                    Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                                    TimeStamp = _blockTimeOffset,
                                });
                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored UnlockRuneSlot action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RenderSubscriber: {message}", ex.Message);
                        }
                    });

            _actionRenderer.EveryUnrender<ActionBase>()
                .Subscribe(
                    ev =>
                    {
                        try
                        {
                            if (ev.Exception != null)
                            {
                                return;
                            }

                            if (ev.Action is HackAndSlash has)
                            {
                                MySqlStore.DeleteHackAndSlash(has.Id);
                                Log.Debug("Deleted HackAndSlash action in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationConsumable combinationConsumable)
                            {
                                MySqlStore.DeleteCombinationConsumable(combinationConsumable.Id);
                                Log.Debug("Deleted CombinationConsumable action in block #{index}", ev.BlockIndex);
                            }

                            if (ev.Action is CombinationEquipment combinationEquipment)
                            {
                                MySqlStore.DeleteCombinationEquipment(combinationEquipment.Id);
                                Log.Debug("Deleted CombinationEquipment action in block #{index}", ev.BlockIndex);
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        combinationEquipment.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable);
                                }

                                Log.Debug(
                                    "Reverted avatar {address}'s equipments in block #{index}",
                                    combinationEquipment.avatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is ItemEnhancement itemEnhancement)
                            {
                                MySqlStore.DeleteItemEnhancement(itemEnhancement.Id);
                                Log.Debug("Deleted ItemEnhancement action in block #{index}", ev.BlockIndex);
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        itemEnhancement.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable);
                                }

                                Log.Debug(
                                    "Reverted avatar {address}'s equipments in block #{index}",
                                    itemEnhancement.avatarAddress,
                                    ev.BlockIndex);
                            }

                            if (ev.Action is Buy buy)
                            {
                                var buyerInventory = ev.OutputStates.GetAvatarStateV2(buy.buyerAvatarAddress).inventory;

                                foreach (var purchaseInfo in buy.purchaseInfos)
                                {
                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        AvatarState sellerState = ev.OutputStates.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        MySqlStore.StoreAgent(ev.Signer);
                                        Equipment? equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            ProcessEquipmentData(
                                                purchaseInfo.SellerAvatarAddress,
                                                purchaseInfo.SellerAgentAddress,
                                                equipmentNotNull);
                                        }
                                    }
                                }

                                Log.Debug(
                                    "Reverted avatar {address}'s equipment in block #{index}",
                                    buy.buyerAvatarAddress,
                                    ev.BlockIndex);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RenderSubscriber: {message}", ex.Message);
                        }
                    });

            return Task.CompletedTask;
        }

        private void ProcessEquipmentData(
            Address agentAddress,
            Address avatarAddress,
            Equipment equipment)
        {
            var cp = CPHelper.GetCP(equipment);
            _eqList.Add(new EquipmentModel()
            {
                ItemId = equipment.ItemId.ToString(),
                AgentAddress = agentAddress.ToString(),
                AvatarAddress = avatarAddress.ToString(),
                EquipmentId = equipment.Id,
                Cp = cp,
                Level = equipment.level,
                ItemSubType = equipment.ItemSubType.ToString(),
            });
        }

        private void ProcessAgentAvatarData(ActionBase.ActionEvaluation<ActionBase> ev)
        {
            if (!_agents.Contains(ev.Signer.ToString()))
            {
                _agents.Add(ev.Signer.ToString());
                _agentList.Add(new AgentModel()
                {
                    Address = ev.Signer.ToString(),
                });

                if (ev.Signer != _miner)
                {
                    var agentState = ev.OutputStates.GetAgentState(ev.Signer);
                    if (agentState is { } ag)
                    {
                        var avatarAddresses = ag.avatarAddresses;
                        foreach (var avatarAddress in avatarAddresses.Select(avatarAddress => avatarAddress.Value))
                        {
                            try
                            {
                                AvatarState avatarState;
                                try
                                {
                                    avatarState = ev.OutputStates.GetAvatarStateV2(avatarAddress);
                                }
                                catch (Exception)
                                {
                                    avatarState = ev.OutputStates.GetAvatarState(avatarAddress);
                                }

                                if (avatarState == null)
                                {
                                    continue;
                                }

                                var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
                                var itemSlotState = ev.OutputStates.TryGetState(itemSlotStateAddress, out List rawItemSlotState)
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

                                var sheets = ev.OutputStates.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(CharacterSheet),
                                        typeof(CostumeStatSheet),
                                        typeof(RuneSheet),
                                        typeof(RuneListSheet),
                                        typeof(RuneOptionSheet),
                                    });
                                var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
                                var runeSlotState = ev.OutputStates.TryGetState(runeSlotStateAddress, out List rawRuneSlotState)
                                    ? new RuneSlotState(rawRuneSlotState)
                                    : new RuneSlotState(BattleType.Adventure);
                                var runeSlotInfos = runeSlotState.GetEquippedRuneSlotInfos();
                                var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
                                var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
                                var runeStates = new List<RuneState>();
                                foreach (var address in runeSlotInfos.Select(info => RuneState.DeriveAddress(avatarAddress, info.RuneId)))
                                {
                                    if (ev.OutputStates.TryGetState(address, out List rawRuneState))
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

                                _avatarList.Add(new AvatarModel()
                                {
                                    Address = avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                    Timestamp = _blockTimeOffset,
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }
        }

        private string GetSignerAndOtherAddressesHex(Address signer, params Address[] addresses)
        {
            StringBuilder sb = new StringBuilder($"[{signer.ToHex()}");

            foreach (Address address in addresses)
            {
                sb.Append($", {address.ToHex()}");
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}
