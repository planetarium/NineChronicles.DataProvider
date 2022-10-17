using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Nekoyume;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChronicles.DataProvider.Store;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class RaiderWorkerTest : TestBase
{
    private CancellationTokenSource _cts;
    private Address _worldBossListSheetAddress;
    private readonly Address _avatarAddress;
    private readonly Address _raiderAddress;
    private readonly Address _raiderListAddress;
    private readonly RaiderState _raiderState;


    public RaiderWorkerTest()
    {
        _cts = new CancellationTokenSource();
        _worldBossListSheetAddress = Addresses.GetSheetAddress<WorldBossListSheet>();
        _avatarAddress = new PrivateKey().ToAddress();
        _raiderAddress = Addresses.GetRaiderAddress(_avatarAddress, 1);
        _raiderListAddress = Addresses.GetRaiderListAddress(1);
        _raiderState = new RaiderState
        {
            AvatarAddress = _avatarAddress
        };
    }
    [Theory]
    [InlineData(11L, true, false)]
    [InlineData(11L, true, true)]
    [InlineData(1L, false, false)]
    public async Task UpdateRaider(long blockIndex, bool success, bool updated)
    {
        Services.AddSingleton<RaiderWorker>();
        var provider = Services.BuildServiceProvider();
        var worker = provider.GetRequiredService<RaiderWorker>();
        var store = provider.GetRequiredService<MySqlStore>();
        var block = new BlockModel
        {
            Index = blockIndex,
            Hash = "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce",
            Miner = "47d082a115c63e7b58b1532d20e631538eafadde",
            Difficulty = 0L,
            Nonce = "dff109a0abf1762673ed",
            PreviousHash = "asd",
            ProtocolVersion = 1,
            PublicKey = ByteUtil.Hex(new PrivateKey().PublicKey.ToImmutableArray(false)),
            StateRootHash = "ce667fcd0b69076d9ff7e7755daa2f35cb0488e4c47978468dfbd6b88fca8a90",
            TotalDifficulty = 0L,
            TxCount = 1,
            TxHash = "fd47c10ffbee8ff2da8fa08cec3072de06a72f73693f5d3399b093b0877fa954",
            TimeStamp = DateTimeOffset.UtcNow
        };
        Context.Blocks.Add(block);
        var model = new RaiderModel(
            1,
            _raiderState.AvatarName,
            updated ? 100 : _raiderState.HighScore,
            _raiderState.TotalScore,
            _raiderState.Cp,
            _raiderState.IconId,
            _raiderState.Level,
            _raiderState.AvatarAddress.ToHex(),
            _raiderState.PurchaseCount
        );
        Context.Raiders.Add(model);
        Assert.Equal(model.CreatedAt, model.UpdatedAt);
        Assert.False(Context.WorldBossSeasonMigrationModels.Any());
        await Context.SaveChangesAsync(_cts.Token);        
        await worker.StartAsync(_cts.Token);
        _cts.Cancel();
        Assert.Equal(success, store.MigrationExists(1));
        if (success)
        {
            var updatedModel = store.GetRaiderList().Single();
            Assert.Equal(0, updatedModel.HighScore);
            // Check skip updated.
            Assert.Equal(updated, model.CreatedAt != updatedModel.UpdatedAt);
        }
    }
    protected override IValue? GetStateMock(Address address)
    {
        if (address.Equals(_raiderAddress))
        {
            return _raiderState.Serialize();
        }

        if (address.Equals(_raiderListAddress))
        {
            return new List(_raiderAddress.Serialize());
        }

        if (address.Equals(_worldBossListSheetAddress))
        {
            return @"id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count
1,900001,0,10,300,200,100,10".Serialize();
        }

        return null;
    }

    protected override FungibleAssetValue GetBalanceMock(Address address, Currency currency)
    {
        throw new System.NotImplementedException();
    }
}
