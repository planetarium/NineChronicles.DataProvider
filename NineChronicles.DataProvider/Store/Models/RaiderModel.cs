namespace NineChronicles.DataProvider.Store.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(RaidId), nameof(Address), IsUnique = true)]
    public class RaiderModel
    {
        public RaiderModel(
            int raidId,
            string avatarName,
            int highScore,
            int totalScore,
            int cp,
            int iconId,
            int level,
            string address,
            int purchaseCount)
        {
            RaidId = raidId;
            AvatarName = avatarName;
            HighScore = highScore;
            TotalScore = totalScore;
            Cp = cp;
            IconId = iconId;
            Level = level;
            Address = address;
            PurchaseCount = purchaseCount;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public int RaidId { get; set; }

        [Required]
        public string AvatarName { get; set; }

        [Required]
        public int HighScore { get; set; }

        [Required]
        public int TotalScore { get; set; }

        [Required]
        public int Cp { get; set; }

        [Required]
        public int Level { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public int IconId { get; set; }

        [Required]
        public int PurchaseCount { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
