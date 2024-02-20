namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ActivateCollectionModel
    {
        [Key]
        public int Id { get; set; }

        public string AvatarAddress { get; set; } = null!;

        public AvatarModel Avatar { get; set; } = null!;

        public int CollectionId { get; set; }

        public string ActionId { get; set; } = null!;

        public long BlockIndex { get; set; }

        public ICollection<CollectionOptionModel> Options { get; set; } = null!;

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
