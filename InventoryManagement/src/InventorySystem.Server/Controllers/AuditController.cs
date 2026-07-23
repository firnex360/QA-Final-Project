using System.Text.Json;
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
    [Authorize]
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
    [Authorize]
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

    // GET api/audit/stock-movements — the "Historial de Movimientos", derived from the audit
    // log: Product updates whose Quantity changed. No separate table; this reads AuditLogs.
    [HttpGet("stock-movements")]
    [Authorize]
    public async Task<IActionResult> GetStockMovements()
    {
        var movements = await LoadStockMovementsAsync();
        return Ok(movements.Take(100).ToList()); // cap so we don't ship the whole trail; client paginates
    }

    // GET api/audit/stock-movements/stats — aggregated figures over the same derived movements.
    [HttpGet("stock-movements/stats")]
    [Authorize]
    public async Task<IActionResult> GetStockMovementStats()
    {
        var movements = await LoadStockMovementsAsync();
        var weekStart = DateTime.UtcNow.Date.AddDays(-6); // last 7 calendar days incl. today

        var stats = new StockMovementStatsDto
        {
            TotalMovements = movements.Count,
            MovementsThisWeek = movements.Count(m => m.Timestamp >= weekStart),
            TotalUnitsIn = movements.Where(m => m.Delta > 0).Sum(m => m.Delta),
            TotalUnitsOut = movements.Where(m => m.Delta < 0).Sum(m => -m.Delta),
            ByType = new()
            {
                new LabelCountDto { Label = "In",  Count = movements.Count(m => m.Delta > 0) },
                new LabelCountDto { Label = "Out", Count = movements.Count(m => m.Delta < 0) }
            },
            ByDay = movements
                .Where(m => m.Timestamp >= weekStart)
                .GroupBy(m => m.Timestamp.Date)
                .Select(g => new LabelCountDto { Label = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
                .OrderBy(x => x.Label)
                .ToList(),
            // Most sold = most units removed from stock (Out movements), ranked by units.
            TopProducts = movements
                .Where(m => m.Delta < 0)
                .GroupBy(m => m.ProductName)
                .Select(g => new LabelCountDto { Label = g.Key, Count = g.Sum(m => -m.Delta) })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList()
        };
        return Ok(stats);
    }

    // Reads the audit trail and turns every Product update that changed Quantity into a
    // stock movement. Product names are resolved from the Products table so "most sold"
    // reads by name; deleted products fall back to "Product #id".
    private async Task<List<StockMovementDto>> LoadStockMovementsAsync()
    {
        var rows = await db.AuditLogs
            .Where(a => a.EntityName == "Product"
                        && a.Action == "Update"
                        && a.AffectedColumns != null
                        && a.AffectedColumns.Contains("Quantity"))
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new { a.Id, a.EntityId, a.UserId, a.Timestamp, a.OldValues, a.NewValues })
            .ToListAsync();

        var names = await db.Products
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id.ToString(), p => p.Name);

        var movements = new List<StockMovementDto>();
        foreach (var r in rows)
        {
            // AffectedColumns can match "Quantity" as a substring of another column name;
            // only treat it as a movement when a numeric Quantity actually parses on both sides.
            if (!TryGetQuantity(r.OldValues, out var oldQty) || !TryGetQuantity(r.NewValues, out var newQty))
                continue;
            if (oldQty == newQty)
                continue;

            movements.Add(new StockMovementDto
            {
                AuditId = r.Id,
                ProductId = int.TryParse(r.EntityId, out var pid) ? pid : 0,
                ProductName = names.GetValueOrDefault(r.EntityId) ?? $"Product #{r.EntityId}",
                UserId = r.UserId,
                Timestamp = r.Timestamp,
                PreviousQuantity = oldQty,
                NewQuantity = newQty,
                Delta = newQty - oldQty
            });
        }
        return movements;
    }

    // Pulls the integer "Quantity" out of an audit OldValues/NewValues JSON snapshot.
    private static bool TryGetQuantity(string? json, out int quantity)
    {
        quantity = 0;
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("Quantity", out var q))
                return false;

            return q.ValueKind switch
            {
                JsonValueKind.Number => q.TryGetInt32(out quantity),
                JsonValueKind.String => int.TryParse(q.GetString(), out quantity),
                _ => false
            };
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
