namespace NineChronicles.DataProvider.Store.Models
{
    using System.ComponentModel.DataAnnotations;

    public class CollectionOptionModel
    {
        [Key]
        public int Id { get; set; }

        public string StatType { get; set; } = null!;

        public string OperationType { get; set; } = null!;

        public long Value { get; set; }
    }
}
