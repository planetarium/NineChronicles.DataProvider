namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Libplanet.Crypto;
    using Microsoft.Extensions.Hosting;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless.GraphTypes.States;
    using Serilog;

    public class MocaWorker : BackgroundService
    {
        private readonly StateContext _stateContext;
        private readonly MySqlStore _mySqlStore;

        public MocaWorker(StateContext stateContext, MySqlStore mySqlStore)
        {
            _stateContext = stateContext;
            _mySqlStore = mySqlStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Debug();
        }

        private async Task Migrate(CancellationToken stoppingToken)
        {
            var offset = 0;
            var previousOffset = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var blockIndex = _mySqlStore.GetTip();
                    var mocas = _mySqlStore.GetMocas(offset);
                    var collectionSheet = _stateContext.WorldState.GetSheet<CollectionSheet>();
                    foreach (var moca in mocas)
                    {
                        var avatars = _mySqlStore.GetAvatarsFromSigner(moca.Signer);
                        var migrated = true;
                        foreach (var avatar in avatars)
                        {
                            try
                            {
                                var collectionState = _stateContext.WorldState.GetCollectionState(new Address(avatar.Address!));
                                var existIds = avatar.ActivateCollections.Select(i => i.CollectionId);
                                var targetIds = collectionState.Ids.Except(existIds);
                                foreach (var collectionId in targetIds)
                                {
                                    var row = collectionSheet[collectionId];
                                    var options = new List<CollectionOptionModel>();
                                    foreach (var modifier in row.StatModifiers)
                                    {
                                        var option = new CollectionOptionModel
                                        {
                                            StatType = modifier.StatType.ToString(),
                                            OperationType = modifier.Operation.ToString(),
                                            Value = modifier.Value,
                                        };
                                        options.Add(option);
                                    }

                                    var collectionModel = new ActivateCollectionModel
                                    {
                                        ActionId = "Migrate from worker",
                                        Avatar = avatar,
                                        BlockIndex = blockIndex,
                                        CollectionId = collectionId,
                                        Options = options,
                                    };
                                    avatar.ActivateCollections.Add(collectionModel);
                                }

                                _mySqlStore.UpdateAvatar(avatar);
                            }
                            catch (Exception e)
                            {
                                migrated = false;
                                Log.Error(e, "Unexpected exception occurred during MocaWorker: {Exc}", e);
                            }
                        }

                        if (migrated)
                        {
                            moca.Migrated = true;
                            _mySqlStore.UpdateMoca(moca.Signer);
                        }
                    }

                    previousOffset = offset;
                    offset += mocas.Count;
                    Log.Information("[MocaWorker]{OffSet} migration completed", offset + 100);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unexpected exception occurred during MocaWorker: {Exc}", e);
                }

                if (offset == previousOffset)
                {
                    break;
                }

                await Task.Delay(100, stoppingToken);
            }
        }

        private async Task Debug()
        {
            var avatarAddress = Environment.GetEnvironmentVariable("NC_AvatarAddress")!;
            var agentAddress = Environment.GetEnvironmentVariable("NC_AgentAddress")!;
            var moca = _mySqlStore.GetMoca(agentAddress);
            Log.Information("[MocaWorker] integration: {Signer}, {Migrated}", moca.Signer, moca.Migrated);
            var avatar = _mySqlStore.GetAvatar(new Address(avatarAddress), true);
            Log.Information("[MocaWorker] avatar: {Signer}, {Address}, {Collections}", avatar.AgentAddress, avatar.Address, avatar.ActivateCollections.Count);
            foreach (var collection in avatar.ActivateCollections)
            {
                Log.Information("[MocaWorker] activateCollection: {Address}, {BlockIndex}, {CollectionId}", collection.AvatarAddress, collection.BlockIndex, collection.CollectionId);
            }

            var collectionSheet = _stateContext.WorldState.GetSheet<CollectionSheet>();
            var collectionState = _stateContext.WorldState.GetCollectionState(new Address(avatar.Address!));
            var existIds = avatar.ActivateCollections.Select(i => i.Id);
            var targetIds = collectionState.Ids.Except(existIds).ToList();
            var blockIndex = _mySqlStore.GetTip();
            foreach (var targetId in targetIds)
            {
                Log.Information("[MocaWorker] activateCollection new id: {Id}", targetId);
                try
                {
                    foreach (var collectionId in targetIds)
                    {
                        var row = collectionSheet[collectionId];
                        var options = new List<CollectionOptionModel>();
                        foreach (var modifier in row.StatModifiers)
                        {
                            var option = new CollectionOptionModel
                            {
                                StatType = modifier.StatType.ToString(),
                                OperationType = modifier.Operation.ToString(),
                                Value = modifier.Value,
                            };
                            options.Add(option);
                        }

                        var collectionModel = new ActivateCollectionModel
                        {
                            ActionId = "Migrate from worker",
                            Avatar = avatar,
                            BlockIndex = blockIndex,
                            CollectionId = collectionId,
                            Options = options,
                        };
                        avatar.ActivateCollections.Add(collectionModel);
                    }

                    // 업데이트 대상이 있을때만 UPDATE 처리
                    if (targetIds.Any())
                    {
                        _mySqlStore.UpdateAvatar(avatar);
                    }

                    moca.Migrated = true;
                    _mySqlStore.UpdateMoca(moca.Signer);
                }
                catch (Exception e)
                {
                    Log.Error(e, "[MocaWorker] Unexpected exception occurred during MocaWorker: {Exc}", e);
                }
            }

            await Task.CompletedTask;
        }
    }
}
