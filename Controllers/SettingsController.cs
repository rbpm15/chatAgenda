using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAgenda.Data;
using ChatAgenda.Models;
using ChatAgenda.Services;
using System.Threading.Tasks;
using System;

namespace ChatAgenda.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly GoogleCalendarSyncService _syncService;

        public SettingsController(AppDbContext context, GoogleCalendarSyncService syncService)
        {
            _context = context;
            _syncService = syncService;
        }

        // Get current sync configuration status
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var syncState = await _context.GoogleSyncStates.FirstOrDefaultAsync();
            if (syncState == null)
            {
                return NotFound(new { message = "Configuración no encontrada." });
            }

            return Ok(new
            {
                syncState.CalendarId,
                syncState.IsEnabled,
                syncState.LastSyncTime,
                HasCredentials = !string.IsNullOrEmpty(syncState.CredentialsJson)
            });
        }

        public class SaveSettingsRequest
        {
            public string CalendarId { get; set; } = "primary";
            public bool IsEnabled { get; set; }
            public string? CredentialsJson { get; set; } // Null if not changing
        }

        // Save sync configuration
        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] SaveSettingsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CalendarId))
            {
                return BadRequest(new { message = "El ID del calendario es requerido." });
            }

            var syncState = await _context.GoogleSyncStates.FirstOrDefaultAsync();
            if (syncState == null)
            {
                syncState = new GoogleSyncState { Id = 1 };
                _context.GoogleSyncStates.Add(syncState);
            }

            syncState.CalendarId = request.CalendarId.Trim();
            syncState.IsEnabled = request.IsEnabled;

            if (!string.IsNullOrEmpty(request.CredentialsJson))
            {
                // Validate if it is correct JSON
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(request.CredentialsJson);
                    syncState.CredentialsJson = request.CredentialsJson;
                }
                catch
                {
                    return BadRequest(new { message = "Las credenciales de Google deben ser un JSON válido." });
                }
            }

            // If toggling on, reset sync token to force initial full sync and get updated events
            if (request.IsEnabled && !syncState.IsEnabled)
            {
                syncState.NextSyncToken = null;
            }

            await _context.SaveChangesAsync();

            // Trigger sync in background if enabled
            if (syncState.IsEnabled && !string.IsNullOrEmpty(syncState.CredentialsJson))
            {
                _ = Task.Run(() => _syncService.SyncAsync(default));
            }

            return Ok(new
            {
                syncState.CalendarId,
                syncState.IsEnabled,
                syncState.LastSyncTime,
                HasCredentials = !string.IsNullOrEmpty(syncState.CredentialsJson)
            });
        }

        // Manually trigger a synchronization cycle
        [HttpPost("sync-now")]
        public async Task<IActionResult> TriggerSyncNow()
        {
            var syncState = await _context.GoogleSyncStates.FirstOrDefaultAsync();
            if (syncState == null || !syncState.IsEnabled || string.IsNullOrEmpty(syncState.CredentialsJson))
            {
                return BadRequest(new { message = "La sincronización está desactivada o no configurada." });
            }

            // Force full resync by clearing the sync token (optional: could do normal sync. Let's do normal sync unless forced)
            // For manual trigger, let's keep the sync token to prevent huge overhead, but do it immediately
            await _syncService.SyncAsync(default);

            return Ok(new { message = "Sincronización manual ejecutada." });
        }
    }
}
