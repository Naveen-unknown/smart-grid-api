using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGridAPI.Data;
using SmartGridAPI.Services;

namespace SmartGridAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, IAIService aiService, ILogger<DashboardController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>Get full dashboard summary</summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var totalNodes = await _context.GridNodes.CountAsync();
                var activeNodes = await _context.GridNodes.CountAsync(n => n.Status == "Active");
                var maintenanceNodes = await _context.GridNodes.CountAsync(n => n.Status == "Maintenance");

                var totalFaults = await _context.Faults.CountAsync();
                var openFaults = await _context.Faults.CountAsync(f => f.Status == "Reported" || f.Status == "InProgress");
                var criticalFaults = await _context.Faults.CountAsync(f => f.Severity == "Critical" && (f.Status == "Reported" || f.Status == "InProgress"));
                var ongoingOutages = await _context.Outages.CountAsync(o => o.Status == "Ongoing");

                var totalAffectedCustomers = await _context.Outages
                    .Where(o => o.Status == "Ongoing")
                    .SumAsync(o => (int?)(o.AffectedCustomers ?? 0)) ?? 0;

                // Today's energy data
                var latestReading = await _context.EnergyReadings.OrderByDescending(r => r.Timestamp).FirstOrDefaultAsync();
                var todayStart = latestReading != null ? latestReading.Timestamp.Date : DateTime.UtcNow.Date;
                var todayReadings = await _context.EnergyReadings
                    .Where(r => r.Timestamp >= todayStart)
                    .ToListAsync();

                // Last 7 days trend – pull raw grouped data then format client-side
                var weekStart = todayStart.AddDays(-7);
                var weekReadings = (await _context.EnergyReadings
                    .Where(r => r.Timestamp >= weekStart)
                    .GroupBy(r => r.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Consumption = g.Sum(r => r.Consumption),
                        Production = g.Sum(r => r.Production),
                        AvgVoltage = g.Average(r => r.Voltage)
                    })
                    .OrderBy(r => r.Date)
                    .ToListAsync())
                    .Select(r => new
                    {
                        Date = r.Date.ToString("yyyy-MM-dd"),
                        Consumption = Math.Round(r.Consumption, 2),
                        Production = Math.Round(r.Production, 2),
                        AvgVoltage = Math.Round(r.AvgVoltage, 2)
                    }).ToList();

                // Fault trend last 7 days – pull then format client-side
                var faultTrend = (await _context.Faults
                    .Where(f => f.ReportedAt >= weekStart)
                    .GroupBy(f => f.ReportedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Critical = g.Count(f => f.Severity == "Critical")
                    })
                    .OrderBy(f => f.Date)
                    .ToListAsync())
                    .Select(f => new
                    {
                        Date = f.Date.ToString("yyyy-MM-dd"),
                        f.Count,
                        f.Critical
                    }).ToList();

                // Nodes with readings
                var nodeStatus = await _context.GridNodes
                    .Select(n => new
                    {
                        n.Id, n.NodeId, n.Location, n.Status, n.NodeType,
                        LastReading = n.EnergyReadings
                            .OrderByDescending(r => r.Timestamp)
                            .Select(r => new { r.Voltage, r.Current, r.PowerFactor, r.Timestamp })
                            .FirstOrDefault(),
                        OpenFaults = n.Faults.Count(f => f.Status == "Reported" || f.Status == "InProgress")
                    })
                    .ToListAsync();

                var summary = new
                {
                    Grid = new
                    {
                        TotalNodes = totalNodes,
                        ActiveNodes = activeNodes,
                        MaintenanceNodes = maintenanceNodes,
                        InactiveNodes = totalNodes - activeNodes - maintenanceNodes,
                        NodeHealthPercent = totalNodes > 0 ? Math.Round((double)activeNodes / totalNodes * 100, 1) : 0
                    },
                    Faults = new
                    {
                        Total = totalFaults,
                        Open = openFaults,
                        Critical = criticalFaults,
                        ResolvedToday = await _context.Faults.CountAsync(f =>
                            f.ResolvedAt.HasValue && f.ResolvedAt.Value >= todayStart)
                    },
                    Outages = new
                    {
                        Ongoing = ongoingOutages,
                        AffectedCustomers = totalAffectedCustomers,
                        RestoredToday = await _context.Outages.CountAsync(o =>
                            o.RestoredAt.HasValue && o.RestoredAt.Value >= todayStart)
                    },
                    Energy = new
                    {
                        TodayConsumption = Math.Round(todayReadings.Sum(r => r.Consumption), 2),
                        TodayProduction = Math.Round(todayReadings.Sum(r => r.Production), 2),
                        TodayReadings = todayReadings.Count,
                        AvgVoltage = todayReadings.Any() ? Math.Round(todayReadings.Average(r => r.Voltage), 2) : 0,
                        AvgPowerFactor = todayReadings.Any() ? Math.Round(todayReadings.Average(r => r.PowerFactor), 3) : 0
                    },
                    NodeStatus = nodeStatus,
                    EnergyTrend = weekReadings,
                    FaultTrend = faultTrend,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
                return StatusCode(500, new { success = false, message = "Error retrieving dashboard data" });
            }
        }

        /// <summary>Get AI grid health insights</summary>
        [HttpGet("ai-insights")]
        public async Task<IActionResult> GetAIInsights()
        {
            try
            {
                var totalNodes = await _context.GridNodes.CountAsync();
                var activeNodes = await _context.GridNodes.CountAsync(n => n.Status == "Active");
                var openFaults = await _context.Faults.CountAsync(f => f.Status == "Reported" || f.Status == "InProgress");
                var ongoingOutages = await _context.Outages.CountAsync(o => o.Status == "Ongoing");

                // Energy insights
                var recentReadings = await _context.EnergyReadings
                    .OrderByDescending(r => r.Timestamp)
                    .Take(200)
                    .ToListAsync();

                string energyInsights = "No readings available for analysis.";
                string loadOptimization = "No load data available.";
                string healthInsights = string.Empty;

                if (recentReadings.Any())
                {
                    var totalConsumption = recentReadings.Sum(r => r.Consumption);
                    var totalProduction = recentReadings.Sum(r => r.Production);
                    var avgVoltage = recentReadings.Average(r => r.Voltage);

                    energyInsights = await _aiService.AnalyzeEnergyDataAsync(
                        totalConsumption, totalProduction, avgVoltage);

                    var loads = recentReadings
                        .GroupBy(r => r.NodeId)
                        .ToDictionary(g => $"Node-{g.Key}", g => g.Sum(r => r.Consumption));

                    loadOptimization = await _aiService.OptimizeLoadDistributionAsync(loads);
                }

                healthInsights = await _aiService.GetGridHealthInsightsAsync(
                    totalNodes, activeNodes, openFaults, ongoingOutages);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        HealthInsights = healthInsights,
                        EnergyInsights = energyInsights,
                        LoadOptimization = loadOptimization,
                        GeneratedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI insights");
                return StatusCode(500, new { success = false, message = "Error getting AI insights" });
            }
        }

        /// <summary>Generate a period report with AI summary</summary>
        [HttpGet("report")]
        [Authorize(Roles = "Admin,Operator")]
        public async Task<IActionResult> GenerateReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                if (start >= end)
                    return BadRequest(new { success = false, message = "Start date must be before end date" });

                var readings = await _context.EnergyReadings
                    .Where(r => r.Timestamp >= start && r.Timestamp <= end)
                    .ToListAsync();

                var faults = await _context.Faults
                    .Where(f => f.ReportedAt >= start && f.ReportedAt <= end)
                    .ToListAsync();

                var outages = await _context.Outages
                    .Where(o => o.StartedAt >= start && o.StartedAt <= end)
                    .ToListAsync();

                var metrics = new Dictionary<string, object>
                {
                    ["Total Readings"] = readings.Count,
                    ["Total Consumption (kWh)"] = Math.Round(readings.Sum(r => (double)r.Consumption), 2),
                    ["Total Production (kWh)"] = Math.Round(readings.Sum(r => (double)r.Production), 2),
                    ["Avg Voltage (V)"] = readings.Any() ? Math.Round(readings.Average(r => (double)r.Voltage), 2) : 0,
                    ["Avg Power Factor"] = readings.Any() ? Math.Round(readings.Average(r => (double)r.PowerFactor), 3) : 0,
                    ["Total Faults"] = faults.Count,
                    ["Critical Faults"] = faults.Count(f => f.Severity == "Critical"),
                    ["Resolved Faults"] = faults.Count(f => f.Status == "Resolved" || f.Status == "Closed"),
                    ["Total Outages"] = outages.Count,
                    ["Customers Affected"] = outages.Sum(o => o.AffectedCustomers ?? 0),
                    ["Avg Restoration Time (hrs)"] = outages.Where(o => o.RestoredAt.HasValue)
                        .Select(o => (o.RestoredAt!.Value - o.StartedAt).TotalHours)
                        .DefaultIfEmpty(0).Average()
                };

                var aiReport = await _aiService.GenerateReportAsync(start, end, metrics);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Period = new { StartDate = start, EndDate = end, Days = (end - start).Days },
                        Metrics = metrics,
                        AIReport = aiReport,
                        GeneratedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, new { success = false, message = "Error generating report" });
            }
        }
    }
}
