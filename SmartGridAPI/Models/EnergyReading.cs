using System.ComponentModel.DataAnnotations;

namespace SmartGridAPI.Models
{
    public class EnergyReading
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NodeId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Consumption { get; set; } // kWh

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Production { get; set; } // kWh

        [Range(0, double.MaxValue)]
        public decimal Voltage { get; set; } // Volts

        [Range(0, double.MaxValue)]
        public decimal Current { get; set; } // Amperes

        [Range(0, 1)]
        public decimal PowerFactor { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Frequency { get; set; } = 50; // Hz

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? MeterId { get; set; }

        // Navigation properties
        public GridNode? Node { get; set; }
        public User? User { get; set; }
    }
}
