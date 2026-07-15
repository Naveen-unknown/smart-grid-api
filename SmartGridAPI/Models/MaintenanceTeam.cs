using System.ComponentModel.DataAnnotations;

namespace SmartGridAPI.Models
{
    public class MaintenanceTeam
    {
        [Key]
        public int TeamId { get; set; }
        
        [Required]
        public string TeamName { get; set; } = string.Empty;
        
        public double Latitude { get; set; }
        
        public double Longitude { get; set; }
        
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Busy

        public ICollection<MaintenanceTeamMember> Members { get; set; } = new List<MaintenanceTeamMember>();
    }
}
