namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Microsoft.Extensions.Hosting;
    using Nekoyume;
    using Nekoyume.Extensions;
    using Nekoyume.Model.State;
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
            Log.Information("Start MocaWorker");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var blockIndex = _mySqlStore.GetTip();
                    var mocas = _mySqlStore.GetMocas();
                    var collectionSheet = _stateContext.WorldState.GetSheet<CollectionSheet>();
                    foreach (var moca in mocas)
                    {
                        var avatars = _mySqlStore.GetAvatarsFromSigner(moca.Signer);
                        foreach (var avatar in avatars)
                        {
                            var collectionState = _stateContext.WorldState.GetCollectionState(new Address(avatar.Address!));
                            var existIds = avatar.ActivateCollections.Select(i => i.Id);
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

                        moca.Migrated = true;
                        _mySqlStore.UpdateMoca(moca.Signer);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unexpected exception occurred during MocaWorker: {Exc}", e);
                }

                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
