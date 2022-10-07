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
using NineChronicles.DataProvider.Store.Models;
using Xunit;

namespace NineChronicles.DataProvider.Tests;

public class UpdateRaiderQueryTest : TestBase, IDisposable
{
    private readonly List<Address> _addresses = new List<Address>();
    private const int FixedTotalScore = 100;
    private const int FakeCount = 10;
    private const int RealCount = 20;
    
    [Fact]
    public async Task Test()
    {
        _addresses.Clear();
        for (var i = 0; i < RealCount; i++)
        {
            _addresses.Add(new PrivateKey().ToAddress());
        }
        const string query = @"query {
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

        await Context.SaveChangesAsync();
        
        Assert.Equal(Context.Raiders.Count(), FakeCount);
        var result = await ExecuteAsync(query);
        var value = (bool)((Dictionary<string, object>) ((ExecutionNode) result.Data).ToValue())["updateRaiders"];
        Assert.Equal(true, value);
        Assert.Equal(RealCount, Context.Raiders.Count());
        Assert.Equal(RealCount * FixedTotalScore, Context.Raiders.Sum(x=> x.TotalScore));
    }
    protected override IValue? GetStateMock(Address address)
    {
        if (address.Equals(Addresses.GetRaiderListAddress(1)))
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
        CleanUp();
    }
}
