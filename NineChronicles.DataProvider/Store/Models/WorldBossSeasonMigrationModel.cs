namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(RaidId), IsUnique = true)]
    public class WorldBossSeasonMigrationModel
    {
        [Key]
        [Required]
        public int RaidId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset MigratedAt { get; set; }
    }
}
