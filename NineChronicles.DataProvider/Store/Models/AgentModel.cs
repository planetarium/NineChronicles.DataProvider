namespace NineChronicles.DataProvider.Store.Models
{
    using System.ComponentModel.DataAnnotations;

    public class AgentModel
    {
        [Key]
        public string? Address { get; set; }
    }
}
