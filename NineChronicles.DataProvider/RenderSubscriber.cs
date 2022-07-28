namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
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
        private readonly List<AgentModel> _hasAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _hasAvatarList = new List<AvatarModel>();
        private readonly List<HackAndSlashModel> _hasList = new List<HackAndSlashModel>();
        private readonly List<AgentModel> _rbAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _rbAvatarList = new List<AvatarModel>();
        private readonly List<AgentModel> _ccAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _ccAvatarList = new List<AvatarModel>();
        private readonly List<CombinationConsumableModel> _ccList = new List<CombinationConsumableModel>();
        private readonly List<AgentModel> _ceAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _ceAvatarList = new List<AvatarModel>();
        private readonly List<CombinationEquipmentModel> _ceList = new List<CombinationEquipmentModel>();
        private readonly List<AgentModel> _eqAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _eqAvatarList = new List<AvatarModel>();
        private readonly List<EquipmentModel> _eqList = new List<EquipmentModel>();
        private readonly List<AgentModel> _ieAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _ieAvatarList = new List<AvatarModel>();
        private readonly List<ItemEnhancementModel> _ieList = new List<ItemEnhancementModel>();
        private readonly List<AgentModel> _buyAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _buyAvatarList = new List<AvatarModel>();
        private readonly List<ShopHistoryEquipmentModel> _buyShopEquipmentsList = new List<ShopHistoryEquipmentModel>();
        private readonly List<ShopHistoryCostumeModel> _buyShopCostumesList = new List<ShopHistoryCostumeModel>();
        private readonly List<ShopHistoryMaterialModel> _buyShopMaterialsList = new List<ShopHistoryMaterialModel>();
        private readonly List<ShopHistoryConsumableModel> _buyShopConsumablesList = new List<ShopHistoryConsumableModel>();
        private readonly List<AgentModel> _stakeAgentList = new List<AgentModel>();
        private readonly List<StakeModel> _stakeList = new List<StakeModel>();
        private readonly List<AgentModel> _claimStakeAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _claimStakeAvatarList = new List<AvatarModel>();
        private readonly List<ClaimStakeRewardModel> _claimStakeList = new List<ClaimStakeRewardModel>();
        private readonly List<AgentModel> _mmcAgentList = new List<AgentModel>();
        private readonly List<MigrateMonsterCollectionModel> _mmcList = new List<MigrateMonsterCollectionModel>();
        private readonly List<AgentModel> _grindAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _grindAvatarList = new List<AvatarModel>();
        private readonly List<GrindingModel> _grindList = new List<GrindingModel>();
        private readonly List<ItemEnhancementFailModel> _itemEnhancementFailList = new List<ItemEnhancementFailModel>();
        private readonly List<AgentModel> _unlockEquipmentRecipeAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _unlockEquipmentRecipeAvatarList = new List<AvatarModel>();
        private readonly List<UnlockEquipmentRecipeModel> _unlockEquipmentRecipeList = new List<UnlockEquipmentRecipeModel>();
        private readonly List<AgentModel> _unlockWorldAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _unlockWorldAvatarList = new List<AvatarModel>();
        private readonly List<UnlockWorldModel> _unlockWorldList = new List<UnlockWorldModel>();
        private readonly List<ReplaceCombinationEquipmentMaterialModel> _replaceCombinationEquipmentMaterialList = new List<ReplaceCombinationEquipmentMaterialModel>();
        private readonly List<AgentModel> _hasRandomBuffAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _hasRandomBuffAvatarList = new List<AvatarModel>();
        private readonly List<HasRandomBuffModel> _hasRandomBuffList = new List<HasRandomBuffModel>();
        private readonly List<HasWithRandomBuffModel> _hasWithRandomBuffList = new List<HasWithRandomBuffModel>();
        private readonly List<AgentModel> _joinArenaAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _joinArenaAvatarList = new List<AvatarModel>();
        private readonly List<JoinArenaModel> _joinArenaList = new List<JoinArenaModel>();
        private readonly List<AgentModel> _battleArenaAgentList = new List<AgentModel>();
        private readonly List<AvatarModel> _battleArenaAvatarList = new List<AvatarModel>();
        private readonly List<BattleArenaModel> _battleArenaList = new List<BattleArenaModel>();
        private readonly List<BlockModel> _blockList = new List<BlockModel>();
        private readonly List<TransactionModel> _transactionList = new List<TransactionModel>();
        private int _renderedBlockCount;

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
                var block = b.NewTip;
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
                    var actionType = transaction.Actions.Select(action => action.ToString()!.Split('.')
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

                if (_renderedBlockCount == _blockInsertInterval)
                {
                    var start = DateTimeOffset.Now;
                    Log.Debug("Storing Data");
                    MySqlStore.StoreAgentList(_hasAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_hasAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreHackAndSlashList(_hasList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAgentList(_rbAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_rbAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAgentList(_ccAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_ccAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreCombinationConsumableList(_ccList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAgentList(_ceAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_ceAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreCombinationEquipmentList(_ceList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAgentList(_ieAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_ieAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreItemEnhancementList(_ieList.GroupBy(i => i.Id).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAgentList(_buyAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_buyAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreShopHistoryEquipmentList(_buyShopEquipmentsList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreShopHistoryCostumeList(_buyShopCostumesList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreShopHistoryMaterialList(_buyShopMaterialsList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreShopHistoryConsumableList(_buyShopConsumablesList.GroupBy(i => i.OrderId).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAgentList(_eqAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_eqAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.ProcessEquipmentList(_eqList.GroupBy(i => i.ItemId).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAgentList(_stakeAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreStakingList(_stakeList);
                    MySqlStore.StoreAgentList(_claimStakeAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_claimStakeAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreClaimStakeRewardList(_claimStakeList);
                    MySqlStore.StoreAgentList(_mmcAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreMigrateMonsterCollectionList(_mmcList);
                    MySqlStore.StoreAgentList(_grindAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_grindAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreGrindList(_grindList);
                    MySqlStore.StoreItemEnhancementFailList(_itemEnhancementFailList);
                    MySqlStore.StoreAgentList(_unlockEquipmentRecipeAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_unlockEquipmentRecipeAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreUnlockEquipmentRecipeList(_unlockEquipmentRecipeList);
                    MySqlStore.StoreAgentList(_unlockWorldAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_unlockWorldAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreUnlockWorldList(_unlockWorldList);
                    MySqlStore.StoreReplaceCombinationEquipmentMaterialList(_replaceCombinationEquipmentMaterialList);
                    MySqlStore.StoreAgentList(_hasRandomBuffAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_hasRandomBuffAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreHasRandomBuffList(_hasRandomBuffList);
                    MySqlStore.StoreHasWithRandomBuffList(_hasWithRandomBuffList);
                    MySqlStore.StoreAgentList(_joinArenaAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_joinArenaAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreJoinArenaList(_joinArenaList);
                    MySqlStore.StoreAgentList(_battleArenaAgentList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreAvatarList(_battleArenaAvatarList.GroupBy(i => i.Address).Select(i => i.FirstOrDefault()).ToList());
                    MySqlStore.StoreBattleArenaList(_battleArenaList);
                    MySqlStore.StoreBlockList(_blockList);
                    MySqlStore.StoreTransactionList(_transactionList);
                    _renderedBlockCount = 0;
                    _hasAgentList.Clear();
                    _hasAvatarList.Clear();
                    _hasList.Clear();
                    _rbAgentList.Clear();
                    _rbAvatarList.Clear();
                    _ccAgentList.Clear();
                    _ccAvatarList.Clear();
                    _ccList.Clear();
                    _ceAgentList.Clear();
                    _ceAvatarList.Clear();
                    _ceList.Clear();
                    _ieAgentList.Clear();
                    _ieAvatarList.Clear();
                    _ieList.Clear();
                    _buyAgentList.Clear();
                    _buyAvatarList.Clear();
                    _buyShopEquipmentsList.Clear();
                    _buyShopCostumesList.Clear();
                    _buyShopMaterialsList.Clear();
                    _buyShopConsumablesList.Clear();
                    _eqAgentList.Clear();
                    _eqAvatarList.Clear();
                    _eqList.Clear();
                    _stakeAgentList.Clear();
                    _stakeList.Clear();
                    _claimStakeAgentList.Clear();
                    _claimStakeAvatarList.Clear();
                    _claimStakeList.Clear();
                    _mmcAgentList.Clear();
                    _mmcList.Clear();
                    _grindAgentList.Clear();
                    _grindAvatarList.Clear();
                    _grindList.Clear();
                    _itemEnhancementFailList.Clear();
                    _unlockEquipmentRecipeAgentList.Clear();
                    _unlockEquipmentRecipeAvatarList.Clear();
                    _unlockEquipmentRecipeList.Clear();
                    _unlockWorldAgentList.Clear();
                    _unlockWorldAvatarList.Clear();
                    _unlockWorldList.Clear();
                    _replaceCombinationEquipmentMaterialList.Clear();
                    _hasRandomBuffAgentList.Clear();
                    _hasRandomBuffAvatarList.Clear();
                    _hasRandomBuffList.Clear();
                    _hasWithRandomBuffList.Clear();
                    _joinArenaAgentList.Clear();
                    _joinArenaAvatarList.Clear();
                    _joinArenaList.Clear();
                    _battleArenaAgentList.Clear();
                    _battleArenaAvatarList.Clear();
                    _battleArenaList.Clear();
                    _blockList.Clear();
                    _transactionList.Clear();
                    var end = DateTimeOffset.Now;
                    long blockIndex = b.OldTip.Index;
                    StreamWriter blockIndexFile = new StreamWriter(_blockIndexFilePath);
                    blockIndexFile.Write(blockIndex);
                    blockIndexFile.Flush();
                    blockIndexFile.Close();
                    Log.Debug($"Storing Data Complete. Time Taken: {(end - start).Milliseconds} ms.");
                }
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

                            if (ev.Action is HackAndSlash has)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(has.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
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

                                bool isClear = avatarState.stageMap.ContainsKey(has.stageId);

                                _hasAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _hasAvatarList.Add(new AvatarModel()
                                {
                                    Address = has.avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
                                _hasList.Add(new HackAndSlashModel()
                                {
                                    Id = has.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = has.avatarAddress.ToString(),
                                    StageId = has.stageId,
                                    Cleared = isClear,
                                    Mimisbrunnr = has.stageId > 10000000,
                                    BlockIndex = ev.BlockIndex,
                                });
                                if (has.stageBuffId.HasValue)
                                {
                                    _hasWithRandomBuffList.Add(new HasWithRandomBuffModel()
                                    {
                                        Id = has.Id.ToString(),
                                        BlockIndex = ev.BlockIndex,
                                        AgentAddress = ev.Signer.ToString(),
                                        AvatarAddress = has.avatarAddress.ToString(),
                                        StageId = has.stageId,
                                        BuffId = (int)has.stageBuffId,
                                        Cleared = isClear,
                                        TimeStamp = DateTimeOffset.UtcNow,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored HackAndSlash action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is RankingBattle rb)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(rb.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
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

                                _rbAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _rbAvatarList.Add(new AvatarModel()
                                {
                                    Address = rb.avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored RankingBattle avatar data in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is CombinationConsumable combinationConsumable)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(combinationConsumable.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                _ccAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _ccAvatarList.Add(new AvatarModel()
                                {
                                    Address = combinationConsumable.avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
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
                                AvatarState avatarState =
                                    ev.OutputStates.GetAvatarStateV2(combinationEquipment.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                                    costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                _ceAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _ceAvatarList.Add(new AvatarModel()
                                {
                                    Address = combinationEquipment.avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
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
                                                        TimeStamp = DateTimeOffset.UtcNow,
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
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp,
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
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

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

                                if (prevEquipmentLevel == outputEquipmentLevel)
                                {
                                    Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                    Currency ncgCurrency = ev.OutputStates.GetGoldCurrency();
                                    var prevCrystalBalance = previousStates.GetBalance(
                                        ev.Signer,
                                        crystalCurrency);
                                    var outputCrystalBalance = ev.OutputStates.GetBalance(
                                        ev.Signer,
                                        crystalCurrency);
                                    var prevNCGBalance = previousStates.GetBalance(
                                        ev.Signer,
                                        ncgCurrency);
                                    var outputNCGBalance = ev.OutputStates.GetBalance(
                                        ev.Signer,
                                        ncgCurrency);
                                    var gainedCrystal = outputCrystalBalance - prevCrystalBalance;
                                    var burntNCG = prevNCGBalance - outputNCGBalance;
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
                                        TimeStamp = DateTimeOffset.UtcNow,
                                    });
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                _ieAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _ieAvatarList.Add(new AvatarModel()
                                {
                                    Address = itemEnhancement.avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
                                _ieList.Add(new ItemEnhancementModel()
                                {
                                    Id = itemEnhancement.Id.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = itemEnhancement.avatarAddress.ToString(),
                                    ItemId = itemEnhancement.itemId.ToString(),
                                    MaterialId = itemEnhancement.materialId.ToString(),
                                    SlotIndex = itemEnhancement.slotIndex,
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
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp,
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
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                _buyAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _buyAvatarList.Add(new AvatarModel()
                                {
                                    Address = buy.buyerAvatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });

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
                                            TimeStamp = DateTimeOffset.UtcNow,
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
                                            TimeStamp = DateTimeOffset.UtcNow,
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
                                            TimeStamp = DateTimeOffset.UtcNow,
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
                                            TimeStamp = DateTimeOffset.UtcNow,
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
                                                avatarName,
                                                avatarLevel,
                                                avatarTitleId,
                                                avatarArmorId,
                                                avatarCp,
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

                                _stakeAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _stakeList.Add(new StakeModel()
                                {
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    PreviousAmount = Convert.ToDecimal(previousAmount.GetQuantityString()),
                                    NewAmount = Convert.ToDecimal(newAmount.GetQuantityString()),
                                    RemainingNCG = Convert.ToDecimal(balance.GetQuantityString()),
                                    PrevStakeStartBlockIndex = prevStakeStartBlockIndex,
                                    NewStakeStartBlockIndex = newStakeStartBlockIndex,
                                    TimeStamp = DateTimeOffset.UtcNow,
                                });
                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored Stake action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is ClaimStakeReward claimStakeReward)
                            {
                                var start = DateTimeOffset.UtcNow;
                                var plainValue = (Bencodex.Types.Dictionary)claimStakeReward.PlainValue;
                                var avatarAddress = plainValue[AvatarAddressKey].ToAddress();
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;

                                var id = claimStakeReward.Id;
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

                                _claimStakeAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _claimStakeAvatarList.Add(new AvatarModel()
                                {
                                    Address = avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
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
                                    TimeStamp = DateTimeOffset.UtcNow,
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
                                _mmcAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _mmcList.Add(new MigrateMonsterCollectionModel()
                                {
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    MigrationAmount = Convert.ToDecimal(migrationAmount.GetQuantityString()),
                                    MigrationStartBlockIndex = migrationStartBlockIndex,
                                    StakeStartBlockIndex = stakeStartBlockIndex,
                                    TimeStamp = DateTimeOffset.UtcNow,
                                });
                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored MigrateMonsterCollection action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is Grinding grind)
                            {
                                var start = DateTimeOffset.UtcNow;

                                AvatarState prevAvatarState = ev.PreviousStates.GetAvatarStateV2(grind.AvatarAddress);
                                AgentState agentState = ev.PreviousStates.GetAgentState(ev.Signer);
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(grind.AvatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
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
                                    _grindAgentList.Add(new AgentModel()
                                    {
                                        Address = ev.Signer.ToString(),
                                    });
                                    _grindAvatarList.Add(new AvatarModel()
                                    {
                                        Address = grind.AvatarAddress.ToString(),
                                        AgentAddress = ev.Signer.ToString(),
                                        Name = avatarName,
                                        AvatarLevel = avatarLevel,
                                        TitleId = avatarTitleId,
                                        ArmorId = avatarArmorId,
                                        Cp = avatarCp,
                                    });
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
                                        TimeStamp = DateTimeOffset.UtcNow,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored Grinding action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is UnlockEquipmentRecipe unlockEquipmentRecipe)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(unlockEquipmentRecipe.AvatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                _unlockEquipmentRecipeAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _unlockEquipmentRecipeAvatarList.Add(new AvatarModel()
                                {
                                    Address = unlockEquipmentRecipe.AvatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
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
                                        TimeStamp = DateTimeOffset.UtcNow,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored UnlockEquipmentRecipe action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is UnlockWorld unlockWorld)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(unlockWorld.AvatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                _unlockWorldAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _unlockWorldAvatarList.Add(new AvatarModel()
                                {
                                    Address = unlockWorld.AvatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
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
                                        TimeStamp = DateTimeOffset.UtcNow,
                                    });
                                }

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored UnlockWorld action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is HackAndSlashRandomBuff hasRandomBuff)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(hasRandomBuff.AvatarAddress);
                                var previousStates = ev.PreviousStates;
                                AvatarState prevAvatarState = previousStates.GetAvatarStateV2(hasRandomBuff.AvatarAddress);
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                prevAvatarState.worldInformation.TryGetLastClearedStageId(out var currentStageId);
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                _hasRandomBuffAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _hasRandomBuffAvatarList.Add(new AvatarModel()
                                {
                                    Address = hasRandomBuff.AvatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
                                _hasRandomBuffList.Add(new HasRandomBuffModel()
                                {
                                    Id = hasRandomBuff.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = hasRandomBuff.AvatarAddress.ToString(),
                                    HasStageId = currentStageId,
                                    GachaCount = !hasRandomBuff.AdvancedGacha ? 5 : 10,
                                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                    TimeStamp = DateTimeOffset.UtcNow,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored HasRandomBuff action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is JoinArena joinArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(joinArena.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                Currency crystalCurrency = CrystalCalculator.CRYSTAL;
                                var prevCrystalBalance = previousStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var outputCrystalBalance = ev.OutputStates.GetBalance(
                                    ev.Signer,
                                    crystalCurrency);
                                var burntCrystal = prevCrystalBalance - outputCrystalBalance;
                                _joinArenaAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _joinArenaAvatarList.Add(new AvatarModel()
                                {
                                    Address = joinArena.avatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
                                _joinArenaList.Add(new JoinArenaModel()
                                {
                                    Id = joinArena.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = joinArena.avatarAddress.ToString(),
                                    AvatarLevel = avatarLevel,
                                    ArenaRound = joinArena.round,
                                    ChampionshipId = joinArena.championshipId,
                                    BurntCrystal = Convert.ToDecimal(burntCrystal.GetQuantityString()),
                                    TimeStamp = DateTimeOffset.UtcNow,
                                });

                                var end = DateTimeOffset.UtcNow;
                                Log.Debug("Stored JoinArena action in block #{index}. Time Taken: {time} ms.", ev.BlockIndex, (end - start).Milliseconds);
                            }

                            if (ev.Action is BattleArena battleArena)
                            {
                                var start = DateTimeOffset.UtcNow;
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(battleArena.myAvatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
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

                                _battleArenaAgentList.Add(new AgentModel()
                                {
                                    Address = ev.Signer.ToString(),
                                });
                                _battleArenaAvatarList.Add(new AvatarModel()
                                {
                                    Address = battleArena.myAvatarAddress.ToString(),
                                    AgentAddress = ev.Signer.ToString(),
                                    Name = avatarName,
                                    AvatarLevel = avatarLevel,
                                    TitleId = avatarTitleId,
                                    ArmorId = avatarArmorId,
                                    Cp = avatarCp,
                                });
                                _battleArenaList.Add(new BattleArenaModel()
                                {
                                    Id = battleArena.Id.ToString(),
                                    BlockIndex = ev.BlockIndex,
                                    AgentAddress = ev.Signer.ToString(),
                                    AvatarAddress = battleArena.myAvatarAddress.ToString(),
                                    AvatarLevel = avatarLevel,
                                    EnemyAvatarAddress = battleArena.enemyAvatarAddress.ToString(),
                                    ChampionshipId = battleArena.championshipId,
                                    Round = battleArena.round,
                                    TicketCount = ticketCount,
                                    BurntNCG = Convert.ToDecimal(burntNCG.GetQuantityString()),
                                    Victory = currentArenaScore.Score > previousArenaScore.Score,
                                    MedalCount = medalCount,
                                    TimeStamp = DateTimeOffset.UtcNow,
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
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(combinationEquipment.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    combinationEquipment.avatarAddress,
                                    combinationEquipment.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        combinationEquipment.avatarAddress,
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp,
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
                                AvatarState avatarState = ev.OutputStates.GetAvatarStateV2(itemEnhancement.avatarAddress);
                                var previousStates = ev.PreviousStates;
                                var characterSheet = previousStates.GetSheet<CharacterSheet>();
                                var avatarLevel = avatarState.level;
                                var avatarArmorId = avatarState.GetArmorId();
                                var avatarTitleCostume = avatarState.inventory.Costumes.FirstOrDefault(costume => costume.ItemSubType == ItemSubType.Title && costume.equipped);
                                int? avatarTitleId = null;
                                if (avatarTitleCostume != null)
                                {
                                    avatarTitleId = avatarTitleCostume.Id;
                                }

                                var avatarCp = CPHelper.GetCP(avatarState, characterSheet);
                                string avatarName = avatarState.name;
                                var slotState = ev.OutputStates.GetCombinationSlotState(
                                    itemEnhancement.avatarAddress,
                                    itemEnhancement.slotIndex);

                                if (slotState?.Result.itemUsable.ItemType is ItemType.Equipment)
                                {
                                    ProcessEquipmentData(
                                        ev.Signer,
                                        itemEnhancement.avatarAddress,
                                        avatarName,
                                        avatarLevel,
                                        avatarTitleId,
                                        avatarArmorId,
                                        avatarCp,
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
                                                avatarName,
                                                avatarLevel,
                                                avatarTitleId,
                                                avatarArmorId,
                                                avatarCp,
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
            string avatarName,
            int avatarLevel,
            int? avatarTitleId,
            int avatarArmorId,
            int avatarCp,
            Equipment equipment)
        {
            var cp = CPHelper.GetCP(equipment);
            _eqAgentList.Add(new AgentModel()
            {
                Address = agentAddress.ToString(),
            });
            _eqAvatarList.Add(new AvatarModel()
            {
                Address = avatarAddress.ToString(),
                AgentAddress = agentAddress.ToString(),
                Name = avatarName,
                AvatarLevel = avatarLevel,
                TitleId = avatarTitleId,
                ArmorId = avatarArmorId,
                Cp = avatarCp,
            });
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
    }
}
