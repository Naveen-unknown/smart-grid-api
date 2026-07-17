using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGridAPI.Models
{
    public class MaintenanceTicket
    {
        [Key]
        public int TicketId { get; set; }
        
        [Required]
        public int FaultId { get; set; }
        
        [ForeignKey("FaultId")]
        public Fault? Fault { get; set; }
        
        [Required]
        public int TeamId { get; set; }
        
        [ForeignKey("TeamId")]
        public MaintenanceTeam? Team { get; set; }
        
        public string Status { get; set; } = "Assigned"; // Assigned, En Route, Repairing, Completed
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }
        public string? AcceptedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public string? ProofPhotoUrl { get; set; }
    }
}
