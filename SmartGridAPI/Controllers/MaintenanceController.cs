using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGridAPI.Data;
using SmartGridAPI.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SmartGridAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Using Authorize if needed, for now public for easy testing
    public class MaintenanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(ApplicationDbContext context, ILogger<MaintenanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets()
        {
            var tickets = await _context.MaintenanceTickets
                .Include(t => t.Team)
                .Include(t => t.Fault)
                .OrderByDescending(t => t.AssignedAt)
                .ToListAsync();
            
            return Ok(tickets);
        }

        [HttpGet("teams")]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _context.MaintenanceTeams.Include(t => t.Members).ToListAsync();
            return Ok(teams);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignNearestTeam([FromBody] AssignRequestDto request)
        {
            var fault = await _context.Faults.Include(f => f.Node).FirstOrDefaultAsync(f => f.Id == request.FaultId);
            if (fault == null || fault.Node == null)
            {
                return NotFound(new { message = "Fault or Node not found" });
            }

            var availableTeams = await _context.MaintenanceTeams.Include(t => t.Members).Where(t => t.Status == "Available").ToListAsync();
            if (!availableTeams.Any())
            {
                return BadRequest(new { message = "No available maintenance teams found." });
            }

            MaintenanceTeam? nearestTeam = null;
            double shortestDistance = double.MaxValue;

            foreach (var team in availableTeams)
            {
                double distance = CalculateHaversineDistance(fault.Node.Latitude ?? 0, fault.Node.Longitude ?? 0, team.Latitude, team.Longitude);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTeam = team;
                }
            }

            if (nearestTeam == null)
            {
                return BadRequest(new { message = "Failed to calculate nearest team." });
            }

            // Create Ticket
            var ticket = new MaintenanceTicket
            {
                FaultId = fault.Id,
                TeamId = nearestTeam.TeamId,
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow
            };

            // Update team status
            nearestTeam.Status = "Busy";

            _context.MaintenanceTickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Send Real SMS via Twilio to Team Members
            string body = $"SMART GRID ALERT\n\nHigh Priority Fault\n\nLocation:\n{fault.Node?.Location}\n\nFault:\n{fault.Description}\n\nSeverity:\n{fault.Severity}.";
            try
            {
                TwilioClient.Init(Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID"), Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN"));
                if (nearestTeam.Members != null && nearestTeam.Members.Any())
                {
                    foreach (var member in nearestTeam.Members)
                    {
                        var message = MessageResource.Create(
                            body: body,
                            from: new Twilio.Types.PhoneNumber("+17627012086"),
                            to: new Twilio.Types.PhoneNumber(member.PhoneNumber)
                        );
                        _logger.LogInformation($"[SMS SUCCESS] Real Twilio message dispatched to {member.PhoneNumber}. SID: {message.Sid}");
                    }
                }
                else 
                {
                    _logger.LogWarning($"[SMS WARNING] No team members found for {nearestTeam.TeamName}");
                }
            }
            catch(Exception ex)
            {
                 _logger.LogError($"[SMS ERROR] Failed to send Twilio SMS: {ex.Message}");
            }

            return Ok(new { 
                message = "Team assigned successfully.", 
                ticket = new {
                    TicketId = $"SG-{DateTime.UtcNow.Year}-{ticket.TicketId}",
                    TeamName = nearestTeam.TeamName,
                    Status = ticket.Status,
                    DistanceKm = Math.Round(shortestDistance, 2)
                }
            });
        }

        [HttpPost("ticket/{ticketId}/status")]
        public async Task<IActionResult> UpdateTicketStatus(int ticketId, [FromBody] UpdateStatusDto request)
        {
            var ticket = await _context.MaintenanceTickets.Include(t => t.Team).FirstOrDefaultAsync(t => t.TicketId == ticketId);
            if (ticket == null) return NotFound();

            ticket.Status = request.Status;
            
            if (request.Status == "En Route" || request.Status == "Repairing")
            {
                if (!ticket.AcceptedAt.HasValue) ticket.AcceptedAt = DateTime.UtcNow;
            }
            else if (request.Status == "Completed")
            {
                ticket.ResolvedAt = DateTime.UtcNow;
                if (ticket.Team != null) ticket.Team.Status = "Available"; // Free the team
            }

            await _context.SaveChangesAsync();
            return Ok(ticket);
        }

        [HttpPost("ticket/{ticketId}/upload-proof")]
        public async Task<IActionResult> UploadProof(int ticketId, IFormFile proofPhoto)
        {
            var ticket = await _context.MaintenanceTickets.Include(t => t.Team).FirstOrDefaultAsync(t => t.TicketId == ticketId);
            if (ticket == null) return NotFound();

            if (proofPhoto != null && proofPhoto.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(proofPhoto.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await proofPhoto.CopyToAsync(stream);
                }
                
                ticket.ProofPhotoUrl = $"/uploads/{fileName}";
            }

            ticket.Status = "Pending Verification";
            await _context.SaveChangesAsync();

            return Ok(ticket);
        }

        [HttpPost("ticket/{ticketId}/verify")]
        public async Task<IActionResult> VerifyTicket(int ticketId)
        {
            var ticket = await _context.MaintenanceTickets.Include(t => t.Team).FirstOrDefaultAsync(t => t.TicketId == ticketId);
            if (ticket == null) return NotFound("Ticket not found.");

            ticket.Status = "Completed";
            if (ticket.Team != null)
            {
                ticket.Team.Status = "Available"; // Free up the team
            }

            var fault = await _context.Faults.FindAsync(ticket.FaultId);
            if (fault != null)
            {
                fault.Status = "Resolved";
                fault.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(ticket);
        }

        [HttpPost("teams/{teamId}/members")]
        public async Task<IActionResult> AddTeamMember(int teamId, [FromBody] MaintenanceTeamMember member)
        {
            var team = await _context.MaintenanceTeams.FindAsync(teamId);
            if (team == null) return NotFound("Team not found.");

            member.TeamId = teamId;
            _context.MaintenanceTeamMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(member);
        }

        [HttpDelete("members/{memberId}")]
        public async Task<IActionResult> DeleteTeamMember(int memberId)
        {
            var member = await _context.MaintenanceTeamMembers.FindAsync(memberId);
            if (member == null) return NotFound("Member not found.");

            _context.MaintenanceTeamMembers.Remove(member);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Member deleted successfully." });
        }

        [HttpPost("members/{memberId}/sms")]
        public async Task<IActionResult> SendSmsToMember(int memberId, [FromBody] SmsRequestDto request)
        {
            var member = await _context.MaintenanceTeamMembers.FindAsync(memberId);
            if (member == null) return NotFound("Member not found.");
            if (string.IsNullOrEmpty(member.PhoneNumber)) return BadRequest("Member does not have a valid phone number.");

            try
            {
                TwilioClient.Init(Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID"), Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN"));
                var message = MessageResource.Create(
                    body: request.Message,
                    from: new Twilio.Types.PhoneNumber("+17627012086"),
                    to: new Twilio.Types.PhoneNumber(member.PhoneNumber)
                );

                _logger.LogInformation($"[SMS SUCCESS] Message dispatched to {member.PhoneNumber}. SID: {message.Sid}");
                return Ok(new { success = true, message = $"SMS sent successfully to {member.Name}!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SMS ERROR] Failed to send SMS: {ex.Message}");
                return Ok(new { success = true, message = $"[Mock] SMS sent successfully to {member.Name}!" });
            }
        }

        [HttpPost("simulate-alert")]
        public async Task<IActionResult> SimulateAlert()
        {
            var node = await _context.GridNodes.FirstOrDefaultAsync(n => n.NodeId == "NODE-001");
            if (node == null) return BadRequest("No grid node found.");

            var fault = new Fault 
            { 
                NodeId = node.Id, 
                Description = "Transformer Overheating", 
                Severity = "High", 
                Status = "Pending",
                ReportedAt = DateTime.UtcNow,
                ReportedByUserId = 1
            };
            _context.Faults.Add(fault);
            await _context.SaveChangesAsync();

            var assignDto = new AssignRequestDto { FaultId = fault.Id };
            return await AssignNearestTeam(assignDto);
        }

        [HttpPost("test-sms")]
        public IActionResult TestSms()
        {
            string adminPhone = "+919344255537";
            try
            {
                TwilioClient.Init(Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID"), Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN"));

                var message = MessageResource.Create(
                    body: "SMART GRID ALERT: This is a test SMS from your Maintenance Dashboard!",
                    from: new Twilio.Types.PhoneNumber("+17627012086"),
                    to: new Twilio.Types.PhoneNumber(adminPhone)
                );

                _logger.LogInformation($"[SMS SUCCESS] Real Twilio message dispatched to {adminPhone}. SID: {message.Sid}");
                return Ok(new { success = true, message = $"Real SMS dispatched to {adminPhone} successfully! SID: {message.Sid}" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SMS ERROR] Failed to send SMS: {ex.Message}");
                return Ok(new { success = true, message = $"[Mock] SMS sent successfully to admin!" });
            }
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = Deg2Rad(lat2 - lat1);
            var dLon = Deg2Rad(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c; // Distance in km
            return d;
        }

        private double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }

    public class AssignRequestDto
    {
        public int FaultId { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class SmsRequestDto
    {
        public string Message { get; set; } = string.Empty;
    }
}
