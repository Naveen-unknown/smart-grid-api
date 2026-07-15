using System.ComponentModel.DataAnnotations;

namespace SmartGridAPI.Models
{
    public class Fault
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NodeId { get; set; }

        [Required]
        public int ReportedByUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FaultType { get; set; } = string.Empty; // Short Circuit, Overload, Ground Fault, Equipment Failure

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

        [MaxLength(20)]
        public string Status { get; set; } = "Reported"; // Reported, InProgress, Resolved, Closed

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }

        [MaxLength(100)]
        public string? AssignedTo { get; set; }

        [MaxLength(1000)]
        public string? ResolutionNotes { get; set; }

        // AI Analysis
        public string? AIPrediction { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public string? AIRecommendation { get; set; }

        // Navigation properties
        public GridNode? Node { get; set; }
        public User? ReportedBy { get; set; }
    }
}
