namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
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
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.DataRendering;
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

        public static string GetSignerAndOtherAddressesHex(Address signer, params Address[] addresses)
        {
            StringBuilder sb = new StringBuilder($"[{signer.ToHex()}");

            foreach (Address address in addresses)
            {
                sb.Append($", {address.ToHex()}");
            }

            sb.Append("]");
            return sb.ToString();
        }

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
                _blockList.Add(BlockData.GetBlockInfo(block));

                foreach (var transaction in block.Transactions)
                {
                    _transactionList.Add(TransactionData.GetTransactionInfo(block, transaction));
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
                                var actionType = claimStakeReward.ToString()!.Split('.').LastOrDefault()?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    id,
                                    ev.Signer,
                                    avatarAddress,
                                    ev.BlockIndex,
                                    actionType!,
                                    acquiredRune,
                                    _blockTimeOffset));
                                _claimStakeList.Add(ClaimStakeRewardData.GetClaimStakeRewardInfo(ev, claimStakeReward, _blockTimeOffset));
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
                            _eventDungeonBattleList.Add(EventDungeonBattleData.GetEventDungeonBattleInfo(ev, eventDungeonBattle, _blockTimeOffset));
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
                            _eventConsumableItemCraftsList.Add(EventConsumableItemCraftsData.GetEventConsumableItemCraftsInfo(ev, eventConsumableItemCrafts, _blockTimeOffset));
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
                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputStates, ev.Signer, has.AvatarAddress, has.RuneInfos, _blockTimeOffset));
                            _hasList.Add(HackAndSlashData.GetHackAndSlashInfo(ev, has, _blockTimeOffset));
                            if (has.StageBuffId.HasValue)
                            {
                                _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(ev, has, _blockTimeOffset));
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
                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputStates, ev.Signer, hasSweep.avatarAddress, hasSweep.runeInfos, _blockTimeOffset));
                            _hasSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(ev, hasSweep, _blockTimeOffset));
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
                            _ccList.Add(CombinationConsumableData.GetCombinationConsumableInfo(ev, combinationConsumable));
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
                            _ceList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(ev, combinationEquipment));
                            if (combinationEquipment.payByCrystal)
                            {
                                var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                    .GetReplaceCombinationEquipmentMaterialInfo(
                                        ev,
                                        combinationEquipment,
                                        _blockTimeOffset);
                                foreach (var replaceCombinationEquipmentMaterial in replaceCombinationEquipmentMaterialList)
                                {
                                    _replaceCombinationEquipmentMaterialList.Add(replaceCombinationEquipmentMaterial);
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
                                _eqList.Add(EquipmentData.GetEquipmentInfo(
                                    ev.Signer,
                                    combinationEquipment.avatarAddress,
                                    (Equipment)slotState.Result.itemUsable));
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
                            if (ItemEnhancementFailData.GetItemEnhancementFailInfo(
                                    ev,
                                    itemEnhancement,
                                    _blockTimeOffset) is { } itemEnhancementFailModel)
                            {
                                _itemEnhancementFailList.Add(itemEnhancementFailModel);
                            }

                            _ieList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                ev,
                                itemEnhancement));
                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored ItemEnhancement action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            start = DateTimeOffset.UtcNow;

                            var slotState = ev.OutputStates.GetCombinationSlotState(
                                itemEnhancement.avatarAddress,
                                itemEnhancement.slotIndex);

                            if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                            {
                                _eqList.Add(EquipmentData.GetEquipmentInfo(
                                    ev.Signer,
                                    itemEnhancement.avatarAddress,
                                    (Equipment)slotState.Result.itemUsable));
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
                                    _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfo(
                                        ev,
                                        buy,
                                        purchaseInfo,
                                        equipment,
                                        itemCount,
                                        _blockTimeOffset));
                                }

                                if (orderItem.ItemType == ItemType.Costume)
                                {
                                    Costume costume = (Costume)orderItem;
                                    _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfo(
                                        ev,
                                        buy,
                                        purchaseInfo,
                                        costume,
                                        itemCount,
                                        _blockTimeOffset));
                                }

                                if (orderItem.ItemType == ItemType.Material)
                                {
                                    Material material = (Material)orderItem;
                                    _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfo(
                                        ev,
                                        buy,
                                        purchaseInfo,
                                        material,
                                        itemCount,
                                        _blockTimeOffset));
                                }

                                if (orderItem.ItemType == ItemType.Consumable)
                                {
                                    Consumable consumable = (Consumable)orderItem;
                                    _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfo(
                                        ev,
                                        buy,
                                        purchaseInfo,
                                        consumable,
                                        itemCount,
                                        _blockTimeOffset));
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
                                        _eqList.Add(EquipmentData.GetEquipmentInfo(
                                            ev.Signer,
                                            buy.buyerAvatarAddress,
                                            equipmentNotNull));
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
                            _stakeList.Add(StakeData.GetStakeInfo(ev, _blockTimeOffset));
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
                            _mmcList.Add(MigrateMonsterCollectionData.GetMigrateMonsterCollectionInfo(ev, _blockTimeOffset));
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
                        if (ev.Action is { } grinding)
                        {
                            var start = DateTimeOffset.UtcNow;

                            var grindList = GrindingData.GetGrindingInfo(ev, grinding, _blockTimeOffset);

                            foreach (var grind in grindList)
                            {
                                _grindList.Add(grind);
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
                            var unlockEquipmentRecipeList = UnlockEquipmentRecipeData.GetUnlockEquipmentRecipeInfo(ev, unlockEquipmentRecipe, _blockTimeOffset);
                            foreach (var unlockEquipmentRecipeData in unlockEquipmentRecipeList)
                            {
                                _unlockEquipmentRecipeList.Add(unlockEquipmentRecipeData);
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
                            var unlockWorldList = UnlockWorldData.GetUnlockWorldInfo(ev, unlockWorld, _blockTimeOffset);
                            foreach (var unlockWorldData in unlockWorldList)
                            {
                                _unlockWorldList.Add(unlockWorldData);
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
                            _hasRandomBuffList.Add(HackAndSlashRandomBuffData.GetHasRandomBuffInfo(ev, hasRandomBuff, _blockTimeOffset));
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
                            _joinArenaList.Add(JoinArenaData.GetJoinArenaInfo(ev, joinArena, _blockTimeOffset));
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
                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputStates, ev.Signer, battleArena.myAvatarAddress, battleArena.runeInfos, _blockTimeOffset));
                            _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(ev, battleArena, _blockTimeOffset));
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
                            _battleGrandFinaleList.Add(BattleGrandFinaleData.GetBattleGrandFinaleInfo(ev, battleGrandFinale, _blockTimeOffset));
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
                            _eventMaterialItemCraftsList.Add(EventMaterialItemCraftsData.GetEventMaterialItemCraftsInfo(ev, eventMaterialItemCrafts, _blockTimeOffset));
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
                            _runeEnhancementList.Add(RuneEnhancementData.GetRuneEnhancementInfo(ev, runeEnhancement, _blockTimeOffset));
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
                                var actionType = transferAssets.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    id,
                                    ev.Signer,
                                    avatarAddress,
                                    ev.BlockIndex,
                                    actionType!,
                                    recipient.amount,
                                    _blockTimeOffset));
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
                            var actionType = dailyReward.ToString()!.Split('.').LastOrDefault()
                                ?.Replace(">", string.Empty);
                            _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                dailyReward.Id,
                                ev.Signer,
                                dailyReward.avatarAddress,
                                ev.BlockIndex,
                                actionType!,
                                acquiredRune,
                                _blockTimeOffset));
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
                                var actionType = claimRaidReward.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                {
                                    _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                        claimRaidReward.Id,
                                        ev.Signer,
                                        claimRaidReward.AvatarAddress,
                                        ev.BlockIndex,
                                        actionType!,
                                        acquiredRune,
                                        _blockTimeOffset));
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
                            _unlockRuneSlotList.Add(UnlockRuneSlotData.GetUnlockRuneSlotInfo(ev, unlockRuneSlot, _blockTimeOffset));
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
                            _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(ev, rapidCombination, _blockTimeOffset));
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
                                    _eqList.Add(EquipmentData.GetEquipmentInfo(
                                        ev.Signer,
                                        combinationEquipment.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
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
                                    _eqList.Add(EquipmentData.GetEquipmentInfo(
                                        ev.Signer,
                                        itemEnhancement.avatarAddress,
                                        (Equipment)slotState.Result.itemUsable));
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
                                            _eqList.Add(EquipmentData.GetEquipmentInfo(
                                                purchaseInfo.SellerAgentAddress,
                                                purchaseInfo.SellerAvatarAddress,
                                                equipmentNotNull));
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
                            var sheets = ev.OutputStates.GetSheets(
                                sheetTypes: new[]
                                {
                                    typeof(CharacterSheet),
                                    typeof(CostumeStatSheet),
                                    typeof(RuneSheet),
                                    typeof(RuneListSheet),
                                    typeof(RuneOptionSheet),
                                });

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
                                var actionType = ev.Action.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                if (Convert.ToDecimal(acquiredRune.GetQuantityString()) > 0)
                                {
                                    _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                        ev.Action.Id,
                                        ev.Signer,
                                        ev.Action.AvatarAddress,
                                        ev.BlockIndex,
                                        actionType!,
                                        acquiredRune,
                                        _blockTimeOffset));
                                }
                            }

                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputStates, ev.Signer, ev.Action.AvatarAddress, ev.Action.RuneInfos, _blockTimeOffset));

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
                                _raiderList.Add(RaidData.GetRaidInfo(raidId, raiderState));
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

        private void ProcessAgentAvatarData(ActionBase.ActionEvaluation<ActionBase> ev)
        {
            if (!_agents.Contains(ev.Signer.ToString()))
            {
                _agents.Add(ev.Signer.ToString());
                _agentList.Add(AgentData.GetAgentInfo(ev));

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

                                var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
                                var runeSlotState = ev.OutputStates.TryGetState(runeSlotStateAddress, out List rawRuneSlotState)
                                    ? new RuneSlotState(rawRuneSlotState)
                                    : new RuneSlotState(BattleType.Adventure);
                                var runeSlotInfos = runeSlotState.GetEquippedRuneSlotInfos();

                                _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputStates, ev.Signer, avatarAddress, runeSlotInfos, _blockTimeOffset));
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
    }
}
