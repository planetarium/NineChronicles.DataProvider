namespace NineChronicles.DataProvider.Queries
{
    using GraphQL;
    using GraphQL.Types;
    using Libplanet;
    using NineChronicles.DataProvider.GraphTypes;
    using NineChronicles.DataProvider.Store;

    internal class ShopQuery : ObjectGraphType
    {
        public ShopQuery(MySqlStore store)
        {
            Store = store;
            Field<IntGraphType>(
                name: "ShopEquipmentCount",
                resolve: context =>
                {
                    var shopEquipmentCount = Store.GetShopEquipmentCount();
                    return shopEquipmentCount;
                });
            Field<ListGraphType<ShopEquipmentType>>(
                name: "ShopEquipments",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopEquipments(sellerAvatarAddress);
                });
            Field<IntGraphType>(
                name: "ShopConsumableCount",
                resolve: context =>
                {
                    var shopConsumableCount = Store.GetShopConsumableCount();
                    return shopConsumableCount;
                });
            Field<ListGraphType<ShopConsumableType>>(
                name: "ShopConsumables",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopConsumables(sellerAvatarAddress);
                });
            Field<IntGraphType>(
                name: "ShopCostumeCount",
                resolve: context =>
                {
                    var shopCostumeCount = Store.GetShopCostumeCount();
                    return shopCostumeCount;
                });
            Field<ListGraphType<ShopCostumeType>>(
                name: "ShopCostumes",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" },
                    new QueryArgument<StringGraphType> { Name = "itemSubType" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    string? itemSubType = context.GetArgument<string?>("itemSubType", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopCostumes(sellerAvatarAddress, itemSubType);
                });
            Field<IntGraphType>(
                name: "ShopMaterialCount",
                resolve: context =>
                {
                    var shopMaterialCount = Store.GetShopMaterialCount();
                    return shopMaterialCount;
                });
            Field<ListGraphType<ShopMaterialType>>(
                name: "ShopMaterials",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "sellerAvatarAddress" }
                ),
                resolve: context =>
                {
                    string? address = context.GetArgument<string?>("sellerAvatarAddress", null);
                    Address? sellerAvatarAddress = address == null
                        ? (Address?)null
                        : new Address(address.Replace("0x", string.Empty));
                    return Store.GetShopMaterials(sellerAvatarAddress);
                });
        }

        private MySqlStore Store { get; }
    }
}
