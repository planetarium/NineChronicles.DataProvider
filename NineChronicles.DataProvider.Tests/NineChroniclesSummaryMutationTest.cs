using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using GraphQL.Execution;
using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class NineChroniclesSummaryMutationTest : TestBase, IDisposable
{
    private readonly List<Address> _addresses = new List<Address>();
    private readonly string _csv = @"id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count
1,900001,0,10,300,200,100,10";
    private const int FixedTotalScore = 100;
    private const int FakeCount = 26;
    private const int RealCount = 27;

    [Fact]
    public async Task UpdateRaidersTest()
    {
        _addresses.Clear();
        for (var i = 0; i < RealCount; i++)
        {
            _addresses.Add(new PrivateKey().ToAddress());
        }
        const string query = @"mutation {
        updateRaiders(raidId: 1)
    }";
        for (var i = 0; i < FakeCount; i++)
        {
            var model = new RaiderModel(
                1,
                i.ToString(),
                i,
                1,
                i + 2,
                GameConfig.DefaultAvatarArmorId,
                i,
                _addresses[i].ToHex(),
                0
            );
            Context.Raiders.Add(model);
        }
        
        var block = new BlockModel
        {
            Index = 100,
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

        await Context.SaveChangesAsync();
        Assert.Equal(FakeCount, Context.Raiders.Count());
        var result = await ExecuteAsync(query);
        var value = (bool)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["updateRaiders"];
        Assert.Equal(true, value);
        Assert.Equal(RealCount, Context.Raiders.Count());
        Assert.Equal(RealCount * FixedTotalScore, Context.Raiders.Sum(x=> x.TotalScore));
    }
    protected override IValue? GetStateMock(Address address)
    {
        if (address.Equals(Addresses.GetSheetAddress<WorldBossListSheet>()))
        {
            return _csv.Serialize();
        }
        else if (address.Equals(Addresses.GetRaiderListAddress(1)))
        {
            return new List(_addresses.Select(x => x.Serialize()));
        }
        else
        {
            foreach (var add in _addresses)
            {
                var result = Addresses.GetRaiderAddress(add, 1);
                if (address == result)
                {
                    var state = new RaiderState
                    {
                        AvatarAddress = add,
                        TotalScore = FixedTotalScore
                    };
                    return state.Serialize();
                }
            }
        }

        return null;
    }

    protected override FungibleAssetValue GetBalanceMock(Address address, Currency currency)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // CleanUp();
    }
}
