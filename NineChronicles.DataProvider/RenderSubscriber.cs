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
    using Lib9c.Renderers;
    using Libplanet;
    using Libplanet.Crypto;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Tx;
    using Microsoft.Extensions.Hosting;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Market;
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
        private readonly List<ShopHistoryFungibleAssetValueModel> _buyShopFavList = new List<ShopHistoryFungibleAssetValueModel>();
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
        private readonly List<PetEnhancementModel> _petEnhancementList = new List<PetEnhancementModel>();
        private readonly List<TransferAssetModel> _transferAssetList = new List<TransferAssetModel>();
        private readonly List<RequestPledgeModel> _requestPledgeList = new List<RequestPledgeModel>();
        private readonly List<string> _agents;
        private readonly bool _render;
        private int _renderedBlockCount;
        private DateTimeOffset _blockTimeOffset;
        private Address _miner;
        private string? _blockHash;

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
            _render = Convert.ToBoolean(Environment.GetEnvironmentVariable("NC_Render") ?? "true");
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
                _blockHash = block.Hash.ToString();
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

                            if (ev.Action is ITransferAsset transferAsset)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var actionString = ev.TxId.ToString();
                                var actionByteArray = Encoding.UTF8.GetBytes(actionString!).Take(16).ToArray();
                                var id = new Guid(actionByteArray);
                                _transferAssetList.Add(TransferAssetData.GetTransferAssetInfo(
                                    id,
                                    (TxId)ev.TxId!,
                                    ev.BlockIndex,
                                    _blockHash!,
                                    transferAsset.Sender,
                                    transferAsset.Recipient,
                                    transferAsset.Amount.Currency.Ticker,
                                    transferAsset.Amount,
                                    _blockTimeOffset));

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored TransferAsset action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is IClaimStakeReward claimStakeReward)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var plainValue = (Dictionary)claimStakeReward.PlainValue;
                                var avatarAddress = ((Dictionary)plainValue["values"])[AvatarAddressKey].ToAddress();
                                var id = ((GameAction)claimStakeReward).Id;
#pragma warning disable CS0618
                                var runeCurrency = RuneHelper.StakeRune;
