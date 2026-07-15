using Microsoft.EntityFrameworkCore;
using SmartGridAPI.Models;

namespace SmartGridAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<GridNode> GridNodes { get; set; }
        public DbSet<EnergyReading> EnergyReadings { get; set; }
        public DbSet<Fault> Faults { get; set; }
        public DbSet<Outage> Outages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<MaintenanceTeam> MaintenanceTeams { get; set; }
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }
        public DbSet<MaintenanceTeamMember> MaintenanceTeamMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<EnergyReading>()
                .HasOne(e => e.Node)
                .WithMany(n => n.EnergyReadings)
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EnergyReading>()
                .HasOne(e => e.User)
                .WithMany(u => u.EnergyReadings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fault>()
                .HasOne(f => f.Node)
                .WithMany(n => n.Faults)
                .HasForeignKey(f => f.NodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fault>()
                .HasOne(f => f.ReportedBy)
                .WithMany(u => u.FaultReports)
                .HasForeignKey(f => f.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Outage>()
                .HasOne(o => o.Node)
                .WithMany(n => n.Outages)
                .HasForeignKey(o => o.NodeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add indexes
            modelBuilder.Entity<EnergyReading>()
                .HasIndex(e => new { e.NodeId, e.Timestamp });

            modelBuilder.Entity<Fault>()
                .HasIndex(f => new { f.NodeId, f.Status });

            modelBuilder.Entity<Outage>()
                .HasIndex(o => new { o.NodeId, o.Status });

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<MaintenanceTicket>()
                .HasOne(t => t.Fault)
                .WithMany()
                .HasForeignKey(t => t.FaultId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MaintenanceTicket>()
                .HasOne(t => t.Team)
                .WithMany()
                .HasForeignKey(t => t.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Maintenance Teams
            modelBuilder.Entity<MaintenanceTeam>().HasData(
                new MaintenanceTeam { TeamId = 1, TeamName = "Team A", Latitude = 11.1271, Longitude = 78.6569, Phone = "9876543210", Status = "Available" },
                new MaintenanceTeam { TeamId = 2, TeamName = "Team B", Latitude = 11.3500, Longitude = 78.9000, Phone = "9876543211", Status = "Busy" },
                new MaintenanceTeam { TeamId = 3, TeamName = "Team C", Latitude = 11.2000, Longitude = 78.7500, Phone = "9876543212", Status = "Available" }
            );

            // Seed Maintenance Team Members
            modelBuilder.Entity<MaintenanceTeamMember>().HasData(
                new MaintenanceTeamMember { MemberId = 1, TeamId = 1, Name = "Lead Engineer", Role = "Senior Engineer", PhoneNumber = "+919344255537" },
                new MaintenanceTeamMember { MemberId = 2, TeamId = 2, Name = "Technician John", Role = "Technician", PhoneNumber = "+919876543211" }
            );

            // Seed dummy faults
            modelBuilder.Entity<Fault>().HasData(
                new Fault { Id = 101, NodeId = 1, Description = "Transformer Oil Leak", Severity = "Critical", Status = "Reported", ReportedAt = DateTime.UtcNow.AddHours(-2), ReportedByUserId = 1 },
                new Fault { Id = 102, NodeId = 2, Description = "Voltage Sag", Severity = "Medium", Status = "InProgress", ReportedAt = DateTime.UtcNow.AddHours(-5), ReportedByUserId = 1 },
                new Fault { Id = 103, NodeId = 3, Description = "Phase Imbalance", Severity = "High", Status = "Reported", ReportedAt = DateTime.UtcNow.AddHours(-1), ReportedByUserId = 1 },
                new Fault { Id = 104, NodeId = 4, Description = "Broken Insulator", Severity = "Low", Status = "Resolved", ReportedAt = DateTime.UtcNow.AddDays(-1), ResolvedAt = DateTime.UtcNow.AddHours(-12), ReportedByUserId = 1 }
            );

            // Seed dummy outages
            modelBuilder.Entity<Outage>().HasData(
                new Outage { Id = 101, NodeId = 2, AffectedCustomers = 450, StartedAt = DateTime.UtcNow.AddHours(-4), Status = "Ongoing" },
                new Outage { Id = 102, NodeId = 5, AffectedCustomers = 1200, StartedAt = DateTime.UtcNow.AddDays(-1), RestoredAt = DateTime.UtcNow.AddHours(-10), Status = "Restored" }
            );

            // Seed dummy energy readings
            modelBuilder.Entity<EnergyReading>().HasData(
                new EnergyReading { Id = 101, NodeId = 1, UserId = 1, Voltage = 220m, Current = 15m, PowerFactor = 0.95m, Consumption = 120m, Production = 0m, Timestamp = DateTime.UtcNow.AddHours(-1) },
                new EnergyReading { Id = 102, NodeId = 2, UserId = 1, Voltage = 215m, Current = 20m, PowerFactor = 0.92m, Consumption = 150m, Production = 50m, Timestamp = DateTime.UtcNow.AddHours(-2) },
                new EnergyReading { Id = 103, NodeId = 3, UserId = 1, Voltage = 230m, Current = 10m, PowerFactor = 0.98m, Consumption = 80m, Production = 0m, Timestamp = DateTime.UtcNow.AddHours(-3) },
                new EnergyReading { Id = 104, NodeId = 1, UserId = 1, Voltage = 225m, Current = 16m, PowerFactor = 0.94m, Consumption = 125m, Production = 0m, Timestamp = DateTime.UtcNow.AddDays(-1) },
                new EnergyReading { Id = 105, NodeId = 2, UserId = 1, Voltage = 210m, Current = 22m, PowerFactor = 0.90m, Consumption = 160m, Production = 40m, Timestamp = DateTime.UtcNow.AddDays(-1) },
                new EnergyReading { Id = 106, NodeId = 3, UserId = 1, Voltage = 235m, Current = 9m, PowerFactor = 0.99m, Consumption = 75m, Production = 0m, Timestamp = DateTime.UtcNow.AddDays(-2) },
                new EnergyReading { Id = 107, NodeId = 1, UserId = 1, Voltage = 218m, Current = 18m, PowerFactor = 0.93m, Consumption = 135m, Production = 0m, Timestamp = DateTime.UtcNow.AddDays(-3) }
            );

            // Seed grid nodes
            modelBuilder.Entity<GridNode>().HasData(
                new GridNode
                {
                    Id = 1, NodeId = "NODE-001", Location = "Downtown Substation",
                    Status = "Active", Latitude = 40.7128, Longitude = -74.0060,
                    NodeType = "Substation", MaxCapacity = 5000,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new GridNode
                {
                    Id = 2, NodeId = "NODE-002", Location = "Industrial Zone",
                    Status = "Active", Latitude = 40.7580, Longitude = -73.9855,
                    NodeType = "Distribution", MaxCapacity = 3000,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new GridNode
                {
                    Id = 3, NodeId = "NODE-003", Location = "Residential Area North",
                    Status = "Active", Latitude = 40.7831, Longitude = -73.9712,
                    NodeType = "Feeder", MaxCapacity = 1500,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new GridNode
                {
                    Id = 4, NodeId = "NODE-004", Location = "Commercial District",
                    Status = "Active", Latitude = 40.7484, Longitude = -73.9967,
                    NodeType = "Distribution", MaxCapacity = 2500,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new GridNode
                {
                    Id = 5, NodeId = "NODE-005", Location = "Eastern Substation",
                    Status = "Maintenance", Latitude = 40.7282, Longitude = -73.7949,
                    NodeType = "Substation", MaxCapacity = 4000,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Seed admin user (password: Admin@123)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@smartgrid.com",
                    PasswordHash = "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 2,
                    Username = "electricity_officer",
                    Email = "officer@smartgrid.com",
                    PasswordHash = "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm",
                    Role = "Electricity Officer",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 3,
                    Username = "maintenance_team",
                    Email = "team@smartgrid.com",
                    PasswordHash = "$2a$11$t3KQzl4lh40owPIXHkxXCOJpfYSMIvH/YF6CygLub4220I5GMGXnm", // Password: Admin@123
                    Role = "Maintenance Team",
                    IsActive = true,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
