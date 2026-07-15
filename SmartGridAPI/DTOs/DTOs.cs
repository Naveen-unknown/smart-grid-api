using System.ComponentModel.DataAnnotations;

namespace SmartGridAPI.DTOs
{
    public class LoginDTO
    {
        [Required]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDTO
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthResponseDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class EnergyReadingDTO
    {
        [Required]
        public int NodeId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Consumption { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Production { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Voltage { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Current { get; set; }

        [Range(0, 1)]
        public decimal PowerFactor { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Frequency { get; set; } = 50;

        public string? MeterId { get; set; }
    }

    public class FaultReportDTO
    {
        [Required]
        public int NodeId { get; set; }

        [Required]
        public int ReportedByUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FaultType { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Severity { get; set; } = "Medium";

        public string? AssignedTo { get; set; }
    }

    public class OutageReportDTO
    {
        [Required]
        public int NodeId { get; set; }

        [Required]
        [MaxLength(200)]
        public string AffectedArea { get; set; } = string.Empty;

        public int? AffectedCustomers { get; set; }

        [MaxLength(500)]
        public string? Cause { get; set; }

        [MaxLength(20)]
        public string OutageType { get; set; } = "Unplanned";

        public int? ReportedByUserId { get; set; }
        public DateTime? EstimatedRestorationTime { get; set; }
    }

    public class GridNodeDTO
    {
        [Required]
        [MaxLength(50)]
        public string NodeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(50)]
        public string NodeType { get; set; } = "Substation";

        public decimal? MaxCapacity { get; set; }
    }
}