#pragma warning restore CS0618
                                var prevRuneBalance = ev.PreviousState.GetBalance(
                                    avatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ev.OutputState.GetBalance(
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
                                    runeCurrency.Ticker,
                                    acquiredRune,
                                    _blockTimeOffset));
                                _claimStakeList.Add(ClaimStakeRewardData.GetClaimStakeRewardInfo(claimStakeReward, ev.PreviousState, ev.OutputState, ev.Signer, ev.BlockIndex, _blockTimeOffset));
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
                            var actionType = eventDungeonBattle.ToString()!.Split('.').LastOrDefault()
                                ?.Replace(">", string.Empty);
                            _eventDungeonBattleList.Add(EventDungeonBattleData.GetEventDungeonBattleInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                eventDungeonBattle.AvatarAddress,
                                eventDungeonBattle.EventScheduleId,
                                eventDungeonBattle.EventDungeonId,
                                eventDungeonBattle.EventDungeonStageId,
                                eventDungeonBattle.Foods.Count,
                                eventDungeonBattle.Costumes.Count,
                                eventDungeonBattle.Equipments.Count,
                                eventDungeonBattle.Id,
                                actionType!,
                                ev.BlockIndex,
                                _blockTimeOffset));
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
                            _eventConsumableItemCraftsList.Add(EventConsumableItemCraftsData.GetEventConsumableItemCraftsInfo(eventConsumableItemCrafts, ev.PreviousState, ev.OutputState, ev.Signer, ev.BlockIndex, _blockTimeOffset));
                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored EventConsumableItemCrafts action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("RenderSubscriber: {message}", ex.Message);
                    }
                });

            _actionRenderer.EveryRender<RequestPledge>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } requestPledge)
                        {
                            var start = DateTimeOffset.UtcNow;
                            _requestPledgeList.Add(RequestPledgeData.GetRequestPledgeInfo(ev.TxId.ToString()!, ev.BlockIndex, _blockHash!, ev.Signer, requestPledge.AgentAddress, requestPledge.RefillMead, _blockTimeOffset));

                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored RequestPledge action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
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
                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputState, ev.Signer, has.AvatarAddress, has.RuneInfos, _blockTimeOffset));
                            _hasList.Add(HackAndSlashData.GetHackAndSlashInfo(ev.PreviousState, ev.OutputState, ev.Signer, has.AvatarAddress, has.StageId, has.Id, ev.BlockIndex, _blockTimeOffset));
                            if (has.StageBuffId.HasValue)
                            {
                                _hasWithRandomBuffList.Add(HasWithRandomBuffData.GetHasWithRandomBuffInfo(ev.PreviousState, ev.OutputState, ev.Signer, has.AvatarAddress, has.StageId, has.StageBuffId, has.Id, ev.BlockIndex, _blockTimeOffset));
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
                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputState, ev.Signer, hasSweep.avatarAddress, hasSweep.runeInfos, _blockTimeOffset));
                            _hasSweepList.Add(HackAndSlashSweepData.GetHackAndSlashSweepInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                hasSweep.avatarAddress,
                                hasSweep.stageId,
                                hasSweep.worldId,
                                hasSweep.apStoneCount,
                                hasSweep.actionPoint,
                                hasSweep.costumes.Count,
                                hasSweep.equipments.Count,
                                hasSweep.Id,
                                ev.BlockIndex,
                                _blockTimeOffset));
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
                            _ccList.Add(CombinationConsumableData.GetCombinationConsumableInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                combinationConsumable.avatarAddress,
                                combinationConsumable.recipeId,
                                combinationConsumable.slotIndex,
                                combinationConsumable.Id,
                                ev.BlockIndex));
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
                            _ceList.Add(CombinationEquipmentData.GetCombinationEquipmentInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                combinationEquipment.avatarAddress,
                                combinationEquipment.recipeId,
                                combinationEquipment.slotIndex,
                                combinationEquipment.subRecipeId,
                                combinationEquipment.Id,
                                ev.BlockIndex));
                            if (combinationEquipment.payByCrystal)
                            {
                                var replaceCombinationEquipmentMaterialList = ReplaceCombinationEquipmentMaterialData
                                    .GetReplaceCombinationEquipmentMaterialInfo(
                                        ev.PreviousState,
                                        ev.OutputState,
                                        ev.Signer,
                                        combinationEquipment.avatarAddress,
                                        combinationEquipment.recipeId,
                                        combinationEquipment.subRecipeId,
                                        combinationEquipment.payByCrystal,
                                        combinationEquipment.Id,
                                        ev.BlockIndex,
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

                            var slotState = ev.OutputState.GetCombinationSlotState(
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
                                    ev.PreviousState,
                                    ev.OutputState,
                                    ev.Signer,
                                    itemEnhancement.avatarAddress,
                                    Guid.Empty,
                                    itemEnhancement.materialIds,
                                    itemEnhancement.itemId,
                                    itemEnhancement.Id,
                                    ev.BlockIndex,
                                    _blockTimeOffset) is { } itemEnhancementFailModel)
                            {
                                _itemEnhancementFailList.Add(itemEnhancementFailModel);
                            }

                            _ieList.Add(ItemEnhancementData.GetItemEnhancementInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                itemEnhancement.avatarAddress,
                                itemEnhancement.slotIndex,
                                Guid.Empty,
                                itemEnhancement.materialIds,
                                itemEnhancement.itemId,
                                itemEnhancement.Id,
                                ev.BlockIndex));
                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored ItemEnhancement action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            start = DateTimeOffset.UtcNow;

                            var slotState = ev.OutputState.GetCombinationSlotState(
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
                            AvatarState avatarState = ev.OutputState.GetAvatarStateV2(buy.buyerAvatarAddress);
                            var buyerInventory = avatarState.inventory;
                            foreach (var purchaseInfo in buy.purchaseInfos)
                            {
                                var state = ev.OutputState.GetState(
                                Addresses.GetItemAddress(purchaseInfo.TradableId));
                                ITradableItem orderItem =
                                    (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                Order order =
                                    OrderFactory.Deserialize(
                                        (Dictionary)ev.OutputState.GetState(
                                            Order.DeriveAddress(purchaseInfo.OrderId))!);
                                int itemCount = order is FungibleOrder fungibleOrder
                                    ? fungibleOrder.ItemCount
                                    : 1;
                                AddShopHistoryItem(orderItem, buy.buyerAvatarAddress, purchaseInfo, itemCount, ev.BlockIndex);

                                if (purchaseInfo.ItemSubType == ItemSubType.Armor
                                    || purchaseInfo.ItemSubType == ItemSubType.Belt
                                    || purchaseInfo.ItemSubType == ItemSubType.Necklace
                                    || purchaseInfo.ItemSubType == ItemSubType.Ring
                                    || purchaseInfo.ItemSubType == ItemSubType.Weapon)
                                {
                                    var sellerState = ev.OutputState.GetAvatarStateV2(purchaseInfo.SellerAvatarAddress);
                                    var sellerInventory = sellerState.inventory;

                                    if (buyerInventory.Equipments == null || sellerInventory.Equipments == null)
                                    {
                                        continue;
                                    }

                                    Equipment? equipment = buyerInventory.Equipments.SingleOrDefault(i =>
                                        i.ItemId == purchaseInfo.TradableId) ?? sellerInventory.Equipments.SingleOrDefault(i =>
                                        i.ItemId == purchaseInfo.TradableId);

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

            _actionRenderer.EveryRender<BuyProduct>()
                .Subscribe(ev =>
                {
                    try
                    {
                        if (ev.Exception == null && ev.Action is { } buy)
                        {
                            var start = DateTimeOffset.UtcNow;
                            foreach (var productInfo in buy.ProductInfos)
                            {
                                switch (productInfo)
                                {
                                    case FavProductInfo _:
                                        // Check previous product state. because Set Bencodex.Types.Null in BuyProduct.
                                        if (ev.PreviousState.TryGetState(Product.DeriveAddress(productInfo.ProductId), out List productState))
                                        {
                                            var favProduct = (FavProduct)ProductFactory.DeserializeProduct(productState);
                                            _buyShopFavList.Add(new ShopHistoryFungibleAssetValueModel
                                            {
                                                OrderId = productInfo.ProductId.ToString(),
                                                TxId = ev.TxId.ToString(),
                                                BlockIndex = ev.BlockIndex,
                                                BlockHash = _blockHash,
                                                SellerAvatarAddress = productInfo.AvatarAddress.ToString(),
                                                BuyerAvatarAddress = buy.AvatarAddress.ToString(),
                                                Price = decimal.Parse(productInfo.Price.GetQuantityString()),
                                                Quantity = decimal.Parse(favProduct.Asset.GetQuantityString()),
                                                Ticker = favProduct.Asset.Currency.Ticker,
                                                TimeStamp = _blockTimeOffset,
                                            });
                                        }

                                        break;
                                    case ItemProductInfo itemProductInfo:
                                    {
                                        ITradableItem orderItem;
                                        int itemCount = 1;

                                        // backward compatibility for order.
                                        if (itemProductInfo.Legacy)
                                        {
                                            var state = ev.OutputState.GetState(
                                                Addresses.GetItemAddress(itemProductInfo.TradableId));
                                            orderItem =
                                                (ITradableItem)ItemFactory.Deserialize((Dictionary)state!);
                                            Order order =
                                                OrderFactory.Deserialize(
                                                    (Dictionary)ev.OutputState.GetState(
                                                        Order.DeriveAddress(itemProductInfo.ProductId))!);
                                            itemCount = order is FungibleOrder fungibleOrder
                                                ? fungibleOrder.ItemCount
                                                : 1;
                                        }
                                        else
                                        {
                                            // Check previous product state. because Set Bencodex.Types.Null in BuyProduct.
                                            if (ev.PreviousState.TryGetState(Product.DeriveAddress(productInfo.ProductId), out List state))
                                            {
                                                var product = (ItemProduct)ProductFactory.DeserializeProduct(state);
                                                orderItem = product.TradableItem;
                                                itemCount = product.ItemCount;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                        }

                                        var purchaseInfo = new PurchaseInfo(
                                            productInfo.ProductId,
                                            itemProductInfo.TradableId,
                                            productInfo.AgentAddress,
                                            productInfo.AvatarAddress,
                                            itemProductInfo.ItemSubType,
                                            productInfo.Price
                                        );
                                        AddShopHistoryItem(orderItem, buy.AvatarAddress, purchaseInfo, itemCount, ev.BlockIndex);
                                        if (orderItem.ItemType == ItemType.Equipment)
                                        {
                                            var equipment = (Equipment)orderItem;
                                            _eqList.Add(EquipmentData.GetEquipmentInfo(
                                                ev.Signer,
                                                buy.AvatarAddress,
                                                equipment));
                                        }

                                        break;
                                    }
                                }
                            }

                            var end = DateTimeOffset.UtcNow;
                            Log.Debug(
                                "Stored avatar {address}'s equipment in block #{index}. Time Taken: {time} ms.",
                                buy.AvatarAddress,
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
                            _stakeList.Add(StakeData.GetStakeInfo(ev.PreviousState, ev.OutputState, ev.Signer, ev.BlockIndex, _blockTimeOffset));
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
                            _mmcList.Add(MigrateMonsterCollectionData.GetMigrateMonsterCollectionInfo(ev.PreviousState, ev.OutputState, ev.Signer, ev.BlockIndex, _blockTimeOffset));
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

                            var grindList = GrindingData.GetGrindingInfo(ev.PreviousState, ev.OutputState, ev.Signer, grinding.AvatarAddress, grinding.EquipmentIds, grinding.Id, ev.BlockIndex, _blockTimeOffset);

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
                            var unlockEquipmentRecipeList = UnlockEquipmentRecipeData.GetUnlockEquipmentRecipeInfo(ev.PreviousState, ev.OutputState, ev.Signer, unlockEquipmentRecipe.AvatarAddress, unlockEquipmentRecipe.RecipeIds, unlockEquipmentRecipe.Id, ev.BlockIndex, _blockTimeOffset);
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
                            var unlockWorldList = UnlockWorldData.GetUnlockWorldInfo(ev.PreviousState, ev.OutputState, ev.Signer, unlockWorld.AvatarAddress, unlockWorld.WorldIds, unlockWorld.Id, ev.BlockIndex, _blockTimeOffset);
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
                            _hasRandomBuffList.Add(HackAndSlashRandomBuffData.GetHasRandomBuffInfo(ev.PreviousState, ev.OutputState, ev.Signer, hasRandomBuff.AvatarAddress, hasRandomBuff.AdvancedGacha, hasRandomBuff.Id, ev.BlockIndex, _blockTimeOffset));
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
                            _joinArenaList.Add(JoinArenaData.GetJoinArenaInfo(ev.PreviousState, ev.OutputState, ev.Signer, joinArena.avatarAddress, joinArena.round, joinArena.championshipId, joinArena.Id, ev.BlockIndex, _blockTimeOffset));
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
                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputState, ev.Signer, battleArena.myAvatarAddress, battleArena.runeInfos, _blockTimeOffset));
                            _battleArenaList.Add(BattleArenaData.GetBattleArenaInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                battleArena.myAvatarAddress,
                                battleArena.enemyAvatarAddress,
                                battleArena.round,
                                battleArena.championshipId,
                                battleArena.ticket,
                                battleArena.Id,
                                ev.BlockIndex,
                                _blockTimeOffset));
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
                            _battleGrandFinaleList.Add(BattleGrandFinaleData.GetBattleGrandFinaleInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                battleGrandFinale.myAvatarAddress,
                                battleGrandFinale.enemyAvatarAddress,
                                battleGrandFinale.grandFinaleId,
                                battleGrandFinale.Id,
                                ev.BlockIndex,
                                _blockTimeOffset));
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
                            _eventMaterialItemCraftsList.Add(EventMaterialItemCraftsData.GetEventMaterialItemCraftsInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                eventMaterialItemCrafts.AvatarAddress,
                                eventMaterialItemCrafts.MaterialsToUse,
                                eventMaterialItemCrafts.EventScheduleId,
                                eventMaterialItemCrafts.EventMaterialItemRecipeId,
                                eventMaterialItemCrafts.Id,
                                ev.BlockIndex,
                                _blockTimeOffset));
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
                            _runeEnhancementList.Add(RuneEnhancementData.GetRuneEnhancementInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                runeEnhancement.AvatarAddress,
                                runeEnhancement.RuneId,
                                runeEnhancement.TryCount,
                                runeEnhancement.Id,
                                ev.BlockIndex,
                                _blockTimeOffset));
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
                            int count = 0;
                            foreach (var recipient in transferAssets.Recipients)
                            {
                                var actionString = count + ev.TxId.ToString();
                                var actionByteArray = Encoding.UTF8.GetBytes(actionString).Take(16).ToArray();
                                var id = new Guid(actionByteArray);
                                var avatarAddress = recipient.recipient;
                                var actionType = transferAssets.ToString()!.Split('.').LastOrDefault()
                                    ?.Replace(">", string.Empty);
                                _transferAssetList.Add(TransferAssetData.GetTransferAssetInfo(
                                    id,
                                    (TxId)ev.TxId!,
                                    ev.BlockIndex,
                                    _blockHash!,
                                    transferAssets.Sender,
                                    recipient.recipient,
                                    recipient.amount.Currency.Ticker,
                                    recipient.amount,
                                    _blockTimeOffset));
                                _runesAcquiredList.Add(RunesAcquiredData.GetRunesAcquiredInfo(
                                    id,
                                    ev.Signer,
                                    avatarAddress,
                                    ev.BlockIndex,
                                    actionType!,
                                    recipient.amount.Currency.Ticker,
                                    recipient.amount,
                                    _blockTimeOffset));
                                count++;
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
                            var runeCurrency = RuneHelper.DailyRewardRune;
#pragma warning restore CS0618
                            var prevRuneBalance = ev.PreviousState.GetBalance(
                                dailyReward.avatarAddress,
                                runeCurrency);
                            var outputRuneBalance = ev.OutputState.GetBalance(
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
                                runeCurrency.Ticker,
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
                            var sheets = ev.OutputState.GetSheets(
                                sheetTypes: new[]
                                {
                                    typeof(RuneSheet),
                                });
                            var runeSheet = sheets.GetSheet<RuneSheet>();
                            foreach (var runeType in runeSheet.Values)
                            {
#pragma warning disable CS0618
                                var runeCurrency = RuneHelper.ToCurrency(runeType);
#pragma warning restore CS0618
                                var prevRuneBalance = ev.PreviousState.GetBalance(
                                    claimRaidReward.AvatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ev.OutputState.GetBalance(
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
                                        runeCurrency.Ticker,
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
                            _unlockRuneSlotList.Add(UnlockRuneSlotData.GetUnlockRuneSlotInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                unlockRuneSlot.AvatarAddress,
                                unlockRuneSlot.SlotIndex,
                                unlockRuneSlot.Id,
                                ev.BlockIndex,
                                _blockTimeOffset));
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
                            _rapidCombinationList.Add(RapidCombinationData.GetRapidCombinationInfo(
                                ev.PreviousState,
                                ev.OutputState,
                                ev.Signer,
                                rapidCombination.avatarAddress,
                                rapidCombination.slotIndex,
                                rapidCombination.Id,
                                ev.BlockIndex,
                                _blockTimeOffset));
                            var end = DateTimeOffset.UtcNow;
                            Log.Debug("Stored RapidCombination action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
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
                            var sheets = ev.OutputState.GetSheets(
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
                                var runeCurrency = RuneHelper.ToCurrency(runeType);
#pragma warning restore CS0618
                                var prevRuneBalance = ev.PreviousState.GetBalance(
                                    ev.Action.AvatarAddress,
                                    runeCurrency);
                                var outputRuneBalance = ev.OutputState.GetBalance(
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
                                        runeCurrency.Ticker,
                                        acquiredRune,
                                        _blockTimeOffset));
                                }
                            }

                            _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputState, ev.Signer, ev.Action.AvatarAddress, ev.Action.RuneInfos, _blockTimeOffset));

                            int raidId = 0;
                            bool found = false;
                            for (int i = 0; i < 99; i++)
                            {
                                if (ev.OutputState.Delta.UpdatedAddresses.Contains(
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
                                    ev.OutputState.GetRaiderState(ev.Action.AvatarAddress, raidId);
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

            _actionRenderer.EveryRender<PetEnhancement>().Subscribe(ev =>
            {
                try
                {
                    if (ev.Exception == null && ev.Action is { } petEnhancement)
                    {
                        var start = DateTimeOffset.UtcNow;
                        _petEnhancementList.Add(PetEnhancementData.GetPetEnhancementInfo(
                            ev.PreviousState,
                            ev.OutputState,
                            ev.Signer,
                            petEnhancement.AvatarAddress,
                            petEnhancement.PetId,
                            petEnhancement.TargetLevel,
                            petEnhancement.Id,
                            ev.BlockIndex,
                            _blockTimeOffset
                        ));
                        var end = DateTimeOffset.UtcNow;
                        Log.Debug("Stored PetEnhancement action in block #{BlockIndex}. Time taken: {Time} ms", ev.BlockIndex, end - start);
                    }
                }
                catch (Exception e)
                {
                    Log.Error("PetEnhancement RenderSubscriber: {Message}", e.Message);
                }
            });

            return Task.CompletedTask;
        }

        private void AddShopHistoryItem(ITradableItem orderItem, Address buyerAvatarAddress, PurchaseInfo purchaseInfo, int itemCount, long blockIndex)
        {
            if (orderItem.ItemType == ItemType.Equipment)
            {
                Equipment equipment = (Equipment)orderItem;
                _buyShopEquipmentsList.Add(ShopHistoryEquipmentData.GetShopHistoryEquipmentInfo(
                    buyerAvatarAddress,
                    purchaseInfo,
                    equipment,
                    itemCount,
                    blockIndex,
                    _blockTimeOffset));
            }

            if (orderItem.ItemType == ItemType.Costume)
            {
                Costume costume = (Costume)orderItem;
                _buyShopCostumesList.Add(ShopHistoryCostumeData.GetShopHistoryCostumeInfo(
                    buyerAvatarAddress,
                    purchaseInfo,
                    costume,
                    itemCount,
                    blockIndex,
                    _blockTimeOffset));
            }

            if (orderItem.ItemType == ItemType.Material)
            {
                Material material = (Material)orderItem;
                _buyShopMaterialsList.Add(ShopHistoryMaterialData.GetShopHistoryMaterialInfo(
                    buyerAvatarAddress,
                    purchaseInfo,
                    material,
                    itemCount,
                    blockIndex,
                    _blockTimeOffset));
            }

            if (orderItem.ItemType == ItemType.Consumable)
            {
                Consumable consumable = (Consumable)orderItem;
                _buyShopConsumablesList.Add(ShopHistoryConsumableData.GetShopHistoryConsumableInfo(
                    buyerAvatarAddress,
                    purchaseInfo,
                    consumable,
                    itemCount,
                    blockIndex,
                    _blockTimeOffset));
            }
        }

        private void ProcessAgentAvatarData(ActionEvaluation<ActionBase> ev)
        {
            if (!_agents.Contains(ev.Signer.ToString()))
            {
                _agents.Add(ev.Signer.ToString());
                _agentList.Add(AgentData.GetAgentInfo(ev.Signer));

                if (ev.Signer != _miner)
                {
                    var agentState = ev.OutputState.GetAgentState(ev.Signer);
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
                                    avatarState = ev.OutputState.GetAvatarStateV2(avatarAddress);
                                }
                                catch (Exception)
                                {
                                    avatarState = ev.OutputState.GetAvatarState(address: avatarAddress);
                                }

                                if (avatarState == null)
                                {
                                    continue;
                                }

                                var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
                                var runeSlotState = ev.OutputState.TryGetState(runeSlotStateAddress, out List rawRuneSlotState)
                                    ? new RuneSlotState(rawRuneSlotState)
                                    : new RuneSlotState(BattleType.Adventure);
                                var runeSlotInfos = runeSlotState.GetEquippedRuneSlotInfos();

                                _avatarList.Add(AvatarData.GetAvatarInfo(ev.OutputState, ev.Signer, avatarAddress, runeSlotInfos, _blockTimeOffset));
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

        private void StoreRenderedData((Block OldTip, Block NewTip) b)
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
                    MySqlStore.StoreShopHistoryFungibleAssetValues(_buyShopFavList);
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
                    MySqlStore.StorePetEnhancementList(_petEnhancementList);
                    MySqlStore.StoreTransferAssetList(_transferAssetList);
                    MySqlStore.StoreRequestPledgeList(_requestPledgeList);
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
            _buyShopFavList.Clear();
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
            _petEnhancementList.Clear();
            _transferAssetList.Clear();
            _requestPledgeList.Clear();
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
