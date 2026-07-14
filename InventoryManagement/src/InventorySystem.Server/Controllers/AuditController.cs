using InventorySystem.Server.Data;
using InventorySystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuditController(ApplicationDbContext db) : ControllerBase
{
    // GET api/audit — most recent audit records (admin + manager only)
    [HttpGet]
    [Authorize(Policy = "audit:view")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await db.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(100)                  // cap so you don't ship the whole table; client paginates these
            .Select(a => new AuditLogDto
            {
                Id = a.Id, EntityName = a.EntityName, EntityId = a.EntityId,
                Action = a.Action, Timestamp = a.Timestamp, UserId = a.UserId,
                OldValues = a.OldValues, NewValues = a.NewValues,
                AffectedColumns = a.AffectedColumns
            })
            .ToListAsync();
        return Ok(logs);
    }

    // GET api/audit/stats — aggregated audit figures for the admin dashboard
    [HttpGet("stats")]
    [Authorize(Policy = "audit:view")]
    public async Task<IActionResult> GetAuditStats()
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-6); // last 7 calendar days incl. today

        var byAction = await db.AuditLogs
            .GroupBy(a => a.Action)
            .Select(g => new LabelCountDto { Label = g.Key, Count = g.Count() })
            .ToListAsync();

        var byUser = await db.AuditLogs
            .GroupBy(a => a.UserId ?? "anonymous")
            .Select(g => new LabelCountDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var byEntity = await db.AuditLogs
            .GroupBy(a => a.EntityName)
            .Select(g => new LabelCountDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Group last week's events by day in memory (small set; avoids DB date-trunc quirks)
        var lastWeek = await db.AuditLogs
            .Where(a => a.Timestamp >= weekStart)
            .Select(a => a.Timestamp)
            .ToListAsync();
        var byDay = lastWeek
            .GroupBy(ts => ts.Date)
            .Select(g => new LabelCountDto { Label = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
            .OrderBy(x => x.Label)
            .ToList();

        var stats = new AuditStatsDto
        {
            TotalEvents = await db.AuditLogs.CountAsync(),
            EventsToday = await db.AuditLogs.CountAsync(a => a.Timestamp >= today),
            EventsThisWeek = lastWeek.Count,
            ByAction = byAction,
            ByUser = byUser,
            ByDay = byDay,
            ByEntity = byEntity
        };
        return Ok(stats);
    }
}
