using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGridAPI.Data;
using SmartGridAPI.DTOs;
using SmartGridAPI.Models;
using SmartGridAPI.Services;

namespace SmartGridAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FaultController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<FaultController> _logger;

        public FaultController(ApplicationDbContext context, IAIService aiService, INotificationService notificationService, ILogger<FaultController> logger)
        {
            _context = context;
            _aiService = aiService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>Get all faults with optional filters</summary>
        [HttpGet]
        public async Task<IActionResult> GetFaults(
            [FromQuery] string? status,
            [FromQuery] string? severity,
            [FromQuery] int? nodeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Faults
                    .Include(f => f.Node)
                    .Include(f => f.ReportedBy)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(f => f.Status == status);

                if (!string.IsNullOrEmpty(severity))
                    query = query.Where(f => f.Severity == severity);

                if (nodeId.HasValue && nodeId.Value > 0)
                    query = query.Where(f => f.NodeId == nodeId.Value);

                var total = await query.CountAsync();
                var faults = await query
                    .OrderByDescending(f => f.ReportedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new
                    {
                        f.Id, f.NodeId, f.ReportedByUserId, f.FaultType,
                        f.Description, f.Severity, f.Status, f.ReportedAt,
                        f.ResolvedAt, f.AcknowledgedAt, f.AssignedTo,
                        f.ResolutionNotes, f.AIPrediction, f.ConfidenceScore, f.AIRecommendation,
                        NodeLocation = f.Node != null ? f.Node.Location : "Unknown",
                        NodeIdentifier = f.Node != null ? f.Node.NodeId : "N/A",
                        ReportedByName = f.ReportedBy != null ? f.ReportedBy.Username : "System"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = faults,
                    pagination = new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting faults");
                return StatusCode(500, new { success = false, message = "Error retrieving faults" });
            }
        }

        /// <summary>Get a single fault by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFault(int id)
        {
            try
            {
                var fault = await _context.Faults
                    .Include(f => f.Node)
                    .Include(f => f.ReportedBy)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (fault == null)
                    return NotFound(new { success = false, message = "Fault not found" });

                return Ok(new { success = true, data = fault });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fault {FaultId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving fault" });
            }
        }

        /// <summary>Report a new fault</summary>
        [HttpPost]
        public async Task<IActionResult> ReportFault([FromBody] FaultReportDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

                var node = await _context.GridNodes.FindAsync(dto.NodeId);
                if (node == null)
                    return BadRequest(new { success = false, message = $"Node {dto.NodeId} not found" });

                // Get latest reading for AI analysis
                var latestReading = await _context.EnergyReadings
                    .Where(r => r.NodeId == dto.NodeId)
                    .OrderByDescending(r => r.Timestamp)
                    .FirstOrDefaultAsync();

                string aiPrediction = string.Empty;
                string aiRecommendation = string.Empty;
                decimal confidenceScore = 0;

                if (latestReading != null)
                {
                    aiPrediction = await _aiService.PredictFaultAsync(
                        node.NodeId,
                        latestReading.Voltage,
                        latestReading.Current,
                        latestReading.PowerFactor
                    );
                    aiRecommendation = $"Based on latest readings: V={latestReading.Voltage:F1}V, I={latestReading.Current:F1}A, PF={latestReading.PowerFactor:F3}";
                    confidenceScore = latestReading.Voltage < 210 || latestReading.Voltage > 250 ? 0.92m : 0.75m;
                }

                var fault = new Fault
                {
                    NodeId = dto.NodeId,
                    ReportedByUserId = dto.ReportedByUserId,
                    FaultType = dto.FaultType,
                    Description = dto.Description,
                    Severity = dto.Severity,
                    AssignedTo = dto.AssignedTo,
                    Status = "Reported",
                    ReportedAt = DateTime.UtcNow,
                    AIPrediction = aiPrediction,
                    AIRecommendation = aiRecommendation,
                    ConfidenceScore = confidenceScore
                };

                _context.Faults.Add(fault);
                await _context.SaveChangesAsync();

                if (fault.Severity == "Critical")
                    _logger.LogWarning("CRITICAL FAULT at {NodeId}: {Description}", node.NodeId, fault.Description);

                // Trigger Notification / SMS
                var notificationType = fault.Severity == "Critical" ? "Critical" : (fault.Severity == "High" ? "Warning" : "Info");
                await _notificationService.SendNotificationAsync(
                    title: $"New Fault Reported: {fault.FaultType} at {node.Location}",
                    message: $"Severity: {fault.Severity}. Node: {node.NodeId}. Desc: {fault.Description}",
                    type: notificationType,
                    targetRole: "Electricity Officer"
                );

                return CreatedAtAction(nameof(GetFault), new { id = fault.Id },
                    new { success = true, message = "Fault reported successfully", data = fault });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting fault");
                return StatusCode(500, new { success = false, message = "Error reporting fault" });
            }
        }

        /// <summary>Update fault status</summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Electricity Officer")]
        public async Task<IActionResult> UpdateFaultStatus(int id, [FromBody] UpdateFaultStatusRequest request)
        {
            try
            {
                var validStatuses = new[] { "Reported", "InProgress", "Resolved", "Closed" };
                if (!validStatuses.Contains(request.Status))
                    return BadRequest(new { success = false, message = "Invalid status" });

                var fault = await _context.Faults.FindAsync(id);
                if (fault == null)
                    return NotFound(new { success = false, message = "Fault not found" });

                fault.Status = request.Status;

                if (request.Status == "InProgress")
                    fault.AcknowledgedAt ??= DateTime.UtcNow;

                if (request.Status == "Resolved" || request.Status == "Closed")
                {
                    fault.ResolvedAt = DateTime.UtcNow;
                    fault.ResolutionNotes = request.Notes;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Status updated", data = fault });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating fault {FaultId}", id);
                return StatusCode(500, new { success = false, message = "Error updating fault" });
            }
        }

        /// <summary>Get AI fault prediction for a node</summary>
        [HttpGet("predict/{nodeId}")]
        public async Task<IActionResult> PredictFaults(int nodeId)
        {
            try
            {
                var node = await _context.GridNodes.FindAsync(nodeId);
                if (node == null)
                    return NotFound(new { success = false, message = "Node not found" });

                var readings = await _context.EnergyReadings
                    .Where(r => r.NodeId == nodeId)
                    .OrderByDescending(r => r.Timestamp)
                    .Take(48) // Last 48 readings
                    .ToListAsync();

                if (!readings.Any())
                    return BadRequest(new { success = false, message = "No readings available for prediction" });

                var latest = readings.First();
                var prediction = await _aiService.PredictFaultAsync(
                    node.NodeId,
                    latest.Voltage,
                    latest.Current,
                    latest.PowerFactor
                );

                // Historical fault stats
                var recentFaults = await _context.Faults
                    .Where(f => f.NodeId == nodeId && f.ReportedAt >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Node = new { node.Id, node.NodeId, node.Location, node.Status },
                        LatestReading = latest,
                        AIPrediction = prediction,
                        ReadingsAnalyzed = readings.Count,
                        RecentFaults30Days = recentFaults,
                        AnalysisTimestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting faults for node {NodeId}", nodeId);
                return StatusCode(500, new { success = false, message = "Error predicting faults" });
            }
        }

        /// <summary>Get fault statistics</summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetFaultStats()
        {
            try
            {
                var allFaults = await _context.Faults.ToListAsync();

                var stats = new
                {
                    Total = allFaults.Count,
                    ByStatus = allFaults.GroupBy(f => f.Status)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    BySeverity = allFaults.GroupBy(f => f.Severity)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ByType = allFaults.GroupBy(f => f.FaultType)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    Last30Days = allFaults.Count(f => f.ReportedAt >= DateTime.UtcNow.AddDays(-30)),
                    AvgResolutionHours = allFaults
                        .Where(f => f.ResolvedAt.HasValue)
                        .Select(f => (f.ResolvedAt!.Value - f.ReportedAt).TotalHours)
                        .DefaultIfEmpty(0)
                        .Average()
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fault stats");
                return StatusCode(500, new { success = false, message = "Error getting fault statistics" });
            }
        }
    }

    public class UpdateFaultStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
