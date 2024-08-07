namespace NineChronicles.DataProvider.GraphTypes
{
    using GraphQL.Types;
    using NineChronicles.DataProvider.Store.Models.CustomCraft;

    public class CustomEquipmentCraftIconCountType : ObjectGraphType<CustomEquipmentCraftCountModel>
    {
        public CustomEquipmentCraftIconCountType()
        {
            Name = "CustomEquipmentCraftIconCount";
            Field(x => x.ItemSubType);
            Field(x => x.IconId);
            Field(x => x.Count);
        }
    }
}
