namespace NineChronicles.DataProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Libplanet;
    using Microsoft.Extensions.Hosting;
    using Nekoyume;
    using Nekoyume.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using NineChronicles.DataProvider.Store;
    using NineChronicles.DataProvider.Store.Models;
    using NineChronicles.Headless.GraphTypes.States;
    using Serilog;

    public class RaiderWorker : BackgroundService
    {
        private readonly StateContext _stateContext;
        private readonly MySqlStore _mySqlStore;

        public RaiderWorker(StateContext stateContext, MySqlStore mySqlStore)
        {
            _stateContext = stateContext;
            _mySqlStore = mySqlStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Start RaiderWorker");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var blockIndex = _mySqlStore.GetTip();
                    var worldBossListSheetAddress = Addresses.GetSheetAddress<WorldBossListSheet>();
                    var value = _stateContext.GetState(worldBossListSheetAddress);
                    if (value is Text wbs)
                    {
                        var sheet = new WorldBossListSheet();
                        sheet.Set(wbs);
                        int raidId = sheet.FindPreviousRaidIdByBlockIndex(blockIndex);
                        var bossRow = sheet.OrderedList!.First(r => r.Id == raidId);
                        var exist = _mySqlStore.MigrationExists(raidId);
                        if (bossRow.EndedBlockIndex < blockIndex && !exist)
                        {
                            var raiderListAddress = Addresses.GetRaiderListAddress(raidId);
                            if (_stateContext.GetState(raiderListAddress) is List raiderList)
                            {
                                var raiderAddresses = raiderList.ToList(StateExtensions.ToAddress);
                                bool success = UpdateRaider(_stateContext, raiderAddresses, raidId);
                                if (success)
                                {
                                    Log.Information("Success Update raiders");
                                    _mySqlStore.StoreWorldBossMigration(raidId);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unexpected exception occurred during RaiderWorker: {Exc}", e);
                }

                await Task.Delay(TimeSpan.FromHours(8), stoppingToken);
            }
        }

        private bool UpdateRaider(StateContext stateContext, List<Address> raiderAddresses, int raidId)
        {
            try
            {
                var raiderList = new List<RaiderModel>();
                var result = stateContext.GetStates(raiderAddresses);
                foreach (var value in result)
                {
                    if (value is List list)
                    {
                        var raiderState = new RaiderState(list);
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
                        raiderList.Add(model);
                    }
                }

                _mySqlStore.UpsertRaiders(raiderList);
                return _mySqlStore.GetTotalRaiders(raidId) == raiderAddresses.Count;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception occurred during UpdateRaider: {Exc}", e);
                return false;
            }
        }
    }
}
