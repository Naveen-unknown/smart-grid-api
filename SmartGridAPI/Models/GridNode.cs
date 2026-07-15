using System.ComponentModel.DataAnnotations;

namespace SmartGridAPI.Models
{
    public class GridNode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string NodeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Maintenance

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(50)]
        public string NodeType { get; set; } = "Substation"; // Substation, Distribution, Feeder

        public decimal? MaxCapacity { get; set; } // kW

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedAt { get; set; }

        // Navigation properties
        public ICollection<EnergyReading> EnergyReadings { get; set; } = new List<EnergyReading>();
        public ICollection<Fault> Faults { get; set; } = new List<Fault>();
        public ICollection<Outage> Outages { get; set; } = new List<Outage>();
    }
}
