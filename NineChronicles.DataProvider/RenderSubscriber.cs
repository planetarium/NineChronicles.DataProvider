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
        private readonly List<string> _agents;
        private readonly bool _render;
        private bool _migratedRaid = false;
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

                            ProcessAgentAvatarData(ev);

                            if (ev.Action is EventDungeonBattle eventDungeonBattle)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var previousStates = ev.PreviousStates;
                                var outputStates = ev.OutputStates;
                                AvatarState prevAvatarState = previousStates.GetAvatarStateV2(eventDungeonBattle.AvatarAddress);
                                AvatarState outputAvatarState = outputStates.GetAvatarStateV2(eventDungeonBattle.AvatarAddress);
                                var prevAvatarItems = prevAvatarState.inventory.Items;
                                var outputAvatarItems = outputAvatarState.inventory.Items;
                                var addressesHex = GetSignerAndOtherAddressesHex(ev.Signer, eventDungeonBattle.AvatarAddress);
                                var scheduleSheet = previousStates.GetSheet<EventScheduleSheet>();
                                var scheduleRow = scheduleSheet.ValidateFromActionForDungeon(
                                    ev.BlockIndex,
                                    eventDungeonBattle.EventScheduleId,
                                    eventDungeonBattle.EventDungeonId,
                                    "event_dungeon_battle",
                                    addressesHex);
                                var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                                    eventDungeonBattle.AvatarAddress,
                                    eventDungeonBattle.EventDungeonId);
                                var eventDungeonInfo = ev.OutputStates.GetState(eventDungeonInfoAddr)
                                    is Bencodex.Types.List serializedEventDungeonInfoList
                                    ? new EventDungeonInfo(serializedEventDungeonInfoList)
                                    : new EventDungeonInfo(remainingTickets: scheduleRow.DungeonTicketsMax);
                                bool isClear = eventDungeonInfo.IsCleared(eventDungeonBattle.EventDungeonStageId);
                                Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
                                var prevNCGBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var outputNCGBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var burntNCG = prevNCGBalance - outputNCGBalance;
                                var newItems = outputAvatarItems.Except(prevAvatarItems);
                                Dictionary<string, int> rewardItemData = new Dictionary<string, int>();
                                for (var i = 1; i < 11; i++)
                                {
                                    rewardItemData.Add($"rewardItem{i}Id", 0);
                                    rewardItemData.Add($"rewardItem{i}Count", 0);
                                }

                                int itemNumber = 1;
                                foreach (var newItem in newItems)
                                {
                                    rewardItemData[$"rewardItem{itemNumber}Id"] = newItem.item.Id;
                                    if (prevAvatarItems.Any(x => x.item.Equals(newItem.item)))
                                    {
                                        var prevItemCount = prevAvatarItems.Single(x => x.item.Equals(newItem.item)).count;
                                        var rewardCount = newItem.count - prevItemCount;
                                        rewardItemData[$"rewardItem{itemNumber}Count"] = rewardCount;
                                    }
                                    else
                                    {
                                        rewardItemData[$"rewardItem{itemNumber}Count"] = newItem.count;
                                    }

                                    itemNumber++;
                                }

                                _eventDungeonBattleList.Add(new EventDungeonBattleModel()
                                {
                                    Id = eventDungeonBattle.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = eventDungeonBattle.AvatarAddress.ToString(),
                                    EventDungeonId = eventDungeonBattle.EventDungeonId,
                                    EventScheduleId = eventDungeonBattle.EventScheduleId,
                                    EventDungeonStageId = eventDungeonBattle.EventDungeonStageId,
                                    RemainingTickets = eventDungeonInfo.RemainingTickets,
                                    BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                                    Cleared = isClear,
                                    FoodsCount = eventDungeonBattle.Foods.Count,
                                    CostumesCount = eventDungeonBattle.Costumes.Count,
                                    EquipmentsCount = eventDungeonBattle.Equipments.Count,
                                    RewardItem1Id = rewardItemData["rewardItem1Id"],
                                    RewardItem1Count = rewardItemData["rewardItem1Count"],
                                    RewardItem2Id = rewardItemData["rewardItem2Id"],
                                    RewardItem2Count = rewardItemData["rewardItem2Count"],
                                    RewardItem3Id = rewardItemData["rewardItem3Id"],
                                    RewardItem3Count = rewardItemData["rewardItem3Count"],
                                    RewardItem4Id = rewardItemData["rewardItem4Id"],
                                    RewardItem4Count = rewardItemData["rewardItem4Count"],
                                    RewardItem5Id = rewardItemData["rewardItem5Id"],
                                    RewardItem5Count = rewardItemData["rewardItem5Count"],
                                    RewardItem6Id = rewardItemData["rewardItem6Id"],
                                    RewardItem6Count = rewardItemData["rewardItem6Count"],
                                    RewardItem7Id = rewardItemData["rewardItem7Id"],
                                    RewardItem7Count = rewardItemData["rewardItem7Count"],
                                    RewardItem8Id = rewardItemData["rewardItem8Id"],
                                    RewardItem8Count = rewardItemData["rewardItem8Count"],
                                    RewardItem9Id = rewardItemData["rewardItem9Id"],
                                    RewardItem9Count = rewardItemData["rewardItem9Count"],
                                    RewardItem10Id = rewardItemData["rewardItem10Id"],
                                    RewardItem10Count = rewardItemData["rewardItem10Count"],
                                    BlockIndex = ev.BlockIndex,
                                    Timestamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored EventDungeonBattle action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is EventConsumableItemCrafts eventConsumableItemCrafts)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var previousStates = ev.PreviousStates;
                                var addressesHex = GetSignerAndOtherAddressesHex(ev.Signer, eventConsumableItemCrafts.AvatarAddress);
                                var requiredFungibleItems = new Dictionary<int, int>();
                                Dictionary<string, int> requiredItemData = new Dictionary<string, int>();
                                for (var i = 1; i < 11; i++)
                                {
                                    requiredItemData.Add($"requiredItem{i}Id", 0);
                                    requiredItemData.Add($"requiredItem{i}Count", 0);
                                }

                                int itemNumber = 1;
                                var sheets = previousStates.GetSheets(
                                    sheetTypes: new[]
                                    {
                                        typeof(EventScheduleSheet),
                                        typeof(EventConsumableItemRecipeSheet),
                                    });
                                var scheduleSheet = sheets.GetSheet<EventScheduleSheet>();
                                scheduleSheet.ValidateFromActionForRecipe(
                                    ev.BlockIndex,
                                    eventConsumableItemCrafts.EventScheduleId,
                                    eventConsumableItemCrafts.EventConsumableItemRecipeId,
                                    "event_consumable_item_crafts",
                                    addressesHex);
                                var recipeSheet = sheets.GetSheet<EventConsumableItemRecipeSheet>();
                                var recipeRow = recipeSheet.ValidateFromAction(
                                    eventConsumableItemCrafts.EventConsumableItemRecipeId,
                                    "event_consumable_item_crafts",
                                    addressesHex);
                                var materialItemSheet = previousStates.GetSheet<MaterialItemSheet>();
                                materialItemSheet.ValidateFromAction(
                                    recipeRow.Materials,
                                    requiredFungibleItems,
                                    addressesHex);
                                foreach (var pair in requiredFungibleItems)
                                {
                                    if (materialItemSheet.TryGetValue(pair.Key, out var materialRow))
                                    {
                                        requiredItemData[$"requiredItem{itemNumber}Id"] = materialRow.Id;
                                        requiredItemData[$"requiredItem{itemNumber}Count"] = pair.Value;
                                    }

                                    itemNumber++;
                                }

                                _eventConsumableItemCraftsList.Add(new EventConsumableItemCraftsModel()
                                {
                                    Id = eventConsumableItemCrafts.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = eventConsumableItemCrafts.AvatarAddress.ToString(),
                                    SlotIndex = eventConsumableItemCrafts.SlotIndex,
                                    EventScheduleId = eventConsumableItemCrafts.EventScheduleId,
                                    EventConsumableItemRecipeId = eventConsumableItemCrafts.EventConsumableItemRecipeId,
                                    RequiredItem1Id = requiredItemData["requiredItem1Id"],
                                    RequiredItem1Count = requiredItemData["requiredItem1Count"],
                                    RequiredItem2Id = requiredItemData["requiredItem2Id"],
                                    RequiredItem2Count = requiredItemData["requiredItem2Count"],
                                    RequiredItem3Id = requiredItemData["requiredItem3Id"],
                                    RequiredItem3Count = requiredItemData["requiredItem3Count"],
                                    RequiredItem4Id = requiredItemData["requiredItem4Id"],
                                    RequiredItem4Count = requiredItemData["requiredItem4Count"],
                                    RequiredItem5Id = requiredItemData["requiredItem5Id"],
                                    RequiredItem5Count = requiredItemData["requiredItem5Count"],
                                    RequiredItem6Id = requiredItemData["requiredItem6Id"],
                                    RequiredItem6Count = requiredItemData["requiredItem6Count"],
                                    BlockIndex = ev.BlockIndex,
                                    Timestamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored EventConsumableItemCrafts action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is HackAndSlash has)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(has.AvatarAddress);
                                bool isClear = avatarState.stageMap.ContainsKey(has.StageId);
                                _hasList.Add(new HackAndSlashModel()
                                {
                                    Id = has.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = has.AvatarAddress.ToString(),
                                    StageId = has.StageId,
                                    Cleared = isClear,
                                    Mimisbrunnr = has.StageId > 10000000,
                                    BlockIndex = ev.BlockIndex,
                                });
                                if (has.StageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(new HasWithRandomBuffModel()
                                    {
                                        Id = has.Id.ToString(),
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = has.AvatarAddress.ToString(),
                                        StageId = has.StageId,
                                        BuffId = (int)has.StageBuffId,
                                        Cleared = isClear,
                                        TimeStamp = _blockTimeOffset,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored HackAndSlash action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is HackAndSlashSweep hasSweep)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep.avatarAddress);
                                bool isClear = avatarState.stageMap.ContainsKey(hasSweep.stageId);
                                _hasSweepList.Add(new HackAndSlashSweepModel()
                                {
                                    Id = hasSweep.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = hasSweep.avatarAddress.ToString(),
                                    WorldId = hasSweep.worldId,
                                    StageId = hasSweep.stageId,
                                    ApStoneCount = hasSweep.apStoneCount,
                                    ActionPoint = hasSweep.actionPoint,
                                    CostumesCount = hasSweep.costumes.Count,
                                    EquipmentsCount = hasSweep.equipments.Count,
                                    Cleared = isClear,
                                    Mimisbrunnr = hasSweep.stageId > 10000000,
                                    BlockIndex = ev.BlockIndex,
                                    Timestamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored HackAndSlashSweep action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is CombinationConsumable combinationConsumable)
                            {
                                var start = DateTimeOffset.UtcNow;
                                _ccList.Add(new CombinationConsumableModel()
                                {
                                    Id = combinationConsumable.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = combinationConsumable.avatarAddress.ToString(),
                                    RecipeId = combinationConsumable.recipeId,
                                    SlotIndex = combinationConsumable.slotIndex,
                                    BlockIndex = ev.BlockIndex,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored CombinationConsumable action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is CombinationEquipment combinationEquipment)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var previousStates = ev.PreviousStates;
                                _ceList.Add(new CombinationEquipmentModel()
                                {
                                    Id = combinationEquipment.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = combinationEquipment.avatarAddress.ToString(),
                                    RecipeId = combinationEquipment.recipeId,
                                    SlotIndex = combinationEquipment.slotIndex,
                                    SubRecipeId = combinationEquipment.subRecipeId ?? 0,
                                    BlockIndex = ev.BlockIndex,
                                });
                                if (combinationEquipment.payByCrystal)
                                {
                                    Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                    var prevCrystalBalance = previousStates.GetBalance(
                                        ev.Signer,
                                        crystalCurrency);
                                    var outputCrystalBalance = ev.OutputStates.GetBalance(
                                        ev.Signer,
                                        crystalCurrency);
                                    var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                    var requiredFungibleItems = new Dictionary<int, int>();
                                    Dictionary<Type, (Address, ISheet)> sheets = previousStates.GetSheets(
                                        sheetTypes: new[]
                                        {
                                            typeof(EquipmentItemRecipeSheet),
                                            typeof(EquipmentItemSheet),
                                            typeof(MaterialItemSheet),
                                            typeof(EquipmentItemSubRecipeSheetV2),
                                            typeof(EquipmentItemOptionSheet),
                                            typeof(SkillSheet),
                                            typeof(CrystalMaterialCostSheet),
                                            typeof(CrystalFluctuationSheet),
                                        });
                                    var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
                                    var equipmentItemRecipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>();
                                    equipmentItemRecipeSheet.TryGetValue(
                                        combinationEquipment.recipeId,
                                        out var recipeRow);
                                    materialItemSheet.TryGetValue(recipeRow!.MaterialId, out var materialRow);
                                    if (requiredFungibleItems.ContainsKey(materialRow!.Id))
                                    {
                                        requiredFungibleItems[materialRow.Id] += recipeRow.MaterialCount;
                                    }
                                    else
                                    {
                                        requiredFungibleItems[materialRow.Id] = recipeRow.MaterialCount;
                                    }

                                    if (combinationEquipment.subRecipeId.HasValue)
                                    {
                                        var equipmentItemSubRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>();
                                        equipmentItemSubRecipeSheetV2.TryGetValue(combinationEquipment.subRecipeId.Value, out var subRecipeRow);

                                        // Validate SubRecipe Material
                                        for (var i = subRecipeRow!.Materials.Count; i > 0; i--)
                                        {
                                            var materialInfo = subRecipeRow.Materials[i - 1];
                                            materialItemSheet.TryGetValue(materialInfo.Id, out materialRow);

                                            if (requiredFungibleItems.ContainsKey(materialRow!.Id))
                                            {
                                                requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                                            }
                                            else
                                            {
                                                requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                                            }
                                        }
                                    }

                                    var inventory = ev.PreviousStates
                                        .GetAvatarStateV2(combinationEquipment.avatarAddress).inventory;
                                    foreach (var pair in requiredFungibleItems.OrderBy(pair => pair.Key))
                                    {
                                        var itemId = pair.Key;
                                        var requiredCount = pair.Value;
                                        if (materialItemSheet.TryGetValue(itemId, out materialRow))
                                        {
                                            int itemCount = inventory.TryGetItem(itemId, out Inventory.Item item)
                                                ? item.count
                                                : 0;
                                            if (itemCount < requiredCount && combinationEquipment.payByCrystal)
                                            {
                                                _replaceCombinationEquipmentMaterialList.Add(
                                                    new ReplaceCombinationEquipmentMaterialModel()
                                                    {
                                                        Id = combinationEquipment.Id.ToString(),
                                                        BlockIndex = ev.BlockIndex,
                                                        AgentAddress = ev.Signer.ToString(),
                                                        AvatarAddress =
                                                            combinationEquipment.avatarAddress.ToString(),
                                                        ReplacedMaterialId = itemId,
                                                        ReplacedMaterialCount = requiredCount - itemCount,
                                                        BurntCrystal =
                                                            Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                                        TimeStamp = _blockTimeOffset,
                                                    });
                                            }
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug(
                                    "Stored CombinationEquipment action in block #{index}. Time Taken: {time} ms.",
                                    ev.BlockIndex,
                                    (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

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

                                end = DateTimeOffset.UtcNow;
                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}. Time Taken: {time} ms.",
                                    combinationEquipment.avatarAddress,
                                    ev.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (ev.Action is ItemEnhancement itemEnhancement)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(itemEnhancement.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                AvatarState prevAvatarState = previousStates.GetAvatarStateV2(itemEnhancement.avatarAddress);

                                int prevEquipmentLevel = 0;
                                if (prevAvatarState.inventory.TryGetNonFungibleItem(itemEnhancement.itemId, out ItemUsable prevEnhancementItem)
                                    && prevEnhancementItem is Equipment prevEnhancementEquipment)
                                {
                                       prevEquipmentLevel = prevEnhancementEquipment.level;
                                }

                                int outputEquipmentLevel = 0;
                                if (avatarState.inventory.TryGetNonFungibleItem(itemEnhancement.itemId, out ItemUsable outputEnhancementItem)
                                    && outputEnhancementItem is Equipment outputEnhancementEquipment)
                                {
                                    outputEquipmentLevel = outputEnhancementEquipment.level;
                                }

                                Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
                                var prevNCGBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var outputNCGBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var burntNCG = prevNCGBalance - outputNCGBalance;

                                if (prevEquipmentLevel == outputEquipmentLevel)
                                {
                                    Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                    var prevCrystalBalance = previousStates.GetBalance(
                                        ev.Signer,
                                        crystalCurrency);
                                    var outputCrystalBalance = ev.OutputStates.GetBalance(
                                        ev.Signer,
                                        crystalCurrency);
                                    var gainedCrystal = outputCrystalBalance - prevCrystalBalance;
                                    _itemEnhancementFailList.Add(new ItemEnhancementFailModel()
                                    {
                                        Id = itemEnhancement.Id.ToString(),
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = itemEnhancement.avatarAddress.ToString(),
                                        EquipmentItemId = itemEnhancement.itemId.ToString(),
                                        MaterialItemId = itemEnhancement.materialId.ToString(),
                                        EquipmentLevel = outputEquipmentLevel,
                                        GainedCrystal = Convert.ToDecimal(gainedCrystal.GetQuantityString()),
                                        BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                                        TimeStamp = _blockTimeOffset,
                                    });
                                }

                                _ieList.Add(new ItemEnhancementModel()
                                {
                                    Id = itemEnhancement.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = itemEnhancement.avatarAddress.ToString(),
                                    ItemId = itemEnhancement.itemId.ToString(),
                                    MaterialId = itemEnhancement.materialId.ToString(),
                                    SlotIndex = itemEnhancement.slotIndex,
                                    BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                                    BlockIndex = ev.BlockIndex,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored ItemEnhancement action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                                start = DateTimeOffset.UtcNow;

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

                                end = DateTimeOffset.UtcNow;
                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}. Time Taken: {time} ms.",
                                    itemEnhancement.avatarAddress,
                                    ev.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (ev.Action is Buy buy)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(buy.buyerAvatarAddress);
                                var buyerInventory = avatarState.inventory;
                                foreach (var purchaseInfo in buy.purchaseInfos)
                                {
                                    var state = ev.OutputStates.GetState(
                                    Addresses.GetItemAddress(purchaseInfo.TradableId));
                                    ITradableItem orderItem =
                                        (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                    Order order =
                                        OrderFactory.Deserialize(
                                            (Dictionary)ev.OutputStates.GetState(
                                                Order.DeriveAddress(purchaseInfo.OrderId))!);
                                    int itemCount = order is FungibleOrder fungibleOrder
                                        ? fungibleOrder.ItemCount
                                        : 1;
                                    if (orderItem.ItemType == ItemType.Equipment)
                                    {
                                        Equipment equipment = (Equipment)orderItem;
                                        _buyShopEquipmentsList.Add(new ShopHistoryEquipmentModel()
                                        {
                                            OrderId = purchaseInfo.OrderId.ToString(),
                                            TxId = string.Empty,
                                            BlockIndex = ev.BlockIndex,
                                            BlockHash = string.Empty,
                                            ItemId = equipment.ItemId.ToString(),
                                            SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                                            BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                                            Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                                            ItemType = equipment.ItemType.ToString(),
                                            ItemSubType = equipment.ItemSubType.ToString(),
                                            Id = equipment.Id,
                                            BuffSkillCount = equipment.BuffSkills.Count,
                                            ElementalType = equipment.ElementalType.ToString(),
                                            Grade = equipment.Grade,
                                            SetId = equipment.SetId,
                                            SkillsCount = equipment.Skills.Count,
                                            SpineResourcePath = equipment.SpineResourcePath,
                                            RequiredBlockIndex = equipment.RequiredBlockIndex,
                                            NonFungibleId = equipment.NonFungibleId.ToString(),
                                            TradableId = equipment.TradableId.ToString(),
                                            UniqueStatType = equipment.UniqueStatType.ToString(),
                                            ItemCount = itemCount,
                                            TimeStamp = _blockTimeOffset,
                                        });
                                    }

                                    if (orderItem.ItemType == ItemType.Costume)
                                    {
                                        Costume costume = (Costume)orderItem;
                                        _buyShopCostumesList.Add(new ShopHistoryCostumeModel()
                                        {
                                            OrderId = purchaseInfo.OrderId.ToString(),
                                            TxId = string.Empty,
                                            BlockIndex = ev.BlockIndex,
                                            BlockHash = string.Empty,
                                            ItemId = costume.ItemId.ToString(),
                                            SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                                            BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                                            Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                                            ItemType = costume.ItemType.ToString(),
                                            ItemSubType = costume.ItemSubType.ToString(),
                                            Id = costume.Id,
                                            ElementalType = costume.ElementalType.ToString(),
                                            Grade = costume.Grade,
                                            Equipped = costume.Equipped,
                                            SpineResourcePath = costume.SpineResourcePath,
                                            RequiredBlockIndex = costume.RequiredBlockIndex,
                                            NonFungibleId = costume.NonFungibleId.ToString(),
                                            TradableId = costume.TradableId.ToString(),
                                            ItemCount = itemCount,
                                            TimeStamp = _blockTimeOffset,
                                        });
                                    }

                                    if (orderItem.ItemType == ItemType.Material)
                                    {
                                        Material material = (Material)orderItem;
                                        _buyShopMaterialsList.Add(new ShopHistoryMaterialModel()
                                        {
                                            OrderId = purchaseInfo.OrderId.ToString(),
                                            TxId = string.Empty,
                                            BlockIndex = ev.BlockIndex,
                                            BlockHash = string.Empty,
                                            ItemId = material.ItemId.ToString(),
                                            SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                                            BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                                            Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                                            ItemType = material.ItemType.ToString(),
                                            ItemSubType = material.ItemSubType.ToString(),
                                            Id = material.Id,
                                            ElementalType = material.ElementalType.ToString(),
                                            Grade = material.Grade,
                                            ItemCount = itemCount,
                                            TimeStamp = _blockTimeOffset,
                                        });
                                    }

                                    if (orderItem.ItemType == ItemType.Consumable)
                                    {
                                        Consumable consumable = (Consumable)orderItem;
                                        _buyShopConsumablesList.Add(new ShopHistoryConsumableModel()
                                        {
                                            OrderId = purchaseInfo.OrderId.ToString(),
                                            TxId = string.Empty,
                                            BlockIndex = ev.BlockIndex,
                                            BlockHash = string.Empty,
                                            ItemId = consumable.ItemId.ToString(),
                                            SellerAvatarAddress = purchaseInfo.SellerAvatarAddress.ToString(),
                                            BuyerAvatarAddress = buy.buyerAvatarAddress.ToString(),
                                            Price = decimal.Parse(purchaseInfo.Price.ToString().Split(" ").FirstOrDefault()!),
                                            ItemType = consumable.ItemType.ToString(),
                                            ItemSubType = consumable.ItemSubType.ToString(),
                                            Id = consumable.Id,
                                            BuffSkillCount = consumable.BuffSkills.Count,
                                            ElementalType = consumable.ElementalType.ToString(),
                                            Grade = consumable.Grade,
                                            SkillsCount = consumable.Skills.Count,
                                            RequiredBlockIndex = consumable.RequiredBlockIndex,
                                            NonFungibleId = consumable.NonFungibleId.ToString(),
                                            TradableId = consumable.TradableId.ToString(),
                                            MainStat = consumable.MainStat.ToString(),
                                            ItemCount = itemCount,
                                            TimeStamp = _blockTimeOffset,
                                        });
                                    }

                                    if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                        || purchaseInfo.ItemSubType == ItemSubType.Belt
                                        || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                        || purchaseInfo.ItemSubType == ItemSubType.Ring
                                        || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                    {
                                        var sellerState = ev.OutputStates.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                        var sellerInventory = sellerState.inventory;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        Equipment? equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                            i.TradableId == purchaseInfo.TradableId);

                                        if (equipment is { } equipmentNotNull)
                                        {
                                            ProcessEquipmentData(
                                                ev.Signer,
                                                buy.buyerAvatarAddress,
                                                equipmentNotNull);
                                        }
                                    }
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug(
                                    "Stored avatar {address}'s equipment in block #{index}. Time Taken: {time} ms.",
                                    buy.buyerAvatarAddress,
                                    ev.BlockIndex,
                                    (end - start).Milliseconds);
                            }

                            if (ev.Action is Stake stake)
                            {
                                var start = DateTimeOffset.UtcNow;
                                ev.OutputStates.TryGetStakeState(ev.Signer, out StakeState stakeState);
                                var prevStakeStartBlockIndex =
                                    !ev.PreviousStates.TryGetStakeState(ev.Signer, out StakeState prevStakeState)
                                        ? 0 : prevStakeState.StartedBlockIndex;
                                var newStakeStartBlockIndex = stakeState.StartedBlockIndex;
                                var currency = ev.OutputStates.GetGoldCurrency();
                                var balance = ev.OutputStates.GetBalance(ev.Signer, currency);
                                var stakeStateAddress = StakeState.DeriveAddress(ev.Signer);
                                var previousAmount = ev.PreviousStates.GetBalance(stakeStateAddress, currency);
                                var newAmount = ev.OutputStates.GetBalance(stakeStateAddress, currency);
                                _stakeList.Add(new StakeModel()
                                {
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    PreviousAmount = Convert.ToDecimal(previousAmount.GetQuantityString()),
                                    NewAmount = Convert.ToDecimal(newAmount.GetQuantityString()),
                                    RemainingNCG = Convert.ToDecimal(balance.GetQuantityString()),
                                    PrevStakeStartBlockIndex = prevStakeStartBlockIndex,
                                    NewStakeStartBlockIndex = newStakeStartBlockIndex,
                                    TimeStamp = _blockTimeOffset,
                                });
                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored Stake action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is IClaimStakeReward claimStakeReward)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var plainValue = (Bencodex.Types.Dictionary)claimStakeReward.PlainValue;
                                var avatarAddress = plainValue[AvatarAddressKey].ToAddress();
                                var id = ((GameAction)claimStakeReward).Id;
                                ev.PreviousStates.TryGetStakeState(ev.Signer, out StakeState prevStakeState);

                                var claimStakeStartBlockIndex = prevStakeState.StartedBlockIndex;
                                var claimStakeEndBlockIndex = prevStakeState.ReceivedBlockIndex;
                                var currency = ev.OutputStates.GetGoldCurrency();
                                var stakeStateAddress = StakeState.DeriveAddress(ev.Signer);
                                var stakedAmount = ev.OutputStates.GetBalance(stakeStateAddress, currency);

                                var sheets = ev.PreviousStates.GetSheets(new[]
                                {
                                    typeof(StakeRegularRewardSheet),
                                    typeof(ConsumableItemSheet),
                                    typeof(CostumeItemSheet),
                                    typeof(EquipmentItemSheet),
                                    typeof(MaterialItemSheet),
                                });
                                StakeRegularRewardSheet stakeRegularRewardSheet = sheets.GetSheet<StakeRegularRewardSheet>();
                                int level = stakeRegularRewardSheet.FindLevelByStakedAmount(ev.Signer, stakedAmount);
                                var rewards = stakeRegularRewardSheet[level].Rewards;
                                var accumulatedRewards = prevStakeState.CalculateAccumulatedRewards(ev.BlockIndex);
                                int hourGlassCount = 0;
                                int apPotionCount = 0;
                                foreach (var reward in rewards)
                                {
                                    var (quantity, _) = stakedAmount.DivRem(currency * reward.Rate);
                                    if (quantity < 1)
                                    {
                                        // If the quantity is zero, it doesn't add the item into inventory.
                                        continue;
                                    }

                                    if (reward.ItemId == 400000)
                                    {
                                        hourGlassCount += (int)quantity * accumulatedRewards;
                                    }

                                    if (reward.ItemId == 500000)
                                    {
                                        apPotionCount += (int)quantity * accumulatedRewards;
                                    }
                                }

                                if (ev.PreviousStates.TryGetSheet<StakeRegularFixedRewardSheet>(
                                        out var stakeRegularFixedRewardSheet))
                                {
                                    var fixedRewards = stakeRegularFixedRewardSheet[level].Rewards;
                                    foreach (var reward in fixedRewards)
                                    {
                                        if (reward.ItemId == 400000)
                                        {
                                            hourGlassCount += reward.Count * accumulatedRewards;
                                        }

                                        if (reward.ItemId == 500000)
                                        {
                                            apPotionCount += reward.Count * accumulatedRewards;
                                        }
                                    }
                                }

                                _claimStakeList.Add(new ClaimStakeRewardModel()
                                {
                                    Id = id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    ClaimRewardAvatarAddress = avatarAddress.ToString(),
                                    HourGlassCount = hourGlassCount,
                                    ApPotionCount = apPotionCount,
                                    ClaimStakeStartBlockIndex = claimStakeStartBlockIndex,
                                    ClaimStakeEndBlockIndex = claimStakeEndBlockIndex,
                                    TimeStamp = _blockTimeOffset,
                                });
                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored ClaimStakeReward action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is MigrateMonsterCollection mc)
                            {
                                var start = DateTimeOffset.UtcNow;
                                ev.OutputStates.TryGetStakeState(ev.Signer, out StakeState stakeState);
                                var agentState = ev.PreviousStates.GetAgentState(ev.Signer);
                                Address collectionAddress = MonsterCollectionState.DeriveAddress(ev.Signer, agentState.MonsterCollectionRound);
                                ev.PreviousStates.TryGetState(collectionAddress, out Dictionary stateDict);
                                var monsterCollectionState = new MonsterCollectionState(stateDict);
                                var currency = ev.OutputStates.GetGoldCurrency();
                                var migrationAmount = ev.PreviousStates.GetBalance(monsterCollectionState.address, currency);
                                var migrationStartBlockIndex = ev.BlockIndex;
                                var stakeStartBlockIndex = stakeState.StartedBlockIndex;
                                _mmcList.Add(new MigrateMonsterCollectionModel()
                                {
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    MigrationAmount = Convert.ToDecimal(migrationAmount.GetQuantityString()),
                                    MigrationStartBlockIndex = migrationStartBlockIndex,
                                    StakeStartBlockIndex = stakeStartBlockIndex,
                                    TimeStamp = _blockTimeOffset,
                                });
                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored MigrateMonsterCollection action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is Grinding grind)
                            {
                                var start = DateTimeOffset.UtcNow;

                                AvatarState prevAvatarState = ev.PreviousStates.GetAvatarStateV2(grind.AvatarAddress);
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
                                foreach (var equipmentId in grind.EquipmentIds)
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

                                foreach (var equipment in equipmentList)
                                {
                                    _grindList.Add(new GrindingModel()
                                    {
                                        Id = grind.Id.ToString(),
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = grind.AvatarAddress.ToString(),
                                        EquipmentItemId = equipment.ItemId.ToString(),
                                        EquipmentId = equipment.Id,
                                        EquipmentLevel = equipment.level,
                                        Crystal = Convert.ToDecimal(crystal.GetQuantityString()),
                                        BlockIndex = ev.BlockIndex,
                                        TimeStamp = _blockTimeOffset,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored Grinding action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is UnlockEquipmentRecipe unlockEquipmentRecipe)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var previousStates = ev.PreviousStates;
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                foreach (var recipeId in unlockEquipmentRecipe.RecipeIds)
                                {
                                    _unlockEquipmentRecipeList.Add(new UnlockEquipmentRecipeModel()
                                    {
                                        Id = unlockEquipmentRecipe.Id.ToString(),
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = unlockEquipmentRecipe.AvatarAddress.ToString(),
                                        UnlockEquipmentRecipeId = recipeId,
                                        BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                        TimeStamp = _blockTimeOffset,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored UnlockEquipmentRecipe action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is UnlockWorld unlockWorld)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var previousStates = ev.PreviousStates;
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                foreach (var worldId in unlockWorld.WorldIds)
                                {
                                    _unlockWorldList.Add(new UnlockWorldModel()
                                    {
                                        Id = unlockWorld.Id.ToString(),
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = unlockWorld.AvatarAddress.ToString(),
                                        UnlockWorldId = worldId,
                                        BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                        TimeStamp = _blockTimeOffset,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored UnlockWorld action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is HackAndSlashRandomBuff hasRandomBuff)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var previousStates = ev.PreviousStates;
                                AvatarState prevAvatarState = previousStates.GetAvatarStateV2(hasRandomBuff.AvatarAddress);
                                prevAvatarState.worldInformation.TryGetLastClearedStageId(out var currentStageId);
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                _hasRandomBuffList.Add(new HasRandomBuffModel()
                                {
                                    Id = hasRandomBuff.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = hasRandomBuff.AvatarAddress.ToString(),
                                    HasStageId = currentStageId,
                                    GachaCount = !hasRandomBuff.AdvancedGacha ? 5 : 10,
                                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                    TimeStamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored HasRandomBuff action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is JoinArena joinArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(joinArena.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                _joinArenaList.Add(new JoinArenaModel()
                                {
                                    Id = joinArena.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = joinArena.avatarAddress.ToString(),
                                    AvatarLevel = avatarState.level,
                                    ArenaRound = joinArena.round,
                                    ChampionshipId = joinArena.championshipId,
                                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                    TimeStamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored JoinArena action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is BattleArena battleArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(battleArena.myAvatarAddress);
                                var previousStates = ev.PreviousStates;
                                var myArenaScoreAdr =
                                    ArenaScore.DeriveAddress(battleArena.myAvatarAddress, battleArena.championshipId, battleArena.round);
                                previousStates.TryGetArenaScore(myArenaScoreAdr, out var previousArenaScore);
                                ev.OutputStates.TryGetArenaScore(myArenaScoreAdr, out var currentArenaScore);
                                Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
                                var prevNCGBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var outputNCGBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    ncgCurrency);
                                var burntNCG = prevNCGBalance - outputNCGBalance;
                                int ticketCount = battleArena.ticket;
                                var sheets = previousStates.GetSheets(
                                    containArenaSimulatorSheets: true,
                                    sheetTypes: new[] { typeof(ArenaSheet), typeof(ItemRequirementSheet), typeof(EquipmentItemRecipeSheet), typeof(EquipmentItemSubRecipeSheetV2), typeof(EquipmentItemOptionSheet), typeof(MaterialItemSheet), }
                                );
                                var arenaSheet = ev.OutputStates.GetSheet<ArenaSheet>();
                                var arenaData = arenaSheet.GetRoundByBlockIndex(ev.BlockIndex);
                                var arenaInformationAdr =
                                    ArenaInformation.DeriveAddress(battleArena.myAvatarAddress, battleArena.championshipId, battleArena.round);
                                previousStates.TryGetArenaInformation(arenaInformationAdr, out var previousArenaInformation);
                                ev.OutputStates.TryGetArenaInformation(arenaInformationAdr, out var currentArenaInformation);
                                var winCount = currentArenaInformation.Win - previousArenaInformation.Win;
                                var medalCount = 0;
                                if (arenaData.ArenaType != ArenaType.OffSeason &&
                                    winCount > 0)
                                {
                                    var materialSheet = sheets.GetSheet<MaterialItemSheet>();
                                    var medal = ArenaHelper.GetMedal(battleArena.championshipId, battleArena.round, materialSheet);
                                    if (medal != null)
                                    {
                                        medalCount += winCount;
                                    }
                                }

                                _battleArenaList.Add(new BattleArenaModel()
                                {
                                    Id = battleArena.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = battleArena.myAvatarAddress.ToString(),
                                    AvatarLevel = avatarState.level,
                                    EnemyAvatarAddress = battleArena.enemyAvatarAddress.ToString(),
                                    ChampionshipId = battleArena.championshipId,
                                    Round = battleArena.round,
                                    TicketCount = ticketCount,
                                    BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                                    Victory = currentArenaScore.Score > previousArenaScore.Score,
                                    MedalCount = medalCount,
                                    TimeStamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored BattleArena action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is BattleGrandFinale battleGrandFinale)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(battleGrandFinale.myAvatarAddress);
                                var previousStates = ev.PreviousStates;
                                var scoreAddress = battleGrandFinale.myAvatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, BattleGrandFinale.ScoreDeriveKey, battleGrandFinale.grandFinaleId));
                                previousStates.TryGetState(scoreAddress, out Integer previousGrandFinaleScore);
                                ev.OutputStates.TryGetState(scoreAddress, out Integer outputGrandFinaleScore);

                                _battleGrandFinaleList.Add(new BattleGrandFinaleModel()
                                {
                                    Id = battleGrandFinale.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = battleGrandFinale.myAvatarAddress.ToString(),
                                    AvatarLevel = avatarState.level,
                                    EnemyAvatarAddress = battleGrandFinale.enemyAvatarAddress.ToString(),
                                    GrandFinaleId = battleGrandFinale.grandFinaleId,
                                    Victory = outputGrandFinaleScore > previousGrandFinaleScore,
                                    GrandFinaleScore = outputGrandFinaleScore,
                                    Date = _blockTimeOffset,
                                    TimeStamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored BattleGrandFinale action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is EventMaterialItemCrafts eventMaterialItemCrafts)
                            {
                                var start = DateTimeOffset.UtcNow;
                                Dictionary<string, int> materialData = new Dictionary<string, int>();
                                for (var i = 1; i < 13; i++)
                                {
                                    materialData.Add($"material{i}Id", 0);
                                    materialData.Add($"material{i}Count", 0);
                                }

                                int itemNumber = 1;
                                foreach (var pair in eventMaterialItemCrafts.MaterialsToUse)
                                {
                                    materialData[$"material{itemNumber}Id"] = pair.Key;
                                    materialData[$"material{itemNumber}Count"] = pair.Value;
                                    itemNumber++;
                                }

                                _eventMaterialItemCraftsList.Add(new EventMaterialItemCraftsModel()
                                {
                                    Id = eventMaterialItemCrafts.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = eventMaterialItemCrafts.AvatarAddress.ToString(),
                                    EventScheduleId = eventMaterialItemCrafts.EventScheduleId,
                                    EventMaterialItemRecipeId = eventMaterialItemCrafts.EventMaterialItemRecipeId,
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
                                    BlockIndex = ev.BlockIndex,
                                    Date = _blockTimeOffset,
                                    Timestamp = _blockTimeOffset,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored EventMaterialItemCrafts action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
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
                                        var previousStates = ev.PreviousStates;
                                        var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                        var avatarLevel = sellerState.level;
                                        var avatarArmorId = sellerState.GetArmorId();
                                        var avatarTitleCostume = sellerState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                        int? avatarTitleId = null;
                                        if (avatarTitleCostume != null)
                                        {
                                            avatarTitleId = avatarTitleCostume.Id;
                                        }

                                        var avatarCp = CPHelper.GetCP(sellerState, characterSheet);
                                        string avatarName = sellerState.name;

                                        if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                        {
                                            continue;
                                        }

                                        MySqlStore.StoreAgent(ev.Signer);
                                        MySqlStore.StoreAvatar(
                                            purchaseInfo.SellerAvatarAddress,
                                            purchaseInfo.SellerAgentAddress,
                                            avatarName,
                                            avatarLevel,
                                            avatarTitleId,
                                            avatarArmorId,
                                            avatarCp);
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

            _actionRenderer.EveryRender<Raid>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception is null)
                        {
                            List<Address> avatars = new List<Address> { ev.Action.AvatarAddress };
                            List<string> agents = new List<string> { "0075bebCA3d3b91F88aDdec2D49831E806C73C02", "00a3b0e2c84a896064Db2784E90ff604EF0afa89", "00C32B4d7c690B5F900da469f4302f6496810B62", "00f24b262Af2686dDeD6728a94EA3AA7774Cc9e9", "01069aaf336e6aEE605a8A54D0734b43B62f8Fe4", "015A804063eE198D1ea4FA70DB856F3Effb97E12", "01675d3f6CDB0ea558d2AF4202A703faB6DB3EeA", "018B9F14777920f4CbEBDC7C4C8d6b8180CBA782", "01E1d5777CD9C091D685F30E4D901d09aD74E84E", "01F77880fd40BA916fb3091Ae0D3094DCA2235c6", "020ee2fB5f44Dd61334c4e89Bd29461a2943f093", "02229827ae99bF1AE0F0068654AB289776032189", "024951eaf6c21fC7C40c9af8a8b653E94190B5E6", "02aD17F0e635913e61d666c6aCEb04360Cc34F7f", "02De230b84AAd10f6D8c88Ef32213387A477EA4c", "03123dd02B64fbf1be9771D853Af462A209621d0", "0345E72912B847db9a671e57c41a61619E6f1650", "047badAb6FAc558ab7281856E0787e9432607066", "04c755b6db3BC957D0b2119887499D944e41C346", "0558A7d65159DdF3e17dD489107111061bcCab94", "056c7e5ECff69B1eF668F951c10E7466fd41B396", "0594728EE2FF6F3A70b97300D9236FeeE94Fb7Cc", "05F4b907B185E5bfB6DCe51c3c13b26b3CfD2b30", "0607Bc7516DE63cecC323B61e2818279d00c3137", "069cb175693c043f97E7BFfC1df8A3d5e199D0A3", "06C5F5d0d8849621F6DEE734D0012D762C9977dc", "06Cd44B02dFF777d0F04267eFB4F494fFaf6b036", "06F75c403b12c7857b867DB4f62AA0001641818E", "06FaABfE31439cC30f293Cbc2315be04C64AD466", "07038BEF9AC34D50a699192C53A46e9e873cC5Ab", "078D8951e67358ae13A7132AFB23e7a9B2F3C08c", "0851fD7109D0A855e9E44271d16a2EfaB593a2a0", "0912ffe4D9313f0Bc6279004695e6E2502fFCBCb", "09C573368B386c5962F3d9e772412C054589a84E", "09e66Ddb1EFB0cb80724F9B8B89BBBDa563856BA", "0b4A782d16117888aF306D61dbE0D7BC2d872919", "0bbeAbAD519018e469DE5A0aEeC03DF9b7b6A2a2", "0c33f87f3C3613F072c5b054346bf722382944c1", "0C3985A13b833f0a00c90E82aa1f49CEa1A7b007", "0d24dbad18f21446d1C7B04199a62497C544A9D8", "0e3259895a06473aba68181347F748d8f4b754a8", "0E7745D2e9553037f2B8c2A226Cb4F348920d221", "0e7B1FfeE7Ec94c36b10fA463bb5048dB2909ad1", "0e82106B51630C52793EF33Fcc2b2563BecE32Ef", "0e8E268afD8c520e85dC9862c7943168e903550b", "0E9C57c46099B3a638E159Ed78D8a7e1d28f2cBE", "0F2347C7798E48A3BA2456ef448dAE735B422882", "0F663f24D6119ad400c29e695E8bf333B7479923", "0F71EbCb0F7e8f0274BA935d0aD01890dF97d780", "0f7207Aa123142F39c0D49847986cF89feB0e994", "0f85A0798F109c43a28C7b5c183DB7848C1b8C5F", "0Fc1ad662c4c53392171759B5dBdC18380Eca6d5", "0Fea268F2c1bF19d0D3731AD20da0ED35a8443c1", "1012041FF2254f43d0a938aDF89c3f11867A2A58", "1027f7184409179AcCf912A620278C8Cf8Cc243F", "1090DF3A5743dCDA546b33d02d5067765394f146", "109E5eF9846b945b04c0bD10012710bb2b663e3E", "111613AFA0dC35ac1730eC8078CBee952f82f936", "11A239C77f8682Ee01e15a09abd53E4F3ED37A52", "11e35183722eB0542dc0Fe8102c9a2C792aca599", "12207f32b3a5a1F8516D3f337da8b8E39240515f", "122F6a5F9ADe822037a7e35BD81d5f1558C9F33f", "12D3f0e15dAE97ca7067BD500372b32f12575fAf", "130FBDA9A9601980286E599BA2e9B4F3FD87bBf6", "1347299B44c62D4Ba749b19Dacd6381778902D35", "13C43Fd41F79C3FB5818A8acaD377B065d8144dC", "13fFF2F75605b4e1F869F108b10e3D13770ae017", "1423dAC12349ed670393C17E7725764A06fC16EF", "14253A1F5619898eD16B5586977f42Da759f5CE2", "1455aF7a8352b6c5C4FC5e1Bf9296Aaa384D6a4E", "14DB60F5Ccf8618fC47009a4a1d75d8f6f7a3D6C", "1518e99d7970e82B161b981Cf7A557b5e6009FfB", "1592f246839839C76AcB039456Ed12e1cad29740", "15FD90F3c1b43696376fD519D7Fa27B400cFC7E4", "162B98C41A2e9142b44780999252Fb3f444bd55b", "16D9Bbc9470ddbc82D971dcB6F20124f449520e8", "1725429581747D0aDecE54678d94049C54ADFe09", "172869e4eEc6aC51fF0c2a64068406921F466F3E", "174ec8E4D7d22794DeB5382B307a2A70B103A31d", "175131066356d717C9922F8499C0877ceB65b2E9", "1774cd5d2C1C0f72AA75E9381889a1a554797a4c", "177F0473BbfDAAC4EB83E9b4fED3727E7cEb0A45", "17f99D053D4e0E859b4c58825b129311AaE38E49", "17Fcd5bC05471c55B06ecF769c2429Ee789E3AA9", "181Cc10422798827de47b27C5990227F594d4264", "182D3B9f3d96E45DD37e75A9fcC06578De2951db", "189Ad2C51fcD2926A36AE7f13818954265491d4B", "193aAC3EB68F13E682b7983534f01899Dd1381fb", "19E1345E784E6199750C91d5EF3cdc5a4528abD6", "1a130Bf4C55AEf7DA93E8B7a69483F117F65d846", "1A1d8e9ee81e0E680372F715501DD1b4aCDb90e5", "1A2452Ff052BFEaC4800f4bDCC5960afbB06c5c7", "1aA947c6328a08EF615cb88695c8736A8a24df65", "1aae9C97984961FCc5c05f9DF382e28fcF8EDe41", "1b57ff1Ede28a462204e7e314A3B2663813dC9C8", "1b6C15679d203Ef624F71b0c52E9Ff1A7A41BA86", "1B733A24310E51756Efc34fb7A007bC019A86def", "1C04b020ace8234362969046B31642c70D5Dd7bf", "1CCF48bF6e1bbE113DBDd6fdD6b08815BB0ed7EA", "1Ce3f55DeB40fACB876A12C1c443418B389fA7a2", "1d286946fffB3Cc7F981030dFAa52e825F662161", "1D5101FB1A9B289b483cbbED6D81999CD919af0F", "1Dc75384a57A1c848418B30cd6867a97E02a4bD0", "1dd315e66f8Efe9eCC8ed60BA56a446D62337933", "1E3A74F191bA8Cc134bdfCA8b9a25DCD555274E1", "1E6688fd5C3C4639061d98c55A26990e5bd9b498", "1e7ef4BB2FbD3ed0fF10338c6C9807b5aFE6F30B", "1Ebe622404bDC74ff417798E1b39AeD07F425E2d", "1f4bA7a772CFe4Db28398Cf47437F49aF69434DC", "1fc7b0a845dEC549A6A81fb12E0fb8b84e55b287", "1fdb60b0B0147091Fb5Bcc72B8bde401F8825D8B", "209EB6661eC7BF6948e361e26b4183c796F1d1BB", "20C4e8C44D12C3bA8c6AEfe947FbF29e86E7C192", "20dFb11f85c77508bB8304E07865E197836fe72F", "2116a161D3da2Df117822b736b7ce61d86194d20", "219E8282cd0F2Df910Afe26dcb6824F8d24E4dF0", "220f841E14497acB27dFC7A42B4cFD4bFd322A08", "2223446852825A0E4774A32cD0b08Ee538348cA2", "22387aeeB534aB422F03dBc5711F8a1D9A3c8700", "228F912b4632e1EDC45Ac069e5C63cFe7e24844C", "22A28AEB62A9ED8DF939c1D18dAf1d64fF300Cbb", "234C5b1cAa45DfD4bc5418eDDEaC49578e88A727", "23d02761E6C61EB16d9d184CfA757355ce3857Ce", "23dE4Dbc0aF2946d31f7Da5523f8DFf23F39cD2d", "24b9D5567D414eE257Bb8BB50Ff8d9B49F115597", "258A3271830D04Aac3aD1d68EC67c8394598d11F", "2592c7E60B2914e16a8d1396E21779a3707119de", "25aC5db59829865C76D27301D2e5Db716186Fb54", "25E9a29e1030D485910fFF501828B2D38956a1fA", "269CB95c421E0C1b1a770F1Ed47E73e09A1333ED", "26A1e4FD4CFdb5b52803ab92cf7cBE9e1602b28B", "27480439753f7b29b3989780588Cb90e2Fca070F", "2775F537cc6d6891535b1A1F6bae330bCD238965", "2792549218347076863b793219290cF9438FC384", "2824d01C33d656E884919ac74aA9D97CF39EB640", "288816E15A692A42BFAB1EE206ce0656Ee3Fe871", "2888b082DA1277DBd403D08A985d2f10BF46CC7b", "2902246D6D2146E9C7A78e7ee969a9bf1A208030", "290D42C5202f37EE28C42b9C82199f012754DF60", "2958a1ee4850054f703E8236a228ac8A17189ce3", "299EE38908Fe598E9B001d3b2193B0f108012FeA", "29aa3D7d52283c90828a5A31abC7C0aFfdEd9E67", "2A164BdDbB4cC05D07bB5DacAE02C557ce827394", "2Bfb9997B5d31904A86fb51D99b3364bD747ccE7", "2CA3E2CCE1e85a618b2048cb95b5dc3907F0D918", "2CfD3A844313935F4cA5a314367eA645Db0d186A", "2DBC9679ADa0d493630aB5CC631406944d23eB5a", "2DcA85c97231C09c34A73826215b514F324B4344", "2DEECE3972fD1788859CAB3DDb14f365eF93A3Fa", "2df1f7fE2f79f3CfEd9Aeab34d2bf9cA67AAAD01", "2e97F05ED2AdE9d5420b0269Ce8EB66D10EF9DF1", "2f8a40FD4F133568dFC84bd1e850D243c9c04afd", "3016a74599Fbad981324c9DC8FF87325c11Dbc7B", "305F6D3Ae0164Bb340C09FDf9B5F8E1E33f5C829", "308589BC3627Ce31B31479b61E4bE4682233A158", "30A32CF3F23d27d9cFB228D241D2177f4a67DdAc", "30fa0873AE5a6f1c71C5CF9cB5f86919BA40B2EB", "31720E986EDfC0EA188A5fCE1AF8A6c7Ed96F433", "31d9cE1EeF128931F312dAFfE44a8232642a4C40", "31F436CE8991Cc0e6796B49891FE474de9a0487D", "32151c839e52c172EB8A896d26770B401A9aE088", "3232Af7eaAefDF1114340565d0BF47fb4080cd6e", "3298419C905511Bc5153EB47B5da6b079732Baed", "329c5F9185244402306fFF03a4e6d750522A050A", "331486825ad674ecc268A7a9AB2a64D924786C94", "33BEAcc7Da639653848bF360460d22a8BfA3Dd0b", "3415c0d91595B30c415034E4722EdC58c938e71d", "341c1179CFF290bE40649B5D967a5f77A89392f5", "3428225D9203E6ee9d3a00799c801008928029Ca", "34522bB502545D1e598dE5b2180E1aD9099d77C4", "3518e164B0bEEF2f7F7c30E243bE88BcFB04bcF5", "357b1e1df5a37547205d8385836Ea3D7016f8D67", "358CF01171E05bbb4c20CC1A4c7940f5daC65a6b", "35a236518562F8A41f2D68afaA025874B3C9CF9a", "35f603e41a59c356E65dcf23f7e275Ae2847eB9f", "35F8715Fb44527284296A7192134748D31D0E243", "365BF0eb182239e76638b0EE2dea149b65B1B2c1", "368E695b0D8Beffe0CAb6CB3305931c6a1a6145A", "37D0F3799f09b455Fe5AF373579196c7CFB45AF1", "383004c1e4a65b59A3Fe2472B773D848659f2B89", "38b6A6E5cd27a6C46Eb1C5bd1C5FA088C7Bdb554", "38c4F3A4Fa4dFF1588282650FdA8b1d8AD226009", "38e145B6e4eaE492B5d0bD0B3047Bec78a087ad9", "38e782a80Bec4B7821d14CFc0b49489aEB0AfF42", "393631DA09afBE0A7eC517D68fCD2cE4E747Df01", "3995DD31D377A84778865b35F51aEfdC784190f8", "39A09dfC3B9F187214cFDCFDd703E30184813785", "3aCf9D27e3aB27f14b569102606C41cCB4C59581", "3B03693F4C5805a64b496F8b0Fae9C05efed2bfD", "3b35F066E67D4bc80EB587fB0c6f930e85E8995b", "3b5C9Ccb5E2413788262b8D06C7973B2B0BD44c5", "3b92f465343FB9f7bE45495a3aE8809c82Afb9E5", "3BdC87fAb06CF3358F87c0368b3086f288f7a95f", "3C070f2AdEDee363863A5e12cF8d21D7a969a272", "3C434b47Ae7c1C45bCAb9773B4a52CbD0227137d", "3cA75621C61d80940E46d89CE1edf1E0A155E497", "3E8e755F0b7FeBD3E7C84BE6107231e1f2b7D46e", "3f360148D27748d7Ab0081e210614E45274C8013", "3f57C197e231DEBE35D2771F9daBCBfd87749b8d", "3F6DA590bEe03Ea6A7DbAea4De04c0da37E44183", "3Fac2AAA22B013487009C14C266ee29d73a3c8C1", "3ff244E92191351250f7b7ef31d8D75329e2B919", "4002b34cc5aE319001F96CcDaB8b3035b24A19FA", "404A0fBcF3287296b4Ef86977Fd77bFBfF1b75B1", "409c6285B5CA7bcDE9ae5B128dF70e820AF276c1", "40a37F7C9c35401cefF82F8BFfECD19c27CAcE62", "40E23D342347DBBeb1ec4285812Ea189255EAf8f", "40e7CD32cd7ba51BeCE162C8C27761231b1854CD", "4116E16433bc3DcfF87f759D49bA9802813528d1", "4239dA27982C6d1381b070662e653F4262fc4795", "424fe7D4CE7Ee51b662da81Dee4c05286a69bbA8", "4316a60737D77723802ed62ec1A5AC3771833F72", "4448aa8a3432186C9365B2A6c72916d606646566", "444c61B3090e0B3781Bce7856aCBb720d4e32A0A", "445df64F5aaa9334c3094b4248364F778ed71AFf", "44f10ce9eA5B5CF43cBFC21ba34F5AB861D1b772", "450CCD8b5352C0C67c258Cf87abE81Af77054d8E", "452A4176f72066B0F0774DfE7733d9B8f385e289", "4645EeD0A420326F962160f0fC32873f62da14FD", "46528E7DEdaC16951bDccb55B20303AB0c729679", "4656Ab4eEB7a22C7B9A7740c70551F7069f5F185", "47503cc23d3F33fd97984C147a6C5c9BD7d7D314", "475B43AE4695cA5Ec7Af513b9C8be8F921D58C44", "47639Bdbf9ffA28dBdCe3ec8E84977B8132EEa80", "479E1B355D31Fc9234C75134EC330Ccf44Fc5545", "47CDf5bF8ea48b89A2bBf46B71969227fA3d4BAF", "4820b8EE941c79EA4cEEb6D2875BE81b701ef6D1", "483F05C66ddDc1eAb071083d64C425DB9550557a", "48d2Eb22b9B7Ed45e74B0BAbb71aEb6Be66F881a", "4921844f4c1EA4897D27a44644125898ddF1EFFf", "495e62C0aE74F3867b767408dA0B67cEFbEf2EC9", "499ca1F30dA36d33B08ad88Aa553D3C5897e54eF", "499FB52B840276B5EFE0c6cD80224f79500e6ffD", "4aD5b8f49f46DEef367AAec49226B0E928eDFf2b", "4AE71F350e1cf3A2C9Be9452c9DaF1Eac2afaB70", "4B2C3937fb9d7916765418803CE73C7b37134Df2", "4bFe876c896882765Df5Bcc4531D58794d3F8a14", "4c83cc0B3E3deDe38C081d7767c44c598E5d2776", "4ca885506Cb4cf69c6a402fBd3b08AC4d0Ce1F65", "4d858A073bdF1bb5E378d870b6B7152e23BF8988", "4DB131eB65040598d27BbfA3a2868EaBF6cde7eF", "4db4ADb64FA9600713Ca5082cf3A469C926717eD", "4e1980abc15F98F4992Dd9A52106711097432f0C", "4e5620c408DA2C18dcf2844891460FbBb2280317", "4e6BbFD8743E92Ae2F7D7418B0a2DCb85CBA6486", "5093038c804BDe743AfA6FD2E480B946c1a654BE", "50C2fFa34579c18ee143B657D8E0221D0A6d2A79", "50DF875CBdDAa0AFE9c20554e988dCa97395DC87", "50f6F01d0072C480573f958E71c1fa0C13a7bbe3", "510bD3E5411bF46621Af02769096FeCCE1B56bf5", "51cA9df0E16B4d372600AdE21f085cF8820B8caF", "52944F82dFfD11ad20108D9695d5F42cF1b766B3", "52C5D7e803fC3609b0A41214e02E89d6bbfA63D1", "52c995756bbd58401DeAEaEc91a05Cfab24aDc5f", "53160723c785f0F04FD7A8FA6AA2FF014153bE6d", "5324d19E8FbBdC65e8Ee33A61D025B645F49e1Dd", "53f1ea2F7a3CD72b9B9A8523ae6f88C60ec9DBCD", "541f11FD0e81534c1Fc7F564B1A9cd7111c8ed98", "544460091Ff880E436878B24f5897bEA7bA112D2", "54BC8E2CfC8f4786c65ad43ef711236c472b9B40", "54C8C21243967d925A2C6396fB68563e49F7D4E2", "5542a5b622d3da0fc86ea8F6b460b7dEB6065FAE", "558143379EBCA59Ae975E960Ffea4b250Bb63092", "558393b9B423652ae630eA2a95e1F4dEA907Db82", "558A49C4dBaDD1Af687AbE4811fb9e9E1cd329a6", "559301F907b83C131258e58FF53623b4C24AdE66", "55C00f6879Dc67b52d873A6bCd9e936BE6a81347", "5630D7C2228D9Ba6a770eB430c7186cCf022af29", "56803A7082C6b9FCf496Ddf38f74EBE740E39A8a", "568Fc7db5BB0b0c6d9d3f974C4f7D84C434E765F", "57321AAE63F651A8870dee929b7041Bd23B70870", "582449DCb537ea19b127da8E1339217a0d8B957b", "5831aFFB7bd95286775B59e2f8d52FEA26E1856C", "58742d993a61A2148D44a9b61A32249b05Fc992C", "5898e2EFBad6af516CC36FE81d437C08e5bd89c9", "5902a281FB16AF4C244A11055A639A34E2c244a9", "592e17F6f9045f584f8312a9bD747Ae9de69d538", "5971BcDEad48118e26c6196B23562E050eeEa311", "5989fA7958e292B17A5c892Fa66e79F7984394C6", "598a34fE157F808BC6D0a108EC35d02E4394Dae2", "59e37c172994A9399238b8907F436F13DA225A4F", "5a8cA1BaDbb57E333D8D1bD4d2bdebdA4b1E4b19", "5Aa38377c1D0ec3C6D73514C6EF2705B64a9563d", "5AeA3E478D9C84421c8E4Fb62Ba5fE67D298790d", "5B2AC7887743d9430Ea38D1F7e1A57A696669974", "5B47AE377beA2cB3DfFEF8cFeCDC15b53CF36426", "5b7EfDA1a32EDB9668Fc44aD4Ba773B07A3bf674", "5b8fD81c243027ea4E71BaBa5A5a017791e85cA9", "5c3115517495066D3F51985E19d30C5f8912C4FC", "5c930445255d185D5Cb45F693c3A00C4eD8eCE2b", "5cd21dBC7FEE4b03124bD7Ba4285912159c78fC3", "5CdcF2568C773AC551E4B31270c6dAea3F387395", "5cFb925A861005202af99fc5968cB2829a8E59B6", "5D2cB8e20FFaf27b28ff99d25FF0B3650699821E", "5d45140cCc257AD7A9eCE607d3eC8b830a356020", "5D476292Ef0cEfC8DDEA3998ed1Aa1abB533d31e", "5d589418e93F6F5FB02FF1EDBDd8F62d7098029F", "5dCE62808756fC8C001426cC98Df5A09753494bE", "5E6Cd5cbBc48411255511576ef9F05d1523E0D02", "5EA8E83D178462a42b3F4190b05b0A3789EeB99C", "5eCB61F1197199f99A1c4a9544b52653Aa977c93", "5F04F2C73215B446f6E44df38877E4C7cb5cF857", "5f35D30Af0dE87231be0fc6DEec8fe29196E7Cf6", "600F18C46eFb9AB8b0d195E21E5C3424a9e6d3DD", "602265Fae50382E237dF1f04e2f60474Ad1A920e", "610B41f68ffE7A17441EA81C6a21D99C134c01e7", "615d57Da56A108C677929D345dEE6B5fCE2A3d20", "616f6Ed0ec8BF0b1b90EcFD611357C06EB44577b", "617501213c115dc5F6589Ed4E61873734848e02A", "625D3aF1b2D9f6D13fc91b7AF6dde41A3c72B2AC", "6284d7190C257A564394E849527f61E5afc7C2e8", "6292a078D506029Ce23eF6b923EB1956d5B39C78", "62Df6d37d33A80B76525310a43966ea2319F7D2c", "63048669481844DF7EF12ecAebB0D82768DCc1b8", "6338aa47485C28c479109d66fE6092b54CbfD0D5", "635D8c457Ce16f188d7c7b18ED0C8d0051b9655e", "6374FE5F54CdeD72Ff334d09980270c61BC95186", "63D092b0029f5A663B01Ee4D09836f6198846BB4", "640B80F1ce5906e21e7A93AE48a110CbE8CADF98", "640Ea05b28B855fdE6cE1f78FaB0667fD1a1EB5e", "646e8AC4228004d8ec1AFD474AAE350FF1Bd9c21", "64FB1DBbBAc395B629997962DA25BA02a09FaBF5", "650F91E91003045A0B0A0D879dD07720d80c1f8b", "6513A66945137226747791f7e6CBe7abB0b5c289", "656dEB2BCe0D048E968126495d0fD9CF56aA27F1", "65A1E52f03E6cdAe30954a488764edd79A208f67", "65a81bFdDa0d28437097eD3F3096687Ae8af9a87", "664BCC8ff19dbc3195182352fDf17609aE64d3cF", "6697931319f413D8ED35be2c6595a27cF0AFdf97", "66Cd976CD1b92F99DA460D11dDC6A5B90c75232e", "66eA23e3625aBb8E90c4466f60f2A7770b3Dc797", "66F689233B05Db577B44e4547716d3371166b075", "67085817219119f9b6310898f6FD870d6686e0B8", "67652f7Ef20F98D01DC6F6E0f058878751a93799", "67A9d7011D7b5D4dE06D454D6279B5f30d89c21A", "67Ab7fE6971423a3f399a9BdAdF1e9E896A867C1", "67E799d7A57477644B7835F3722861531407173d", "682f0e2a66813983C9c095E758C9A705a03a6544", "685aF8Dde484337c13665FbC4d535A53a6C03029", "6882a44582e556A691E9E0892438a98BD9Aa9695", "68C76f99B56Bfd4C256C7995476Ed4202F419Cfb", "68Dba8C8EA1ee181b2304928EC6E852120a5ee5D", "69CaFE30BBE46FfF74868FdE603DD59fF97338c9", "6a08598Bba23988dA373D59D1D6144d76eC2Ae8c", "6a2CDd406dfe590e0d7Ea206891eA400deCf3E0C", "6A45d14F67826d814137B9c382AF3D9Da59153A4", "6a66499069bcEd08430d680CcD7CBe032E93f2B8", "6A71F62f5E77ED8cfcF6326741d808B6D4A39641", "6a8DCa318D6Cd0D5cE6E081978dDA87Dd594d8b9", "6b146bfac046D0235a7B3c7723FF03Fe3b33dccD", "6B4FBa1d2f6F25a5335795Da9DD518d1777f3907", "6B5a621F8a4390aC66fB0e088DCd647C0A09860E", "6c0c279f5A8EFd85d2b2a085a7aa105bf472b729", "6C39732790D159458EcA107625331e1e5224e2e9", "6C45ACd310DA722BA429C8CE0e71Fadc2eF374Bd", "6D5ee4BD59997310eC47b58166006f1691D5F799", "6eC904b44BE6B22f93782f24365DAA6226A5963e", "6eF4DC9e7c8AafDBfF6058692d05345D3C4a6ccB", "6f0BcfE9D9183941fEFEeF986448de0c7DF2D328", "6f80FB02d515857A0717C44D06393d3D1E7940Ac", "6fa35c927aeb8945260feCbE57E37C4B02C56455", "707f782B98d936e226983C1b2A8619EF4B366808", "70BF32a2908774E611Da2bdF2006C874FD05E27d", "70f6aD11A73587A08d0d43A1Fe17F03A954d1323", "7159B36FBE6f2a91CF151abFC824A0283757F122", "71aD3689aeb72c06d1107DffC6a35887c5D571DF", "71bD8667fb68B76e442d7f1eB5dCd30A0A74e0FA", "71c1d5a7e6672A908419AD7ab9d9036E55D6f48c", "71DBDdC88a44B9428B87F76509c6203f93DF7597", "72EE3A0231e318116DF61d1d28ea7A5F75190a11", "7312D3eC9DCc53B6b64E104dF80de087C942248B", "7354abaa8fD3faF837d12849ebF4D3B7624939d9", "74064Eedc5b5477Ba806853a94EB1EfAaD475556", "749624458CeF62CFF4d5075FfCE604A3bbE7F061", "7502e24744A6C7644CA71982A6097260cE3e6177", "7511eE4eE0A1c1DEE58945Da6752029bEB0A6Fbb", "7526a92Dd452076B69C5705602Ba2FD22a4D15Cc", "753601F6335Fd48Afe37af00fe825490F8E5d98D", "75A7a447E701Fb1f5E1649a69fa64bf08b6C1748", "7623c6C45b85D57257Ba01F09d76821607A5D309", "762f829F8A4193828e0b32ad0Bf27Db35f5B2914", "76417c30d60d81404dc3C630A51FE37b44740ec3", "7694D80880A2A6A658188BB540c084cF11E5785c", "76BFf56683CC4948eAAaC860c29B9e369b8D7eaa", "76dD79c4e3Ca18412519Ac01dDA1cB9A4535D051", "7740E73503Ff182aa0564b91A31EB536C92a276A", "77A9EE4E87ebE5E8F936B7496AB26552F6578E6c", "7814AC44BD59B843d88A588100D6c9Cc95515346", "7847822903742383412358c4D149Da4261772Fd1", "784bDF7EA502aab7189600F2378348563Db4bB36", "789d2903ef31068Cc9Deb0Ad05324C3213033D06", "78D9a6688375660832789Fa1018041222b6a8F4a", "796d6c65446596FEB2aa86B6ef27017852b4Af4F", "79B023954A64F3AE648c2fF3D4f85535F7e84E06", "79BF17b6D144153C677Ca2C1871490dfd16D5D16", "7a0ebBb22B467337608DBE8c692d877c2CB178ab", "7A380b5b7a51F93E0Bd3C4E2d2F6ECd0d8C80c04", "7bB523d5a747071Dd1611F68b5361A7b41cc94c8", "7bd1E211Bb7af9Be758280E02297a28ACEc5ACa9", "7c79E93ef57420ae089731185aa12b7597F4a159", "7cb482f031b021384993cc99072722CeB1Eadec6", "7D1FC34103A199d645808ad8E91064442Fa140bE", "7D6C386aAb01D57Fc85CA856c73c5A073954ab03", "7Da8DAAcfe4b19145a85BA3F4101729070a64991", "7DbA97f906E7539D7A3f589C0419cdd9F3d560A5", "7E00281561C8dBfcCcc2D38da68D8Cfcc893247a", "7E0E19472Bf59A00070581E4F96265b1Fa46fa32", "7E3C3693409e84d84E0e9cb6806e4178eFe0a052", "7Ed2787B3a056A4D38404a843E59c9B1bFbB9E2f", "7ED2A207c510D7c2853Ed360b0f946d8f7732aB0", "7f10C26B41E38c9FCA93133988B9Adfb60fa8b61", "7F329f87a04F00f44b579A826F3EF3A309d14faA", "7f6C37081F3272A87770962F2E6e5D6Dc651e752", "7f75c622c682EAf1B566151E703aaA4b118Cb30d", "7fcb3B104dcF7fCe6E7a65c5E4A9D207C61d28B2", "7FE3DA4B17485A6B9aa1CFAeaF8C08b1dCa31656", "8040220c7E6Bd014429f137E019660Ca64F1a58C", "804f4c0bFAaf6580446f199FCC198Dee86fD4f92", "806A1FC60604d442Cb2F63880986EDa3536F073E", "8110F960a9B609F90A627aF63024Ea9da38e5183", "8129A10a23d468377c2e0CE553B4914C161c158B", "814E752aAA7513B93f3794D0f311A1DDa7426697", "818e1a93d8f6AEab148da941adb10c4B4EBDf357", "823803Bf05c582e8715114F9a7f41B645144DdA1", "82B4E7ec28759d4313de56F5B991c95fc4CD8f03", "82BD2Db6929895Ad3599421b4D3478bF913bd89a", "83553EDabf329868a73A5A11B415c411731DbFD8", "836542D5F75edAE96373e8cA2563089c560C8951", "83B48BE67cBFFBB2918585C7Ee6522E645F93aC7", "840963BF423F439917367a303dE787180f3D1DC6", "844898C54d381639c6640c3069E873Afc0BBD5f1", "844E6C5C659e4c4D25cD49052F0d5147C2cCc632", "8460b0ebDcCd0647a6c27A5Fd7850DFB8274525E", "84a6D7bC98f0Bb07F36af171172303486bA95f7c", "85b193B5124176c5427398fB5f992bB3de9c43C0", "85c78857C8580d68889e1eFe30AD986502CCec80", "8624b0C2161FE9959855091D65976c7A7666d4D5", "863507e53Cd65A4ab5d8E048E88E6A8bEA8F4616", "86935eE345ceAE3E12055a6A00776A36ee6b6C82", "86a0547f55AD28b7f24492f39D29De52485a1d24", "8766d2BDFF3fcF459AB2Eefe4eC63BE1BE797CDe", "87A308ad92aAc09390f88CCAfcD36942ff221C7d", "87d1B0982E946e958089f733ab670d995FdDffE9", "87d380eB672f15721930FAF9D1508BCa2317A406", "881412fc3e28Eff3E07FeB96f7F357180e0B8D1b", "8828D2917C37F684616f02365C247d095c8aef78", "883dC19cec1ADd1C7fbab1290C8384bb9cb90d83", "88b27BAEd25a738E75617473aF2b2b5cD362e47B", "893725fA64afFa5db9E4e41e65EF18a3aEdd89b9", "894d673bcD5594b0AdA5CFbf03aC3b454e348fdD", "8970b9252AeBc0d96Edb8851C47472ab020dCA22", "8AdFd5E1F789CFE3a2322E71d109Ef43D70c124B", "8Af4A93ff4cC104380F984e87C4F6fB186D0CeF8", "8B30A122D23D0c8AB5215880Dc0C2Fb0a4F4b8d3", "8b72544019e3C3591E458B6ab18CAB00F109a238", "8Bf91e5eF37Ee14Bc13F52143AF847Aa82399dc9", "8C1e779f304ef4903A7915a6a95109086aDd31c4", "8CC8a70F1256a8fB210494adcE85Bd33b3A53D40", "8d29bE36604FA2968C54717648fcC74575bAAdA1", "8D45A60b37D09bEEdA9c1C86b7e4d714Ae625254", "8d954056E9a49C8587be5A7f12c8fAA6944eb664", "8E7B7D3F78697077a48f3d3E459998701bfF8152", "8eB26bd2fC3cF00581176DF5D4730936a800c586", "8EDB916c36Cc2012D7c147032bE070C95Bddd706", "8eDD94b510F9F9f876657631338797d1624F3d8D", "8f09189FeE5C73D397EDFe3bC408067a42B0FCad", "8F7d9236165222Ea3C38c94c2d41516BF3B72Bba", "8Fa1BE41598d40E5B6685d96F54a9dD0C898461A", "90fccce4CC4eF238e03704AbeC275F44c357b8C2", "91392F800989E76AFe30dBF4422B6dcA9F6cfE25", "918BE543FA7C08fFcaebA2304a6009Fa0E06Fc32", "91aF371E0a93ffc624707DaE190D937dF78abA8C", "9201c813732A688d8F4558A2C8d0962016f8345A", "9215f4E541Aab1DdC53d7A0BFa24F8Dec18Bf70d", "921FB579F2940F84aea870a8068F70a93A6a8B4B", "9240447cEfB1352a589CE09435E333F42A80e919", "925D7DeA9eC52D8BA9a3E63bb8f53Fa7D196E00c", "92b3c84f4C452DF490419A84Db899062FD4ABF19", "92C3fe8753d15AD8318F816F039027A06Eb5c193", "93888157C05Bd91673fc69E4DC33179ec8F12945", "939731b907cf30e34360670859D5440ab1A9503B", "94112e856c927A67f184CbC75fd56cdF70EA35d5", "94313B7002B03D56909424F712958bDB1E7E477b", "945999e288Dae9F7642d012Bbd2AB8d45d0B0E12", "947A2Ad666476D86E8AfD962468bA91E0bbC6087", "947E6c2c6661A318f3c3Bec1B4ef3fD2C27cD736", "9520cdeCc24689Af98b1bd66A608f644841462a3", "956c6d6871C795A4757305dd3BF7BFFDe444289f", "95976b269e8eC56A17ec35Ad4eE61A844bf3a88c", "95a1Fb2195d48bdb82A5456175cE77D5538DD0Da", "96b6309Ed174Cb0DA8f660AA49eBFD50d628c831", "96fBfD1418fAc65297a9002f43dF923b8af56489", "975f3C3f62fA43daD51891F961fe360d191b1442", "977609EDc0dFc4dB692a28AAF43391ABc1B159b9", "979b88198Ff605eEE4f6c021eb60b4d0034dDB1B", "97e2a95f2A3AdEfCC0D6d7bC6CAC2fb21f87fA93", "97E80015D5e93631e6329255e485f3e290aF3010", "980AC204df2283492C20A36960CD84EA95C7f9ed", "9820be971a98676AADe839755b817613757941fE", "982a8F5c10d6b7594793550B6a79d9DDA0490eCF", "983F864bF263dF993C2494eeE47ff392803C6A9c", "9848C9B05127D531B896d86e208Eea22BBC4d328", "98BE1A079Abe1fd612A9541583A4927B80e0b0E8", "98EC353Ce6E78364F50415BD2b817b6c1BDB3525", "9988350a4fEd915049628958FE2F3EFC6463802c", "998A8Db628Cd42a52aC216A627c24C9C4A8c5e1F", "9990E7a6807AB3F0f05357d5e5A370A52F204C97", "99FFF8b3449fFb80F488cB8e6FB7b96De064262a", "9A2d019B5007a446D3e8fC9fCc2e6FD8f88F6B37", "9A4034E922288D9CF5B64b50c9D6cfe9671c7AC1", "9aD18623F032C51d31225D456c51829829c5FB1A", "9Adc7B39dA711b94fBa6638bE4C3CCF69Ce30E50", "9B5b714fa4317B25b3787b4c7EEd50C37340CE7c", "9C0F9D02885609e9F94Fb5a4e9C37256aE46F685", "9C575fF92EcCfd886e429f11a34E2a43820bB836", "9C6fA3033304084b24A01303f8dc05EabecAeFeA", "9c850D5b30A6D2917eDeBe6157eC3b0132FC6B6A", "9CCb84e920fd294016EEcDa3AD8352540037A032", "9ccFD59d343F63Eabe22c2C28b883D298A5a1415", "9CF017e8513360977cE0DEeA5Ca29f46b95b0d35", "9cF588a473bEE8949a902df6264426Ffb747D33c", "9D801797Cf730e99699ff25b080de4DAE582F8bB", "9DB834bcD6Be3a6f044ef4008f2B3F48f6A7d98a", "9e16E3767C7264c9282099Bb13C58acDb1A562C8", "9E525E4621C499354e7dceb5b4faF93e62BB882D", "9EBD1b4F9DbB851BccEa0CFF32926d81eDf6De52", "9f04b648eB552334F2a840d9Ee09FccE68328aB9", "9f6b2c62e3f5c1faCEB84ACe40159e8e0aE10333", "9fE3824C56F7Eb04550149025696114bc7547C0D", "A062F2F9116aB4396b6f929AFAAf940b8203C143", "a07F77E0a420825911bfF20bc9CeDD7c0Ef710e9", "a0A9f6307A88e301B702342F08e726A0c37A1876", "a0ab3b5501a437d05b1503d59552E310e978c4aD", "a1393ee6b2226DB3165367CbFc21bb02326be437", "a1b047453E756efCaCCF7E245087517E029b3Ec5", "A1f6aa0d4eba77B1f37D447dcc8b96020d8b7429", "A36E233d38E40d5dE34F78De01aE3E7c47bE6C8b", "A3A37074AE2259849a87a267Aa9b3064807D15F8", "A402f54c5025da6AF00e1E6CFb917408d5A8e765", "A417048f80c27e9E7a5051eeAadF0B5a77448350", "A41d26011cA1AEa631Bc25B8bf8B049066dcb8E0", "a44383c1D4f12920f0eb8c77cca24886eA666dd5", "A486039FC70b3a1c12B1Bd2CA0B5fC78aa97f35A", "a49d64c31A2594e8Fb452238C9a03beFD1119963", "A4b6924F3977C6c96F00D7035b15E3818991e4b6", "A55555f90626Ecba61DC05BE34A634A496B10562", "a581AD5fe688f53b6bBf2DbCA6AD8cA7D493b7F6", "A64872Ac9E4AC62e4C6098A3ec489baa09BAe547", "a657F99A67C7bF9B6c9C334c269814E178cF5C7c", "A69d8e01A1FCe0428C2FEbC7E683edBB9Dc5dBCc", "a6F446c064c0A6144E6ef04295f889Dc6825B3B1", "A756A4aC7e94403a02E51C2F335416126Cd574ac", "a78420Aeb39ef6e769ef8A5449baea8e129eEE36", "a78637dCeB231D44C47e2240BAb103AEc29A3e1D", "a84D1b09d58394A1C2BfE5Aaf4987eDA9E28Fc13", "a8E6D4f874a3872b008A6d0dF957b8527A32adfC", "A90B930F8E95230B9D0a30EB1F646a5D497f884b", "A968e9C3bb5952dCfB0D102F9ac3E8d04EeF82a1", "A992aFdaD2832975b51f79Bae9516a670B973f2a", "A9A44bFac4ED9e03F926707eEb24fbDC6Db9Bd93", "A9C63a0723a2FB616A60E5f9A434d7cFA1a243DA", "a9f2B7AD1a707B33CA254215d2133A55681a42c9", "AA56B362da3B1646F5990D46d4eEfa42C53e72Be", "aA6b08F0C34Fc8C2E5941E23416D2D48e14F5834", "AA976196bb25dD99356Efa09FcDbE645c040e2A7", "aA9882A744F1263a392Fa702fb26a9220F372d68", "AAa5D234c4D82Bbcf2415617108beE5Bf4C0f77c", "aB2f4b1898f5b40d28E374886755135B56d6e2E5", "aBE18626252b9AAeeaB99258A3Dc41B77aBA8b53", "AC08Ec12DBE52D96c9c18D73E7f9AF3eb0956f97", "AC7c3Fc1EA6e21Bf8dd3AAb5649f84bF518b112E", "AC901d962b94F71Af4d2950C9851C2188da94Cc7", "AcAc48c88Ed4444ea96b9AEF57234B9FB1f57D54", "AD1b4A3d126e2dFfF62c22a2ed83c497Ed0F619B", "AD4f4C11b867B0Ed558D47E126043293533b6d7e", "aD7D6771a8F46F22A72cCCE5aD999cf6D358B2b6", "ae2Cbcea00F37fd78350CDca6e521a12A4CAEbBb", "af49278A3Acd46131Cd1f0De50032164F0db855b", "AF5Cdb065dd2d5BC6C6011e67353eB1AabA837D8", "AF86cd27292B395255CB6A78a8e457d370C85CE4", "Afc1584B2Bb2a457b4b4FFC414BDBD8542F89ade", "aFF2c9622054D5eC822F4d1c4C0b72AE1e69e991", "B013EAFC17d82338084b21982e58E60aCA5AA47f", "B02418D5Eb77A13d58F83E984142191BE660237b", "B03b5f703a007e5f5907395a56d672C7Ea5bc477", "b06F0D44c0D5a82a76184Ae790642d954CfA7715", "B091dA7cb65da2313cBBAA9FF2Fc17c5719C90Ec", "b16ECD9e085d4d9D79539f97D15DAE8e5E38Da30", "b1e22F4d762e7EB085Ce60Ca36C93E2127087D26", "B210d899804c4b0F857e7c95FE6a9971c8A86f92", "b24ffEf5c5605631FdF7019B008111088F5ce5E0", "b286E1FD5b125DA61Aab8075B10cFE658323Bd28", "b2B6e513d581e812bCA40FDA24319140b13fD266", "b2cf54FD389A223BB129EE3342b3b4D3f94d7E25", "b307935EFE8C62994fED62ba3553f347d3a69481", "b316B36Ab83013edD7F4fcc88B8d3dD56d7C1408", "B345e517550B1386CfE7C17bb185b8532bbE7Fb8", "b3569A79cd4498a8EB6f4D0Cbd3057820156F670", "b3b206657CA0e74D9D2088a647b58F856eF1EF56", "b3CD738a415B9E05068811df21D5A640E406bA86", "B3f87920f022c2F5f4c3D3e420667d17A0687862", "b412304966402C541140bFb8BC3E78f5cDeE02D8", "b41874ab17b14131954EC3d89640E9Fa8ab88f14", "b4450798024D0fd9ffE3570b06BEAa161d0b138a", "B4e3E8e33e623b1845E7B0e11407e297a0D7E5f3", "b4ECe7C7ADe4CeC6a5D76522f867b9628a660A64", "B4ED76b1bc16FE973503A313aAD755d9D7A03999", "B51B6Fb64D4438dE60904441b88A5456b89cFcF8", "b56ECfB29ec01172Dc4c5A53875D9c6D8E0f606B", "b5713075BAbAA5de6599bFe6e325350Db460B772", "B5e46372A5F069Edf2C274C5491d91F94d4B5dF3", "b5Ef23b246c944f3C7308597e3C9426C8Adc7cd4", "b62ef7fDdd5C4d50D650A1d7239EA4cdAe09c7FD", "B75acB980A27Ce0e096e7aC44e9F55bb45eB1E9e", "B75e727f890b42FBE202AD33b836ec5Ea2eeb58d", "b7A9702E32114d6E44c62261BdBfD215E5b3cf87", "b804085d97750d9500c34bFda46E6cc3DA7cE0E2", "b83fcA0c989fA7b7af713CEBd3A8689f809847eC", "b96A947E44b4675f83A4028c145cE3cF6f472929", "B97F2059e0878AB006A3d4C5D9dC5b3c41971392", "b9Aaa7aC95C5114f4B0E3909356A7581B93Fe964", "b9d81365189BEB3Ec86660A3bc787eDb5818776f", "b9F421b4ae0Fe246f2eF2cd9E8785BCdbF4B680c", "BA78Fc2BC1FB6AAef94f8705B0269fBC5531E205", "baE6EF402db7932bFD16037906EcC6d4dE337340", "bB4e59279a526bb1E6B295b0C3fFeCF9BCb02b4D", "BbbB402f844850f5DbCAF9E1dfD33BCdd0eF1e97", "BBF6f263035D866449Bb57AB41db3749F317F78C", "BcEcf25C23a174dA684149a7FDc66D4a4416fcAd", "bda9AF57a4cD5e6Ce737a493Ecb1C5BE358FbBA9", "bf16E2CAD842dD9a3EC824A5F04836Bf263CF78C", "Bf1C3bf91D9AA25d91D1FDF33e8ee6c2CdaDc403", "BF346885B50f7266a4c0B34D45993b01A5C56C2D", "BFAF7e6F5dd56eE5828E769c5a3d02cd4Df5723a", "C036Ab7f585C2ecE643659c8784fEaf55Ee18Ae4", "c046724401405AA8ef8321588b5818E4144A9638", "c074Dfa814cFDa418CD196C63C1557F61B1b21a8", "C0bA278CB8379683E66C28928fa0Aa8bfF3D95E6", "C14CBcAcC18b8ce707D9aF5fB489216e551aF0E6", "C1C2B313273612Af1fFd86D70976425cBFB5eFc0", "c22196E00acb2D18cd2060bE33f801255AAa0bfb", "c2abC5F77172C9e230c0c7C73e433f08F9952535", "c2CC5b875b3b02d5E3311f7B645df15847dc9242", "c3496699439cDB83923659079cf5dc9462639b53", "C36f031aA721f52532BA665Ba9F020e45437D98D", "c3a6111D4Aefb723Fd83fE03c2AEAC7A43547ca7", "C3C7fD0579a1dcf6533E9e65F6ADF166F1C4820C", "C404fd5f50F49B34C597D09CAd302f3c501aa284", "C49BACd770b8656920008111b5eAa3029fbC42f9", "C4e74ed5554f3Bfd695BEd65612e639975BFCbBA", "C55585b7D03906F27dB6342f801fDA1D668e8C62", "c5850a5116F58a11f9AC8702fD9F23ff878a6b82", "c60d67C255F2206a6c040a178d78894Cbcd4542E", "c62e7756b25Cb71d09E6E3955c9318e8fC16135f", "C649F857aD471D9A664d08AbE7D831C284FA85Bd", "C6A623e1De801EE9D84d405E41F984Cff60f1eab", "c7587dE0de311Bb2B9438E36d2f4B78b6EdF3dDd", "C793de138b438a5edc7Bb4fFAb9f8Bf250bD306a", "C7FffFC194f81d2c5C5684C2cD7C731A1DDe133B", "c83BD63cb6E3b1c4A7613BD3a977B07FC71dB01D", "C863A8777c31e14daF1eAfe417aabf6bd84A0c79", "c8Dc307FE696b71f30429fF3a602CB2420965F16", "c8eb183Bc8Df5FA26C0Ec0E40a5FdcCCb5383222", "c9B06fb786f9cCA8FAcC47Ed34890729495A61B1", "CA7a233B5D8D46b0aa96EAD6F79d3D4e140a37C0", "cAF8f1CB3A748649A370161Cb3f7a3e2B37f6b93", "cB2126576D9606D05ebF5fE5dCa2F0b70Daec8E5", "Cb35A995231A5E9a0ea44986598f6E05092AE70f", "CCC75d426C882325c8ee1cCcf1CF51153a7cC3F5", "cD1D8ef767fb774c62Cd2cbda07Fe09935557341", "CD94CC315D44E2d55Fe584534D843b03f029dCe4", "CdFa2E1D71265654954cD3bfFA5DB15F489DEC6a", "CE47D3dA6F9Bf051B755AC8589362C86d89F4Cc2", "cE5B2F4aD60568079922Eb3496c7D1BbDdac912b", "ce8458338e21C29F79d4A7C5E82882b4dC1174f5", "cF00069801D787c4a7C85ce4102e5Dce32797070", "CfaA743C6d3e957Dc7902057cD4b8217aA1BEBfc", "Cfb1e333a2788e7D19E97fC53DC01ad170D9CB39", "cfF6fD90eAF6469eba315652B3198CA36E4c9218", "d0337188C063E46D3a20bBc7C9c6d486Bf9ee42F", "D06eF17133817DbCc4ac0AaB7Be699fb5c7Bb3CB", "D0870F3d7B7657dee391Ab84B816840Ca5d940A9", "d1C90CB02919f39b64E71B533312D58417771C43", "D20d2203f36a222F1DE1F6715613829db7E3f4c6", "d2116d0137315A9B6F2b9DA6eea3c2BA06bbF5c6", "D2294958b5654C70f5e0Cec91F52C38de75A9492", "d24E69d42F6e30f24214F4bd925B29d841709761", "D25E498AdC7f0f9dDC88CcE9cA1473ffff968BBc", "D2B8Ea24427E2d7B1BAC082433482436F2212B7E", "d34467c4530E2b57A9a9F7e07459a7c5Ca889280", "D3839E3558438ce6513F0b532B58bb347e5CAC77", "D3C96C6faFDdd97C70dbb459A823973f8F98a272", "d3D1EB3eEb53f31D54f80614E9bA25b918f92c9d", "D3DC36514e7C2B1175e38d7C0dAA644D19F349E7", "d3e4f748e701C4949f084614C30388f658F309f4", "d457b6C843C6A63cF223805aac76E81833e26739", "D478d3A6cFe30C32F501e80D1d228C5d2881c4Cf", "D4BBF1621aE01065040C29cAEF53b9bF2F6a21f3", "d4D5A05Cf02392d51f50Db9412d4401A23898001", "D50F858F01f5Fdc859a39b826Beb55C47e110612", "d65c29a8fE89Bf8C4d2E3845e1001DDC95440F2D", "D6811a84CCf6911a445838026cC86E343589014a", "d68613fdF7a2bf5921E6A2be5519d9C11e4E0d83", "d6867EC9256E04452DD7Ec1E7431c72d3FF02a26", "D6E11BC848f7dB23fA136d510e18DF8C02184836", "D6e93A8bD54f74c6543aad2f0E98d49E07718d07", "D6Fe64cEA2FEd9cf9317c514a4675a4DDb2e5Aa4", "d7033FAE1f4Aab10f615ac3caA1E18011DD71c4f", "d736718F24DDE5dfe5a407767644eB0599cCf533", "D75bc43397DB94542452e24CaFF92592543D546C", "D80C93CB03694f85A01A58D4e4D474cBe9Dc8658", "d813D4d75e59Ab1465B90688DeBf86ec9668ae19", "d8B0779d4883D56aBF396D205D5C47e914F62422", "d93E27237CEa4d9113C294301B143705cEC62c06", "db34da60db486bEecDE41A5f8472f565C13Fec9D", "db6c7e5083e1F617bc4EBCf85f958Cd417400AFa", "db748D7C6afBf711aC3528B42c625A29a0b3EdED", "dbbC5e5f81d5a9eE06062cfC7F3cAe0ACa14bC58", "DBdD36EF88d4D9082e8B0eFf4c0e6E7d6783caAA", "DcB9EB5A77b761e46DbE8d73C375782b082fB4c2", "DD84007052cDdBc6435BD7cf52e154d5815eF9Db", "dde23c49C0e36B5f8206Dbdac60675288484B37E", "dE8C2ADa5405f043f1aF4539e9A9bc5405779Aa7", "dF29FACED5a748a204e5F595c689638FC95E0C04", "dFa43357279200D1ff7C0a6c460eD4b8E5a4cC1E", "e0548D151238936c4C9e563A4DE239709f655D11", "E059505731954D823F8619aFf800113aC5F24304", "e06aa36AD36A9cb5e6A3f99AcD0521609B7e29c0", "E0EfCa790Dd63f2033CeA271a52046EA1a8002c1", "e0f395fA45b96042b87072012fD68c73eE8Faded", "e106B9D06cf7dc10c40F3C4B9e52E178606b721E", "e1cdB862710b553756fa5EeF89E382ce1F7de41c", "e2534D80C7bFe8F79FF4B63ff8C3c7419924669e", "E26d41522Ff7375Ab8a6DA1285950F7C8f52AeCd", "E28349619943d9af1f7b44592C0a8dfeeE34e963", "e2E3191b94D5C89FB0603B5A2CEc27352B066E34", "E2F1bE869b1fB109CFe9066BadC0E4b6c16b245B", "e4F30eb3e7572e0eF86Ccc6C99888e1329188977", "e554D8F5BD9b68511ee837DEE1ef3C9d6be287D8", "E579eEf2E1c46634861dA496e7A98755e00f8f48", "E57eeFA60c8540556BecB2360D38A37df7f375f0", "e63871FA0b56614b9054787656fbc7A935d56EEF", "e668406297B9D207110E6aE3713Bc23CC1D1B250", "E6AB7fA67faE9cEb29BE637DF21640F35CD19419", "e6b678bA7EDc9deA84F1Ee66F85A4871c84aEbea", "E6C86774d991879264cda8031DFc46A6D0768836", "E6e0CF635A7EdE3243382AA612a8C91E6a5F64FC", "e6f030E3E9Df6628A8FE3707AE4DFD41664999cf", "e7d3aAa5238525Faa3a1A69aed29df684a8aeC3f", "E8F54C9afC37845505CD4B6F8B68c19FBFdeF307", "E9017A789318571b8C32f3c8cf425dc24Ac87C2a", "E93b593eA5F24262D865C728Ce26F4eF86F28347", "E97b955045589f95c14249c26954395DD48C92f9", "e98FD690865E4bd3231Af97A4C01dFFb6F5b7A0B", "E9D837fbC79d0b5BB814C583f7698012413A2Db4", "Ea455Eeb4c251BD1b4d14ef32cf205a8F11A11AC", "EAd2349a0C0C3fcb75BB51784Ed985180ECD35d9", "EAe7d6bD5cDBeb8958302fC25f8D7B0cEd4E5a3E", "eB5E57e36506097202b5c833B75D2afc3c141978", "eb87379bB15D0f0d60CE1F1de2554534CEaE033f", "ebA7EE991668F83E58457bC74fBB1a2B646Bea15", "eC19b9B29603e2c295776c3C7476CFb5AA74F43E", "Ec622Ab4B75aF2CBa829E62494d8166D6F0758a8", "ec73473A7B6359825d94cC19557C319d7Cec5fb3", "Ec7A17218D777B24cb0b1D384047F3Dc139BE28A", "eCb1ba32dDdF465A318943631912553e7730A659", "ECd06259bB4a39690c91FdfB70767f7862c3f387", "ED2F400aECec4D5a98f9Dc9056935fd844cBA17E", "Ed62e93A10d6A1a7cb368653a2CD0D1E08a4796B", "edE11fc26131De79f36FC875f5D36b97c0728483", "EE9cA24FB62BFc021e1A46E09e1C1CbECD3341B5", "EEc50a90C8Db0a736775B0cB8394Bb710D924f83", "eed48D9985abA770540Feee917A301FAaFEAfaF2", "EEE0F468D0214c389693b119Cec2094Af727574f", "Ef6dd0C792A7fbEb1c564a30E13f988bE05C477A", "efdD0C13007Fc72Ed86b5Af37B1cB43BD8F5c79d", "F037F0db578167C65a7C2eC705E4b5C821E34D39", "F098e95c75795592E045fd585fD0bf68D31C538D", "f15c128e78db166BD3EC7c8f21f62cDa45507c63", "F1ac24C415b4aB7BcA0a7D7D983B62326ad83556", "F1d027De03cC39394D52D36068d86F0a9F5675cd", "f243AcF31D3003b7b5D5177E2Bf5275a0BE85534", "F2bAEDC6B4362cB1Cdd63Df97dA7Fc4C40166E75", "f392d97E4D1757070Fd5b4dB9cdB9bD024F2c00e", "F4116C8e2e98DED6aC1FF21577ceBB94045B99ab", "f4118E0CBb0a469b99e9e94DB1Cca05e3fc0F183", "f42b145D6e278648f86C98a00c9802378cDD6f78", "F465f55ca2a5C2C67e2743EF0B5024C17537Ba78", "F4D51201F29C9fa774d6fBca9EbE1Ec27263ae4b", "F53573E53eE3be690182bDCD4B2Eb89D7f7f74C2", "f5511dD699e98D3FF7E6dF8899E7C1Ce75A258D4", "f5579482F88DcE084Baabf92c22AcC5b4D00a18d", "f599d4bD138B5331Dd7314EAE12941Fa57358635", "F7261Aba7Ac7Acb210523BD96463F3b8771D3C05", "f7B5607F6bAf30EAc2621ce9dbcB32100A30f0F0", "F7c3a34bbFe5F46bc0Aa4202F0dAb51890d93f2d", "f7DEA2A5F5691a1038a18b1C12D5687997c8dd7D", "f8185569dd605152582a06226470809c917163f1", "F832543daaA40972Ffa52bb683F4b1464bB08d98", "f84826434975D6AB8fE9B2043fB91633f1d210F8", "f865E7d9974D77101a51307879264575d4A6FC08", "F8c1e09194Fcf575e41d80F81225cbfff04218e5", "f8eD36E709A7B177B18d24E468F74573Fb681F05", "f90993dF79ee53A509BFA72648c54a463842268E", "f972D8998A1c3ced596BA90274148Bf31dce7959", "F97b2CB06b3e572642d7790DD40A5c121465504a", "facf2174af963d91cF82e8E64Eb03378C12403A3", "FadcB1209C46b6eB7aaCA83205eD3CD3075D6245", "FAe1ac37183a687A4EE79Ba6d688A5B0a3e1BBf8", "Fb73799000dc6775a5e3E57868d08959202795b8", "FbCBD3d8C4F1a577A5F39017740f153e8457CFdb", "FbF0455Bf1778cE859E3bf0d988FaF2bBE3Cc7B2", "fc7CaF6b020Dab6e22A7B80e51012Ad2B8eC1A73", "Fcada9a672d631f89b382d0Ec74b2fA4bDbAF68D", "fDC73C699c946121D7354D5fd8BbF26D1134667f", "fe1AaDE40b40f3a4ac76A63f79e8D6f6c4C48AeA", "FE256CD2F95a89229A82BE7Cb11061abA2450593", "fe80409A1509750FFbdf7f7e15cB2F05e7C12513", "FEc3460A71417Eefa8E78a5927305be105Fe67ab", "FeDDc18f892c4ecDCA9C7477FEA4b9090C9cD47E", "fEeE8268683B8a082b256a9598d9A48BEd98e35E", "ff295256CeCc799ca9f974033C11d08F797815d1", "fF99c5478B7231a8440997418bD803Fb951071bF", "0D95130B78570601597435F0F389d0FCcd6b067B", "19f9c5C223EaECCB436927824e83aFe37b2eCDb1", "1E3972F19a53C1D326E9F689A2984B750596c5f8", "21B5EFC782e5521A1BD14dA51E66C50A66b64Ff4", "2992FEebD894d79A3ad4F6220790880Ec9F6A1C3", "30Fc21542410c38b7e2D6C45328599Dfef6cce51", "317F91928487686A87bCF2842F783128bE53ec6B", "41bA751932c7C54c0445962ad2F25aF6e0c6B2e6", "436F65632Fb51156AD3d7a57c9b87E8860b605Bb", "48e68C0F7dCffa21799Ea075769797D4A222e029", "5Cca7979be74514E69bfD754315a923Ca9575249", "5E7aCd60c54be693feE89467eEd90F62089B670E", "667df6aD060aE570DcE51d9bf017b4aa02fdFc5F", "67A0a9F50bc9f40647cB7A41A5d5d78A79Caf39e", "6D353fEbA28E8cff7272D7EF3A7B37024AF40F19", "7313f3B8a8Ea885FA6D3831336DF96b47351fBC8", "7d24eaC633fd9A408359b683a3c89Acf9C7764E5", "8156F0B1D7957673847160F49BE06Ea437381eaC", "81572B8483EA297C68cc6B5C0290938cF2D5D609", "81d3855f126451E99B0580672f05732a802918eA", "857307211EB0A07CC37ADCe39493C492e44D231f", "94eB034836C15941459248E0287dEA8a3A4b8bdc", "9c8AE786EF4d9421f5019a1554561F27c7902944", "9D3B9F6939c980ad52b6046425f6973BA7dEd997", "a33FDC20ba7bD84174b8D0E5Ced2861A4201900d", "a7C6bf792b19945665F20244ee657f27F7f5A7f3", "Ad2a3cF68Bd99bAEC1084C991B1cb01b470Fa2b6", "b28653C92530C6B672502F539D302e4Be72c6597", "b60434365550419b9f69dCD5D53351c1C581aB13", "b8c36821fB161276544f8814bC38d7011b19697a", "B8d5a90767355a0453707d1d0723308128288536", "bb311506397DCA5F75C33F5Ace1850066e0e088C", "bF111FC370FCC04C1D24d81d47E089B67dDD60ec", "C6a3eE1e0f912120931e2668CD2Eb655B763f3F7", "D0b2FE496B117fe64528691Fa7055Bf7Daf38106", "D114dbc137aD756a3b8E1Dd9d606Fb701c6C1E51", "D9fCe9E571ed4bdB34D3CF46A918Eb6938571a5B", "da6746D2AAa5708387ccd7Fad04AF9bD3FD0f257", "DcbD2dc177ae46Aa2dc522ECFeDd15c64A369881", "dE332A44aD2cAB340e537D9793BC0312f640CA41", "dfC45533dA0289aEA431c571B8D2F5b11B8cB760", "E33A12E3f1C88ab133eE1763F2d338D5a5CaBb67", "e6Ed2AFbd6627ea4B6C190Ce00a3892c381d38F8", "e7d66807bFc73e7335cEA5b3D072C8Ed2D007F60", "E8ADDD163f0d40B0f7DD504f9123D971770e3B74", "f1f2804245fB2F952cFAF3e73b02B1f457cb24eC", "fCe8723c7639Df276FC115cCf1929c0b3e7D0D19" };
                            if (!_migratedRaid)
                            {
                                Log.Debug("GETRAIDAGENTADDRESSES");
                                foreach (var agent in agents)
                                {
                                    Log.Debug("RAIDAGENTADDRESSES: {0}", agent);
                                    var agentAddress = new Address(agent);
                                    var avatarAddresses = ev.OutputStates.GetAgentState(agentAddress).avatarAddresses;
                                    foreach (var avatar in avatarAddresses.Values)
                                    {
                                        avatars.Add(avatar);
                                    }
                                }

                                Log.Debug("RAIDAVATARADDRESSESCOUNT:{0}", avatars.Count);
                                Log.Debug("RAIDAVATARADDRESSES:{0}", avatars);
                                _migratedRaid = true;
                            }

                            foreach (var avatar in avatars)
                            {
                                if (avatar == ev.Action.AvatarAddress)
                                {
                                    int raidId = 0;
                                    bool found = false;
                                    for (int i = 0; i < 99; i++)
                                    {
                                        if (ev.OutputStates.UpdatedAddresses.Contains(
                                                Addresses.GetRaiderAddress(avatar, i)))
                                        {
                                            raidId = i;
                                            found = true;
                                            break;
                                        }
                                    }

                                    if (found)
                                    {
                                        RaiderState raiderState =
                                            ev.OutputStates.GetRaiderState(avatar, raidId);
                                        var model = new RaiderModel(
                                            raidId,
                                            raiderState.AvatarName,
                                            raiderState.HighScore,
                                            raiderState.TotalScore,
                                            raiderState.Cp,
                                            raiderState.IconId,
                                            raiderState.Level,
                                            raiderState.AvatarAddress.ToHex(),
                                            raiderState.PurchaseCount);
                                        _raiderList.Add(model);
                                        MySqlStore.StoreRaider(model);
                                    }
                                    else
                                    {
                                        Log.Error("can't find raidId.");
                                    }
                                }
                                else
                                {
                                    int raidId = 3;
                                    RaiderState raiderState = ev.OutputStates.GetRaiderState(avatar, raidId);

                                    if (raiderState != null)
                                    {
                                        Log.Debug("YESRAIDAVATARADDRESSES:{0}", avatars);
                                        var model = new RaiderModel(
                                            raidId,
                                            raiderState.AvatarName,
                                            raiderState.HighScore,
                                            raiderState.TotalScore,
                                            raiderState.Cp,
                                            raiderState.IconId,
                                            raiderState.Level,
                                            raiderState.AvatarAddress.ToHex(),
                                            raiderState.PurchaseCount);
                                        _raiderList.Add(model);
                                        MySqlStore.StoreRaider(model);
                                    }
                                    else
                                    {
                                        Log.Error("can't find raidId.");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
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
                        foreach (var avatarAddress in avatarAddresses)
                        {
                            try
                            {
                                AvatarState avatarState;
                                try
                                {
                                    avatarState = ev.OutputStates.GetAvatarStateV2(avatarAddress.Value);
                                }
                                catch (Exception)
                                {
                                    avatarState = ev.OutputStates.GetAvatarState(avatarAddress.Value);
                                }

                                if (avatarState == null)
                                {
                                    continue;
                                }

                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
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

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
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
                                    Address = avatarAddress.Value.ToString(),
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
