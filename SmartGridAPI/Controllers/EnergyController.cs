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
    public class EnergyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<EnergyController> _logger;

        public EnergyController(ApplicationDbContext context, IAIService aiService, ILogger<EnergyController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>Get energy readings with optional filters</summary>
        [HttpGet("readings")]
        public async Task<IActionResult> GetReadings(
            [FromQuery] int? nodeId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.EnergyReadings
                    .Include(r => r.Node)
                    .AsQueryable();

                if (nodeId.HasValue && nodeId.Value > 0)
                    query = query.Where(r => r.NodeId == nodeId.Value);

                if (startDate.HasValue)
                    query = query.Where(r => r.Timestamp >= startDate.Value.ToUniversalTime());

                if (endDate.HasValue)
                    query = query.Where(r => r.Timestamp <= endDate.Value.ToUniversalTime());

                var total = await query.CountAsync();
                var readings = await query
                    .OrderByDescending(r => r.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id, r.NodeId, r.UserId, r.Consumption, r.Production,
                        r.Voltage, r.Current, r.PowerFactor, r.Frequency,
                        r.Timestamp, r.MeterId,
                        NodeName = r.Node != null ? r.Node.Location : "Unknown"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = readings,
                    pagination = new { total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting energy readings");
                return StatusCode(500, new { success = false, message = "Error retrieving readings" });
            }
        }

        /// <summary>Add a new energy reading</summary>
        [HttpPost("readings")]
        public async Task<IActionResult> AddReading([FromBody] EnergyReadingDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

                var node = await _context.GridNodes.FindAsync(dto.NodeId);
                if (node == null)
                    return BadRequest(new { success = false, message = $"Node {dto.NodeId} not found" });

                var reading = new EnergyReading
                {
                    NodeId = dto.NodeId,
                    UserId = dto.UserId,
                    Consumption = dto.Consumption,
                    Production = dto.Production,
                    Voltage = dto.Voltage,
                    Current = dto.Current,
                    PowerFactor = dto.PowerFactor,
                    Frequency = dto.Frequency,
                    MeterId = dto.MeterId,
                    Timestamp = DateTime.UtcNow
                };

                _context.EnergyReadings.Add(reading);

                // Update node last updated
                node.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetReadings), new { },
                    new { success = true, message = "Reading added successfully", data = reading });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding energy reading");
                return StatusCode(500, new { success = false, message = "Error adding reading" });
            }
        }

        /// <summary>Get analytics for a period</summary>
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics(
            [FromQuery] int? nodeId,
            [FromQuery] string period = "daily")
        {
            try
            {
                var endDate = DateTime.UtcNow;
                var startDate = period switch
                {
                    "hourly" => endDate.AddHours(-24),
                    "daily" => endDate.AddDays(-7),
                    "weekly" => endDate.AddDays(-30),
                    "monthly" => endDate.AddMonths(-12),
                    _ => endDate.AddDays(-7)
                };

                var query = _context.EnergyReadings
                    .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate);

                if (nodeId.HasValue && nodeId.Value > 0)
                    query = query.Where(r => r.NodeId == nodeId.Value);

                var readings = await query.ToListAsync();

                if (!readings.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        analytics = new { Message = "No data available for the selected period" },
                        insights = "No data to analyze.",
                        chartData = Array.Empty<object>()
                    });
                }

                var analytics = new
                {
                    TotalConsumption = Math.Round(readings.Sum(r => r.Consumption), 2),
                    TotalProduction = Math.Round(readings.Sum(r => r.Production), 2),
                    AverageVoltage = Math.Round(readings.Average(r => r.Voltage), 2),
                    AverageCurrent = Math.Round(readings.Average(r => r.Current), 2),
                    AveragePowerFactor = Math.Round(readings.Average(r => r.PowerFactor), 3),
                    AverageFrequency = Math.Round(readings.Average(r => r.Frequency), 2),
                    PeakConsumption = readings.Max(r => r.Consumption),
                    PeakProduction = readings.Max(r => r.Production),
                    MinVoltage = readings.Min(r => r.Voltage),
                    MaxVoltage = readings.Max(r => r.Voltage),
                    DataPoints = readings.Count,
                    Period = period,
                    StartDate = startDate,
                    EndDate = endDate
                };

                // Chart data grouped by day
                var chartData = readings
                    .GroupBy(r => r.Timestamp.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Consumption = Math.Round(g.Sum(r => r.Consumption), 2),
                        Production = Math.Round(g.Sum(r => r.Production), 2),
                        AvgVoltage = Math.Round(g.Average(r => r.Voltage), 2),
                        AvgPowerFactor = Math.Round(g.Average(r => r.PowerFactor), 3)
                    })
                    .ToList();

                var insights = await _aiService.AnalyzeEnergyDataAsync(
                    analytics.TotalConsumption,
                    analytics.TotalProduction,
                    analytics.AverageVoltage
                );

                return Ok(new { success = true, analytics, insights, chartData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics");
                return StatusCode(500, new { success = false, message = "Error getting analytics" });
            }
        }

        /// <summary>Get summary for a specific node</summary>
        [HttpGet("nodes/{nodeId}/summary")]
        public async Task<IActionResult> GetNodeSummary(int nodeId)
        {
            try
            {
                var node = await _context.GridNodes.FindAsync(nodeId);
                if (node == null)
                    return NotFound(new { success = false, message = "Node not found" });

                var latestReading = await _context.EnergyReadings
                    .Where(r => r.NodeId == nodeId)
                    .OrderByDescending(r => r.Timestamp)
                    .FirstOrDefaultAsync();

                var todayStart = DateTime.UtcNow.Date;
                var todayReadings = await _context.EnergyReadings
                    .Where(r => r.NodeId == nodeId && r.Timestamp >= todayStart)
                    .ToListAsync();

                var openFaults = await _context.Faults
                    .CountAsync(f => f.NodeId == nodeId && f.Status != "Resolved" && f.Status != "Closed");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Node = node,
                        LatestReading = latestReading,
                        TodayConsumption = todayReadings.Sum(r => r.Consumption),
                        TodayProduction = todayReadings.Sum(r => r.Production),
                        TodayReadingCount = todayReadings.Count,
                        AverageVoltage = todayReadings.Any() ? todayReadings.Average(r => r.Voltage) : 0,
                        AveragePowerFactor = todayReadings.Any() ? todayReadings.Average(r => r.PowerFactor) : 0,
                        OpenFaults = openFaults
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node summary for node {NodeId}", nodeId);
                return StatusCode(500, new { success = false, message = "Error getting node summary" });
            }
        }

        /// <summary>Get all grid nodes</summary>
        [HttpGet("nodes")]
        public async Task<IActionResult> GetNodes()
        {
            try
            {
                var nodes = await _context.GridNodes
                    .OrderBy(n => n.NodeId)
                    .ToListAsync();

                return Ok(new { success = true, data = nodes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nodes");
                return StatusCode(500, new { success = false, message = "Error getting nodes" });
            }
        }

        /// <summary>Create a new grid node</summary>
        [HttpPost("nodes")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNode([FromBody] GridNodeDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid data" });

                var exists = await _context.GridNodes.AnyAsync(n => n.NodeId == dto.NodeId);
                if (exists)
                    return Conflict(new { success = false, message = "Node ID already exists" });

                var node = new GridNode
                {
                    NodeId = dto.NodeId,
                    Location = dto.Location,
                    Status = dto.Status,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    NodeType = dto.NodeType,
                    MaxCapacity = dto.MaxCapacity,
                    CreatedAt = DateTime.UtcNow
                };

                _context.GridNodes.Add(node);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNodes), new { },
                    new { success = true, message = "Node created successfully", data = node });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating node");
                return StatusCode(500, new { success = false, message = "Error creating node" });
            }
        }

        /// <summary>Update node status</summary>
        [HttpPatch("nodes/{id}/status")]
        [Authorize(Roles = "Admin,Operator")]
        public async Task<IActionResult> UpdateNodeStatus(int id, [FromBody] string status)
        {
            try
            {
                var validStatuses = new[] { "Active", "Inactive", "Maintenance" };
                if (!validStatuses.Contains(status))
                    return BadRequest(new { success = false, message = "Invalid status. Must be Active, Inactive, or Maintenance" });

                var node = await _context.GridNodes.FindAsync(id);
                if (node == null)
                    return NotFound(new { success = false, message = "Node not found" });

                node.Status = status;
                node.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Node status updated", data = node });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating node status");
                return StatusCode(500, new { success = false, message = "Error updating node status" });
            }
        }
    }
}
