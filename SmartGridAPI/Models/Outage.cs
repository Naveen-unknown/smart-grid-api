using System.ComponentModel.DataAnnotations;

namespace SmartGridAPI.Models
{
    public class Outage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NodeId { get; set; }

        [Required]
        [MaxLength(200)]
        public string AffectedArea { get; set; } = string.Empty;

        public int? AffectedCustomers { get; set; }

        [Required]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RestoredAt { get; set; }
        public DateTime? EstimatedRestorationTime { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Ongoing"; // Ongoing, Restored, UnderInvestigation, Planned

        [MaxLength(20)]
        public string OutageType { get; set; } = "Unplanned"; // Planned, Unplanned

        [MaxLength(500)]
        public string? Cause { get; set; }

        [MaxLength(500)]
        public string? ActionTaken { get; set; }

        // AI Analysis
        public string? AIAnalysis { get; set; }

        public int? ReportedByUserId { get; set; }

        // Navigation properties
        public GridNode? Node { get; set; }
    }
}
