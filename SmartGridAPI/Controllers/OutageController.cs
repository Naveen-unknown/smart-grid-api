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
    public class OutageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OutageController> _logger;

        public OutageController(ApplicationDbContext context, IAIService aiService, INotificationService notificationService, ILogger<OutageController> logger)
        {
            _context = context;
            _aiService = aiService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>Get all outages</summary>
        [HttpGet]
        public async Task<IActionResult> GetOutages(
            [FromQuery] string? status,
            [FromQuery] int? nodeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Outages
                    .Include(o => o.Node)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(o => o.Status == status);

                if (nodeId.HasValue && nodeId.Value > 0)
                    query = query.Where(o => o.NodeId == nodeId.Value);

                var total = await query.CountAsync();
                var rawList = await query
                    .OrderByDescending(o => o.StartedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var outages = rawList.Select(o => new
                {
                    o.Id, o.NodeId, o.AffectedArea, o.AffectedCustomers,
                    o.StartedAt, o.RestoredAt, o.EstimatedRestorationTime,
                    o.Status, o.OutageType, o.Cause, o.ActionTaken, o.AIAnalysis,
                    NodeLocation = o.Node?.Location ?? "Unknown",
                    NodeIdentifier = o.Node?.NodeId ?? "N/A",
                    Duration = Math.Round(o.RestoredAt.HasValue
                        ? (o.RestoredAt.Value - o.StartedAt).TotalHours
                        : (DateTime.UtcNow - o.StartedAt).TotalHours, 2)
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = outages,
                    pagination = new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outages");
                return StatusCode(500, new { success = false, message = "Error retrieving outages" });
            }
        }



        /// <summary>Get outage by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOutage(int id)
        {
            try
            {
                var outage = await _context.Outages
                    .Include(o => o.Node)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (outage == null)
                    return NotFound(new { success = false, message = "Outage not found" });

                return Ok(new { success = true, data = outage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outage {OutageId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving outage" });
            }
        }

        /// <summary>Report a new outage</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Electricity Officer")]
        public async Task<IActionResult> ReportOutage([FromBody] OutageReportDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

                var node = await _context.GridNodes.FindAsync(dto.NodeId);
                if (node == null)
                    return BadRequest(new { success = false, message = $"Node {dto.NodeId} not found" });

                // Get AI analysis
                var aiAnalysis = await _aiService.AnalyzeOutageAsync(
                    node.NodeId,
                    dto.Cause ?? "Unknown",
                    dto.AffectedCustomers ?? 0
                );

                var outage = new Outage
                {
                    NodeId = dto.NodeId,
                    AffectedArea = dto.AffectedArea,
                    AffectedCustomers = dto.AffectedCustomers,
                    Cause = dto.Cause,
                    OutageType = dto.OutageType,
                    Status = "Ongoing",
                    StartedAt = DateTime.UtcNow,
                    EstimatedRestorationTime = dto.EstimatedRestorationTime,
                    AIAnalysis = aiAnalysis,
                    ReportedByUserId = dto.ReportedByUserId
                };

                _context.Outages.Add(outage);

                // Mark node as having an issue
                node.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Outage reported at {NodeId}: affecting {Customers} customers",
                    node.NodeId, dto.AffectedCustomers);

                // Trigger Notification / SMS
                await _notificationService.SendNotificationAsync(
                    title: $"New Outage Reported at {node.Location}",
                    message: $"Type: {dto.OutageType}. Node: {node.NodeId}. Area: {dto.AffectedArea}. Affected Customers: {dto.AffectedCustomers}. Cause: {dto.Cause}",
                    type: "Critical",
                    targetRole: "Electricity Officer"
                );

                return CreatedAtAction(nameof(GetOutage), new { id = outage.Id },
                    new { success = true, message = "Outage reported successfully", data = outage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting outage");
                return StatusCode(500, new { success = false, message = "Error reporting outage" });
            }
        }

        /// <summary>Restore an outage (mark as resolved)</summary>
        [HttpPatch("{id}/restore")]
        [Authorize(Roles = "Admin,Electricity Officer")]
        public async Task<IActionResult> RestoreOutage(int id, [FromBody] RestoreOutageRequest request)
        {
            try
            {
                var outage = await _context.Outages.Include(o => o.Node).FirstOrDefaultAsync(o => o.Id == id);
                if (outage == null)
                    return NotFound(new { success = false, message = "Outage not found" });

                if (outage.Status == "Restored")
                    return BadRequest(new { success = false, message = "Outage already restored" });

                outage.Status = "Restored";
                outage.RestoredAt = DateTime.UtcNow;
                outage.ActionTaken = request.ActionTaken;

                await _context.SaveChangesAsync();

                var duration = outage.RestoredAt.Value - outage.StartedAt;
                _logger.LogInformation("Outage {OutageId} restored after {Hours:F1} hours", id, duration.TotalHours);

                return Ok(new
                {
                    success = true,
                    message = "Outage marked as restored",
                    data = outage,
                    duration = new { Hours = Math.Round(duration.TotalHours, 1), Minutes = (int)duration.TotalMinutes }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring outage {OutageId}", id);
                return StatusCode(500, new { success = false, message = "Error restoring outage" });
            }
        }

        /// <summary>Get outage statistics</summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetOutageStats()
        {
            try
            {
                var allOutages = await _context.Outages.ToListAsync();

                var resolvedOutages = allOutages.Where(o => o.RestoredAt.HasValue).ToList();

                var stats = new
                {
                    Total = allOutages.Count,
                    Ongoing = allOutages.Count(o => o.Status == "Ongoing"),
                    Restored = allOutages.Count(o => o.Status == "Restored"),
                    TotalAffectedCustomers = allOutages.Where(o => o.Status == "Ongoing").Sum(o => o.AffectedCustomers ?? 0),
                    AvgRestorationHours = resolvedOutages.Any()
                        ? Math.Round(resolvedOutages.Average(o => (o.RestoredAt!.Value - o.StartedAt).TotalHours), 2)
                        : 0,
                    Last30Days = allOutages.Count(o => o.StartedAt >= DateTime.UtcNow.AddDays(-30)),
                    ByType = allOutages.GroupBy(o => o.OutageType)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outage stats");
                return StatusCode(500, new { success = false, message = "Error getting outage statistics" });
            }
        }
    }

    public class RestoreOutageRequest
    {
        public string ActionTaken { get; set; } = string.Empty;
    }
}
