using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAgenda.Data;
using ChatAgenda.Models;
using ChatAgenda.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ChatAgenda.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CalendarController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly GoogleCalendarSyncService _syncService;

        public CalendarController(AppDbContext context, GoogleCalendarSyncService syncService)
        {
            _context = context;
            _syncService = syncService;
        }

        // Get calendar events for a date range
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var query = _context.Events.Where(e => e.SyncStatus != "PendingDelete");

            if (start.HasValue)
            {
                query = query.Where(e => e.EndTime >= start.Value);
            }
            if (end.HasValue)
            {
                query = query.Where(e => e.StartTime <= end.Value);
            }

            var events = await query
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            return Ok(events);
        }

        public class CreateEventRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }

        // Create a new event
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { message = "El título del evento es requerido." });
            }
            if (request.StartTime >= request.EndTime)
            {
                return BadRequest(new { message = "La hora de inicio debe ser anterior a la hora de fin." });
            }

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Sistema";
            var displayName = User.FindFirst("DisplayName")?.Value ?? User.Identity?.Name ?? "Usuario LAN";

            var newEvent = new Event
            {
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                StartTime = request.StartTime.ToUniversalTime(),
                EndTime = request.EndTime.ToUniversalTime(),
                CreatedByUserId = currentUserId,
                CreatedByUserName = displayName,
                LastModified = DateTime.UtcNow,
                LastModifiedBy = displayName,
                SyncStatus = "PendingUpdate", // Triggers push to Google Calendar
                Version = 1
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            // Log history
            _context.SyncHistories.Add(new SyncHistory
            {
                EventId = newEvent.Id,
                EventTitle = newEvent.Title,
                ChangeDescription = "Evento creado localmente",
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = displayName,
                Source = "Local"
            });
            await _context.SaveChangesAsync();

            // Trigger sync immediately in background without blocking
            _ = Task.Run(() => _syncService.SyncAsync(default));

            return Ok(newEvent);
        }

        public class UpdateEventRequest
        {
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
        }

        // Update an event
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {
            var localEvent = await _context.Events.FindAsync(id);
            if (localEvent == null || localEvent.SyncStatus == "PendingDelete")
            {
                return NotFound(new { message = "Evento no encontrado." });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { message = "El título del evento es requerido." });
            }
            if (request.StartTime >= request.EndTime)
            {
                return BadRequest(new { message = "La hora de inicio debe ser anterior a la hora de fin." });
            }

            var displayName = User.FindFirst("DisplayName")?.Value ?? User.Identity?.Name ?? "Usuario LAN";

            localEvent.Title = request.Title.Trim();
            localEvent.Description = request.Description.Trim();
            localEvent.StartTime = request.StartTime.ToUniversalTime();
            localEvent.EndTime = request.EndTime.ToUniversalTime();
            localEvent.SyncStatus = "PendingUpdate"; // Mark for sync update
            localEvent.LastModified = DateTime.UtcNow;
            localEvent.LastModifiedBy = displayName;
            localEvent.Version += 1;

            _context.SyncHistories.Add(new SyncHistory
            {
                EventId = localEvent.Id,
                EventTitle = localEvent.Title,
                ChangeDescription = "Evento modificado localmente",
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = displayName,
                Source = "Local"
            });

            await _context.SaveChangesAsync();

            // Trigger sync immediately
            _ = Task.Run(() => _syncService.SyncAsync(default));

            return Ok(localEvent);
        }

        // Delete an event
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var localEvent = await _context.Events.FindAsync(id);
            if (localEvent == null) return NotFound(new { message = "Evento no encontrado." });

            var displayName = User.FindFirst("DisplayName")?.Value ?? User.Identity?.Name ?? "Usuario LAN";

            if (string.IsNullOrEmpty(localEvent.GoogleEventId))
            {
                // Never uploaded to Google, delete immediately
                _context.Events.Remove(localEvent);
            }
            else
            {
                // Mark for deletion from Google
                localEvent.SyncStatus = "PendingDelete";
                localEvent.LastModified = DateTime.UtcNow;
                localEvent.LastModifiedBy = displayName;
            }

            _context.SyncHistories.Add(new SyncHistory
            {
                EventId = localEvent.Id,
                EventTitle = localEvent.Title,
                ChangeDescription = "Evento eliminado localmente",
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = displayName,
                Source = "Local"
            });

            await _context.SaveChangesAsync();

            // Trigger sync immediately
            _ = Task.Run(() => _syncService.SyncAsync(default));

            return Ok(new { message = "Evento eliminado correctamente." });
        }

        // Get conflict and action history log (Admin/Supervisor only)
        [Authorize(Roles = "Admin,Supervisor")]
        [HttpGet("history")]
        public async Task<IActionResult> GetSyncHistory([FromQuery] int limit = 100)
        {
            var history = await _context.SyncHistories
                .OrderByDescending(h => h.ModifiedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(history);
        }
    }
}
