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
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Blocks;
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
        private const int DefaultInsertInterval = 1;
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
        private readonly List<RapidCombinationModel> _rapidCombinationList = new List<RapidCombinationModel>();
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
                    StoreRenderedData(b);
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

#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(RuneHelper.StakeRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ev.PreviousStates.GetBalance(
                                    avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ev.OutputStates.GetBalance(
                                    avatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                _runesAcquiredList.Add(new RunesAcquiredModel()
                                {
                                        Id = id.ToString(),
                                        ActionType = claimStakeReward.ToString()!.Split('.').LastOrDefault()?.Replace(">", string.Empty),
                                        TickerType = RuneHelper.StakeRune.Ticker,
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = avatarAddress.ToString(),
                                        AcquiredRune = Convert.ToDecimal(acquiredRune.GetQuantityString()),
                                        Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                                        TimeStamp = _blockTimeOffset,
                                });
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
                        }
                        catch (Exception ex)
                        {
                            Log.Error("RenderSubscriber: {message}", ex.Message);
                        }
                    });

            _actionRenderer.EveryRender<EventDungeonBattle>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } eventDungeonBattle)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<EventConsumableItemCrafts>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } eventConsumableItemCrafts)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<HackAndSlash>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } has)
                        {
                            var start = DateTimeOffset.UtcNow;
                            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(has.AvatarAddress);
                            bool isClear = avatarState.stageMap.ContainsKey(has.StageId);
                            var itemSlotStateAddress = ItemSlotState.DeriveAddress(has.AvatarAddress, BattleType.Adventure);
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
                                    typeof(RuneListSheet),
                                    typeof(RuneOptionSheet),
                                });
                            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
                            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
                            var runeStates = new List<RuneState>();
                            foreach (var address in has.RuneInfos.Select(info => RuneState.DeriveAddress(has.AvatarAddress, info.RuneId)))
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
                                Address = has.AvatarAddress.ToString(),
                                AgentAddress = ev.Signer.ToString(),
                                Name = avatarName,
                                AvatarLevel = avatarLevel,
                                TitleId = avatarTitleId,
                                ArmorId = avatarArmorId,
                                Cp = avatarCp,
                                Timestamp = _blockTimeOffset,
                            });
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<HackAndSlashSweep>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } hasSweep)
                        {
                            var start = DateTimeOffset.UtcNow;
                            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasSweep.avatarAddress);
                            bool isClear = avatarState.stageMap.ContainsKey(hasSweep.stageId);
                            var itemSlotStateAddress = ItemSlotState.DeriveAddress(hasSweep.avatarAddress, BattleType.Adventure);
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
                                    typeof(RuneListSheet),
                                    typeof(RuneOptionSheet),
                                });
                            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
                            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
                            var runeStates = new List<RuneState>();
                            foreach (var address in hasSweep.runeInfos.Select(info => RuneState.DeriveAddress(hasSweep.avatarAddress, info.RuneId)))
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
                                Address = hasSweep.avatarAddress.ToString(),
                                AgentAddress = ev.Signer.ToString(),
                                Name = avatarName,
                                AvatarLevel = avatarLevel,
                                TitleId = avatarTitleId,
                                ArmorId = avatarArmorId,
                                Cp = avatarCp,
                                Timestamp = _blockTimeOffset,
                            });
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<CombinationConsumable>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } combinationConsumable)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<CombinationEquipment>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } combinationEquipment)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<ItemEnhancement>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } itemEnhancement)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<Buy>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } buy)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<Stake>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } stake)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<MigrateMonsterCollection>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } mc)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<Grinding>()
                .Subscribe(ev =>
                {
                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<UnlockEquipmentRecipe>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } unlockEquipmentRecipe)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<UnlockWorld>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } unlockWorld)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<HackAndSlashRandomBuff>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } hasRandomBuff)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<JoinArena>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } joinArena)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<BattleArena>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } battleArena)
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
                                sheetTypes: new[]
                                {
                                    typeof(ArenaSheet),
                                    typeof(ItemRequirementSheet),
                                    typeof(EquipmentItemRecipeSheet),
                                    typeof(EquipmentItemSubRecipeSheetV2),
                                    typeof(EquipmentItemOptionSheet),
                                    typeof(MaterialItemSheet),
                                    typeof(CharacterSheet),
                                    typeof(CostumeStatSheet),
                                    typeof(RuneListSheet),
                                    typeof(RuneOptionSheet),
                                });
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

                            var itemSlotStateAddress = ItemSlotState.DeriveAddress(battleArena.myAvatarAddress, BattleType.Arena);
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
                            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
                            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
                            var runeStates = new List<RuneState>();
                            foreach (var address in battleArena.runeInfos.Select(info => RuneState.DeriveAddress(battleArena.myAvatarAddress, info.RuneId)))
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
                                Address = battleArena.myAvatarAddress.ToString(),
                                AgentAddress = ev.Signer.ToString(),
                                Name = avatarName,
                                AvatarLevel = avatarLevel,
                                TitleId = avatarTitleId,
                                ArmorId = avatarArmorId,
                                Cp = avatarCp,
                                Timestamp = _blockTimeOffset,
                            });
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<BattleGrandFinale>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } battleGrandFinale)
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
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<EventMaterialItemCrafts>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } eventMaterialItemCrafts)
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

            _actionRenderer.EveryRender<RuneEnhancement>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } runeEnhancement)
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
                            Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                            var prevCrystalBalance = previousStates.GetBalance(
                                ev.Signer,
                                crystalCurrency);
                            var outputCrystalBalance = ev.OutputStates.GetBalance(
                                ev.Signer,
                                crystalCurrency);
                            var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                            var runeStateAddress = RuneState.DeriveAddress(runeEnhancement.AvatarAddress, runeEnhancement.RuneId);
                            RuneState runeState;
                            if (ev.OutputStates.TryGetState(runeStateAddress, out List rawState))
                            {
                                runeState = new RuneState(rawState);
                            }
                            else
                            {
                                runeState = new RuneState(runeEnhancement.RuneId);
                            }

                            RuneState previousRuneState;
                            if (ev.PreviousStates.TryGetState(runeStateAddress, out List prevRawState))
                            {
                                previousRuneState = new RuneState(prevRawState);
                            }
                            else
                            {
                                previousRuneState = new RuneState(runeEnhancement.RuneId);
                            }

                            var sheets = ev.OutputStates.GetSheets(
                                sheetTypes: new[]
                                {
                                    typeof(ArenaSheet),
                                    typeof(RuneSheet),
                                    typeof(RuneListSheet),
                                    typeof(RuneCostSheet),
                                });
                            var runeSheet = sheets.GetSheet<RuneSheet>();
                            runeSheet.TryGetValue(runeState.RuneId, out var runeRow);
