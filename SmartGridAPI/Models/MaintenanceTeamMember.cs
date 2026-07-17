using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGridAPI.Models
{
    public class MaintenanceTeamMember
    {
        [Key]
        public int MemberId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        public MaintenanceTeam? Team { get; set; }

        public int? UserId { get; set; }
    }
}
