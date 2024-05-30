using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Net.Consensus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nekoyume;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChronicles.DataProvider.Store;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class MocaWorkerTest : TestBase
{
    private static readonly Address AvatarAddress = new PrivateKey().Address;
    private readonly CancellationTokenSource _cts;

    public MocaWorkerTest()
    {
        _cts = new CancellationTokenSource();
    }

    [Fact]
    public async Task Moca()
    {
        Services.AddSingleton<MocaWorker>();
        var provider = Services.BuildServiceProvider();
        var worker = provider.GetRequiredService<MocaWorker>();
        var store = provider.GetRequiredService<MySqlStore>();
        var block = new BlockModel
        {
            Index = 1L,
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
        var agentAddress = new Address().ToString();
        var am = new AgentModel
        {
            Address = agentAddress,
        };
        Context.Avatars.Add(new AvatarModel
        {
            Agent = am,
            Address = AvatarAddress.ToString(),
            Timestamp = DateTimeOffset.Now
        });
        await Context.SaveChangesAsync(_cts.Token);
        var avatar = await Context.Avatars.FirstAsync(_cts.Token);
        Assert.Equal(avatar.AgentAddress, agentAddress);
        await Context.Database.BeginTransactionAsync(_cts.Token);
        await Context.Database.ExecuteSqlRawAsync(
            $"INSERT INTO MocaIntegrations(MocaIntegration, Signer, Migrated) VALUES (1, \"{agentAddress}\", {false})");
        await Context.Database.CommitTransactionAsync(_cts.Token);
        var moca = Assert.Single(Context.MocaIntegrations);
        Assert.Equal(moca.Signer, avatar.AgentAddress);
        await worker.StartAsync(_cts.Token);
        _cts.Cancel();
        Assert.Single(Context.MocaIntegrations.Where(i => i.Migrated));
        Assert.Empty(Context.MocaIntegrations.Where(i => !i.Migrated));
        Assert.Single(Context.ActivateCollections);
    }

    protected override IWorldState GetMockState()
    {
        var collectionState = new CollectionState();
        collectionState.Ids.Add(1);
        var mockWorldState = MockWorldState.CreateModern();
        mockWorldState = mockWorldState.SetAccount(ReservedAddresses.LegacyAccount,
            new Account(mockWorldState.GetAccountState(ReservedAddresses.LegacyAccount))
            .SetState(
                Addresses.GetSheetAddress<CollectionSheet>(),
                @"id,item_id1,count1,level1,skill1,item_id2,count2,level2,skill2,item_id3,count3,level3,skill3,item_id4,count4,level4,skill4,item_id5,count5,level5,skill5,item_id6,count6,level6,skill6,stat_type1,modify_type1,modify_value1,stat_type2,modify_type2,modify_value2,stat_type3,modify_type3,modify_value3
1,10114000,1,0,TRUE,10120000,1,0,TRUE,10124000,1,0,TRUE,,,,,,,,,,,,,ATK,Percentage,50,,,,,,
".Serialize())
        )
        .SetAccount(Addresses.Collection, new Account(mockWorldState.GetAccountState(Addresses.Collection)).SetState(AvatarAddress, collectionState.Bencoded));
        return mockWorldState;
    }
}