#pragma warning disable CS0618
                            var runeCurrency = Currency.Legacy(runeRow!.Ticker, 0, minters: null);
#pragma warning restore CS0618
                            var prevRuneBalance = previousStates.GetBalance(
                                runeEnhancement.AvatarAddress,
                                runeCurrency);
                            var outputRuneBalance = ev.OutputStates.GetBalance(
                                runeEnhancement.AvatarAddress,
                                runeCurrency);
                            var burntRune = prevRuneBalance - outputRuneBalance;
                            _runeEnhancementList.Add(new RuneEnhancementModel()
                            {
                                Id = runeEnhancement.Id.ToString(),
                                BlockIndex = ev.BlockIndex,
                                AgentAddress = ev.Signer.ToString(),
                                AvatarAddress = runeEnhancement.AvatarAddress.ToString(),
                                PreviousRuneLevel = previousRuneState.Level,
                                OutputRuneLevel = runeState.Level,
                                RuneId = runeEnhancement.RuneId,
                                TryCount = runeEnhancement.TryCount,
                                BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                                BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                BurntRune = Convert.ToDecimal(burntRune.GetQuantityString()),
                                Date = _blockTimeOffset,
                                TimeStamp = _blockTimeOffset,
                            });
                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored RuneEnhancement action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<TransferAssets>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } transferAssets)
                        {
                            var start = DateTimeOffset.UtcNow;
                            foreach (var recipient in transferAssets.Recipients)
                            {
                                var actionString = ev.BlockIndex +
                                                   recipient.recipient.ToString() +
                                                   recipient.amount.Currency.Ticker +
                                                   recipient.amount.GetQuantityString();
                                var actionByteArray = Encoding.UTF8.GetBytes(actionString);
                                var id = new Guid(actionByteArray);
                                var avatarAddress = recipient.recipient;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(avatarAddress);
                                var agentAddress = avatarState.agentAddress;
                                _runesAcquiredList.Add(new RunesAcquiredModel()
                                {
                                        Id = id.ToString(),
                                        ActionType = transferAssets.ToString()!.Split('.').LastOrDefault()?.Replace(">", string.Empty),
                                        TickerType = recipient.amount.Currency.Ticker,
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = agentAddress.ToString(),
                                        AvatarAddress = avatarAddress.ToString(),
                                        AcquiredRune = Convert.ToDecimal(recipient.amount.GetQuantityString()),
                                        Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                                        TimeStamp = _blockTimeOffset,
                                });
                            }

                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored TransferAssets action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<DailyReward>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } dailyReward)
                        {
                            var start = DateTimeOffset.UtcNow;
#pragma warning disable CS0618
                            var runeCurrency = Currency.Legacy(RuneHelper.DailyRewardRune.Ticker, 0, minters: null);
#pragma warning restore CS0618
                            var prevRuneBalance = ev.PreviousStates.GetBalance(
                                dailyReward.avatarAddress,
                                runeCurrency);
                            var outputRuneBalance = ev.OutputStates.GetBalance(
                                dailyReward.avatarAddress,
                                runeCurrency);
                            var acquiredRune = outputRuneBalance - prevRuneBalance;
                            _runesAcquiredList.Add(new RunesAcquiredModel()
                            {
                                    Id = dailyReward.Id.ToString(),
                                    ActionType = dailyReward.ToString()!.Split('.').LastOrDefault()?.Replace(">", string.Empty),
                                    TickerType = RuneHelper.DailyRewardRune.Ticker,
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = dailyReward.avatarAddress.ToString(),
                                    AcquiredRune = Convert.ToDecimal(acquiredRune.GetQuantityString()),
                                    Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                                    TimeStamp = _blockTimeOffset,
                            });

                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored DailyReward action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<ClaimRaidReward>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } claimRaidReward)
                        {
                            var start = DateTimeOffset.UtcNow;
                            var sheets = ev.OutputStates.GetSheets(
                                sheetTypes: new[]
                                {
                                    typeof(RuneSheet),
                                });
                            var runeSheet = sheets.GetSheet<RuneSheet>();
                            foreach (var runeType in runeSheet.Values)
                            {
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ev.PreviousStates.GetBalance(
                                    claimRaidReward.AvatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ev.OutputStates.GetBalance(
                                    claimRaidReward.AvatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                {
                                    _runesAcquiredList.Add(new RunesAcquiredModel()
                                    {
                                        Id = claimRaidReward.Id.ToString(),
                                        ActionType = claimRaidReward.ToString()!.Split('.').LastOrDefault()?.Replace(">", string.Empty),
                                        TickerType = runeType.Ticker,
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = claimRaidReward.AvatarAddress.ToString(),
                                        AcquiredRune = Convert.ToDecimal(acquiredRune.GetQuantityString()),
                                        Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                                        TimeStamp = _blockTimeOffset,
                                    });
                                }
                            }

                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored ClaimRaidReward action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<UnlockRuneSlot>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } unlockRuneSlot)
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

            _actionRenderer.EveryRender<RapidCombination>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } rapidCombination)
                        {
                            var start = DateTimeOffset.UtcNow;
                            var states = ev.PreviousStates;
                            var slotState = states.GetCombinationSlotState(rapidCombination.avatarAddress, rapidCombination.slotIndex);
                            var diff = slotState.Result.itemUsable.RequiredBlockIndex - ev.BlockIndex;
                            var gameConfigState = states.GetGameConfigState();
                            var count = RapidCombination0.CalculateHourglassCount(gameConfigState, diff);
                            _rapidCombinationList.Add(new RapidCombinationModel()
                            {
                                Id = rapidCombination.Id.ToString(),
                                BlockIndex = ev.BlockIndex,
                                AgentAddress = ev.Signer.ToString(),
                                AvatarAddress = rapidCombination.avatarAddress.ToString(),
                                SlotIndex = rapidCombination.slotIndex,
                                HourglassCount = count,
                                Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                                TimeStamp = _blockTimeOffset,
                            });
                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored RapidCombination action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
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
                                            _blockTimeOffset,
                                            null,
                                            null,
                                            null,
                                            null);
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
                            AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(ev.Action.AvatarAddress);
                            var itemSlotStateAddress = ItemSlotState.DeriveAddress(ev.Action.AvatarAddress, BattleType.Raid);
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
                            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
                            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
                            var runeStates = new List<RuneState>();
                            foreach (var address in ev.Action.RuneInfos.Select(info => RuneState.DeriveAddress(ev.Action.AvatarAddress, info.RuneId)))
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

                            var runeSheet = sheets.GetSheet<RuneSheet>();
                            foreach (var runeType in runeSheet.Values)
                            {
#pragma warning disable CS0618
                                var runeCurrency = Currency.Legacy(runeType.Ticker, 0, minters: null);
#pragma warning restore CS0618
                                var prevRuneBalance = ev.PreviousStates.GetBalance(
                                    ev.Action.AvatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ev.OutputStates.GetBalance(
                                    ev.Action.AvatarAddress,
                                    runeCurrency);
                                var acquiredRune = outputRuneBalance - prevRuneBalance;
                                if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                {
                                    _runesAcquiredList.Add(new RunesAcquiredModel()
                                    {
                                        Id = ev.Action.Id.ToString(),
                                        ActionType = ev.Action.ToString()!.Split('.').LastOrDefault()?.Replace(">", string.Empty),
                                        TickerType = runeType.Ticker,
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = ev.Action.AvatarAddress.ToString(),
                                        AcquiredRune = Convert.ToDecimal(acquiredRune.GetQuantityString()),
                                        Date = DateOnly.FromDateTime(_blockTimeOffset.DateTime),
                                        TimeStamp = _blockTimeOffset,
                                    });
                                }
                            }

                            _avatarList.Add(new AvatarModel()
                            {
                                Address = ev.Action.AvatarAddress.ToString(),
                                AgentAddress = ev.Signer.ToString(),
                                Name = avatarName,
                                AvatarLevel = avatarLevel,
                                TitleId = avatarTitleId,
                                ArmorId = avatarArmorId,
                                Cp = avatarCp,
                                Timestamp = _blockTimeOffset,
                            });

                            int raidId = 0;
                            bool found = false;
                            for (int i = 0; i < 99; i++)
                            {
                                if (ev.OutputStates.UpdatedAddresses.Contains(
                                        Addresses.GetRaiderAddress(ev.Action.AvatarAddress, i)))
                                {
                                    raidId = i;
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                            {
                                RaiderState raiderState =
                                    ev.OutputStates.GetRaiderState(ev.Action.AvatarAddress, raidId);
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

        private void StoreRenderedData((Block<PolymorphicAction<ActionBase>> OldTip, Block<PolymorphicAction<ActionBase>> NewTip) b)
        {
            var start = DateTimeOffset.Now;
            Log.Debug("Storing Data...");
            var tasks = new List<Task>
            {
                Task.Run(() =>
                {
                    MySqlStore.StoreAgentList(_agentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault())
                        .ToList());
                    MySqlStore.StoreAvatarList(_avatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault())
                        .ToList());
                    MySqlStore.StoreHackAndSlashList(_hasList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault())
                        .ToList());
                    MySqlStore.StoreCombinationConsumableList(_ccList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault())
                        .ToList());
                    MySqlStore.StoreCombinationEquipmentList(_ceList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault())
                        .ToList());
                    MySqlStore.StoreItemEnhancementList(_ieList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault())
                        .ToList());
                    MySqlStore.StoreShopHistoryEquipmentList(_buyShopEquipmentsList.GroupBy(i => i.OrderId)
                        .Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreShopHistoryCostumeList(_buyShopCostumesList.GroupBy(i => i.OrderId)
                        .Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreShopHistoryMaterialList(_buyShopMaterialsList.GroupBy(i => i.OrderId)
                        .Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreShopHistoryConsumableList(_buyShopConsumablesList.GroupBy(i => i.OrderId)
                        .Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.ProcessEquipmentList(_eqList.GroupBy(i => i.ItemId).Select(i => i.FirstOrDefault())
                        .ToList());
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
                    MySqlStore.StoreRapidCombinationList(_rapidCombinationList);
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
            _rapidCombinationList.Clear();
            var end = DateTimeOffset.Now;
            long blockIndex = b.OldTip.Index;
            StreamWriter blockIndexFile = new StreamWriter(_blockIndexFilePath);
            blockIndexFile.Write(blockIndex);
            blockIndexFile.Flush();
            blockIndexFile.Close();
            Log.Debug($"Storing Data Complete. Time Taken: {(end - start).Milliseconds} ms.");
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
