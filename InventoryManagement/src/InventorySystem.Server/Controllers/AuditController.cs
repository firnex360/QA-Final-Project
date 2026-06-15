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
    [Authorize(Policy = "CanUpdate")]   // adminY + managerY; use "CanDelete" for admin-only
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
}
