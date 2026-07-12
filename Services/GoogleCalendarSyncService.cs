using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ChatAgenda.Data;
using ChatAgenda.Hubs;
using ChatAgenda.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChatAgenda.Services
{
    public class GoogleCalendarSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GoogleCalendarSyncService> _logger;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(30);

        public GoogleCalendarSyncService(
            IServiceProvider serviceProvider,
            ILogger<GoogleCalendarSyncService> logger,
            IHubContext<ChatHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de sincronización de Google Calendar iniciado.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await SyncAsync(stoppingToken);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        _logger.LogError(ex, "Error durante la ejecución del ciclo de sincronización.");
                    }

                    await Task.Delay(_syncInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Servicio de sincronización de Google Calendar detenido de forma segura.");
            }
        }

        public async Task SyncAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var syncState = await db.GoogleSyncStates.FirstOrDefaultAsync(cancellationToken);
            if (syncState == null || !syncState.IsEnabled || string.IsNullOrEmpty(syncState.CredentialsJson))
            {
                // Sync not configured or disabled
                return;
            }

            try
            {
                var calendarService = GetCalendarService(syncState.CredentialsJson);
                
                // 1. Push local changes to Google
                bool pushedChanges = await PushLocalChangesToGoogleAsync(db, calendarService, syncState, cancellationToken);
                
                // 2. Pull remote changes from Google (incremental sync using SyncToken)
                bool pulledChanges = await PullGoogleChangesAsync(db, calendarService, syncState, cancellationToken);

                if (pushedChanges || pulledChanges)
                {
                    // Save last sync time
                    syncState.LastSyncTime = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);

                    // Notify clients to refresh their calendars
                    await _hubContext.Clients.All.SendAsync("CalendarUpdated");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sincronizar con Google Calendar API.");
            }
        }

        private CalendarService GetCalendarService(string credentialsJson)
        {
#pragma warning disable CS0618
            var credential = GoogleCredential.FromJson(credentialsJson)
                .CreateScoped(CalendarService.Scope.Calendar);
#pragma warning restore CS0618

            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ChatAgendaServer"
            });
        }

        private async Task<bool> PushLocalChangesToGoogleAsync(
            AppDbContext db, 
            CalendarService service, 
            GoogleSyncState syncState, 
            CancellationToken ct)
        {
            var pendingEvents = await db.Events
                .Where(e => e.SyncStatus == "PendingUpdate" || e.SyncStatus == "PendingDelete")
                .ToListAsync(ct);

            if (!pendingEvents.Any()) return false;

            bool anyChanges = false;
            foreach (var localEvent in pendingEvents)
            {
                try
                {
                    if (localEvent.SyncStatus == "PendingDelete")
                    {
                        if (!string.IsNullOrEmpty(localEvent.GoogleEventId))
                        {
                            await service.Events.Delete(syncState.CalendarId, localEvent.GoogleEventId).ExecuteAsync(ct);
                        }
                        db.Events.Remove(localEvent);
                        anyChanges = true;
                    }
                    else if (localEvent.SyncStatus == "PendingUpdate")
                    {
                        var googleEvent = new Google.Apis.Calendar.v3.Data.Event
                        {
                            Summary = localEvent.Title,
                            Description = $"{localEvent.Description}\n\nCreado localmente por {localEvent.CreatedByUserName}",
                            Start = new EventDateTime 
                            { 
                                DateTimeDateTimeOffset = localEvent.StartTime,
                                TimeZone = "UTC"
                            },
                            End = new EventDateTime 
                            { 
                                DateTimeDateTimeOffset = localEvent.EndTime,
                                TimeZone = "UTC"
                            }
                        };

                        if (string.IsNullOrEmpty(localEvent.GoogleEventId))
                        {
                            // Create new event in Google
                            var createdEvent = await service.Events.Insert(googleEvent, syncState.CalendarId).ExecuteAsync(ct);
                            localEvent.GoogleEventId = createdEvent.Id;
                        }
                        else
                        {
                            // Update existing event in Google
                            await service.Events.Update(googleEvent, syncState.CalendarId, localEvent.GoogleEventId).ExecuteAsync(ct);
                        }

                        localEvent.SyncStatus = "Synced";
                        localEvent.Version += 1;
                        localEvent.LastModified = DateTime.UtcNow;

                        db.SyncHistories.Add(new SyncHistory
                        {
                            EventId = localEvent.Id,
                            EventTitle = localEvent.Title,
                            ChangeDescription = "Sincronizado a Google Calendar",
                            ModifiedAt = DateTime.UtcNow,
                            ModifiedBy = localEvent.LastModifiedBy,
                            Source = "Local"
                        });

                        anyChanges = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al enviar evento local {localEvent.Id} a Google.");
                }
            }

            if (anyChanges)
            {
                await db.SaveChangesAsync(ct);
            }
            return anyChanges;
        }

        private async Task<bool> PullGoogleChangesAsync(
            AppDbContext db, 
            CalendarService service, 
            GoogleSyncState syncState, 
            CancellationToken ct)
        {
            var request = service.Events.List(syncState.CalendarId);
            
            // Set incremental sync if we have a token
            if (!string.IsNullOrEmpty(syncState.NextSyncToken))
            {
                request.SyncToken = syncState.NextSyncToken;
            }
            else
            {
                // Full sync setup (limit range for sanity: 30 days back and 365 days forward)
                request.TimeMinDateTimeOffset = DateTime.UtcNow.AddDays(-30);
                request.TimeMaxDateTimeOffset = DateTime.UtcNow.AddDays(365);
                // Expand recurring events into individual instances ONLY during full sync
                request.SingleEvents = true;
            }

            Events googleEvents;
            try
            {
                googleEvents = await request.ExecuteAsync(ct);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.Gone)
            {
                // Sync token is invalid/expired. Reset token and do a full resync.
                _logger.LogWarning("Google Calendar sync token expirado. Realizando sincronización completa...");
                syncState.NextSyncToken = null;
                return await PullGoogleChangesAsync(db, service, syncState, ct);
            }

            if (googleEvents.Items == null || !googleEvents.Items.Any())
            {
                if (syncState.NextSyncToken != googleEvents.NextSyncToken)
                {
                    syncState.NextSyncToken = googleEvents.NextSyncToken;
                    return true;
                }
                return false;
            }

            bool anyChanges = false;

            foreach (var gEvent in googleEvents.Items)
            {
                // Skip recurrent master events, focus on single instances or normal events
                if (gEvent.Recurrence != null) continue;

                var localEvent = await db.Events
                    .FirstOrDefaultAsync(e => e.GoogleEventId == gEvent.Id, ct);

                // Check for deletion from Google
                if (gEvent.Status == "cancelled")
                {
                    if (localEvent != null)
                    {
                        // If it's modified locally, check if local modified time is newer than cancellation time
                        if (localEvent.SyncStatus == "PendingUpdate")
                        {
                            // Local change wins, recreate on Google
                            localEvent.GoogleEventId = null; // Forces recreation on next push
                            db.SyncHistories.Add(new SyncHistory
                            {
                                EventId = localEvent.Id,
                                EventTitle = localEvent.Title,
                                ChangeDescription = "Conflicto: Restaurado localmente tras ser borrado en Google",
                                ModifiedAt = DateTime.UtcNow,
                                ModifiedBy = "Sistema (Sincronizador)",
                                Source = "Local"
                            });
                        }
                        else
                        {
                            db.SyncHistories.Add(new SyncHistory
                            {
                                EventId = localEvent.Id,
                                EventTitle = localEvent.Title,
                                ChangeDescription = "Eliminado desde Google Calendar",
                                ModifiedAt = DateTime.UtcNow,
                                ModifiedBy = "Google Calendar Mobile",
                                Source = "Google"
                            });
                            db.Events.Remove(localEvent);
                        }
                        anyChanges = true;
                    }
                    continue;
                }

                // Parse Google start/end times
                DateTime startTime = gEvent.Start?.DateTimeDateTimeOffset?.UtcDateTime 
                                    ?? DateTime.Parse(gEvent.Start?.Date ?? DateTime.UtcNow.ToString()).ToUniversalTime();
                DateTime endTime = gEvent.End?.DateTimeDateTimeOffset?.UtcDateTime 
                                  ?? DateTime.Parse(gEvent.End?.Date ?? DateTime.UtcNow.ToString()).ToUniversalTime();

                DateTime googleUpdated = gEvent.UpdatedDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow;

                if (localEvent == null)
                {
                    // New event from Google
                    var newEvent = new ChatAgenda.Models.Event
                    {
                        GoogleEventId = gEvent.Id,
                        Title = gEvent.Summary ?? "Evento sin título",
                        Description = gEvent.Description ?? string.Empty,
                        StartTime = startTime,
                        EndTime = endTime,
                        CreatedByUserId = "GoogleSync",
                        CreatedByUserName = "Google Mobile User",
                        LastModified = googleUpdated,
                        LastModifiedBy = "Google Mobile User",
                        SyncStatus = "Synced",
                        Version = 1
                    };

                    db.Events.Add(newEvent);
                    anyChanges = true;

                    // Log history after saving to get Id, but since we save in batch we can just log a message
                }
                else
                {
                    // Existing event updated in Google. Check for conflicts.
                    if (localEvent.SyncStatus == "PendingUpdate")
                    {
                        // Conflict: modified in both places. Last modified wins.
                        if (googleUpdated > localEvent.LastModified)
                        {
                            // Google wins
                            localEvent.Title = gEvent.Summary ?? localEvent.Title;
                            localEvent.Description = gEvent.Description ?? localEvent.Description;
                            localEvent.StartTime = startTime;
                            localEvent.EndTime = endTime;
                            localEvent.SyncStatus = "Synced";
                            localEvent.LastModified = googleUpdated;
                            localEvent.LastModifiedBy = "Google Mobile User";
                            localEvent.Version += 1;

                            db.SyncHistories.Add(new SyncHistory
                            {
                                EventId = localEvent.Id,
                                EventTitle = localEvent.Title,
                                ChangeDescription = "Conflicto resuelto: Ganó Google Calendar (más reciente)",
                                ModifiedAt = DateTime.UtcNow,
                                ModifiedBy = "Sistema (Sincronizador)",
                                Source = "Google"
                            });
                        }
                        else
                        {
                            // Local wins. We leave it as PendingUpdate so it pushes on next cycle.
                            db.SyncHistories.Add(new SyncHistory
                            {
                                EventId = localEvent.Id,
                                EventTitle = localEvent.Title,
                                ChangeDescription = "Conflicto resuelto: Ganó local (más reciente). Subiendo a Google...",
                                ModifiedAt = DateTime.UtcNow,
                                ModifiedBy = "Sistema (Sincronizador)",
                                Source = "Local"
                            });
                        }
                    }
                    else
                    {
                        // No conflict, just update local
                        localEvent.Title = gEvent.Summary ?? localEvent.Title;
                        localEvent.Description = gEvent.Description ?? localEvent.Description;
                        localEvent.StartTime = startTime;
                        localEvent.EndTime = endTime;
                        localEvent.SyncStatus = "Synced";
                        localEvent.LastModified = googleUpdated;
                        localEvent.LastModifiedBy = "Google Mobile User";
                        localEvent.Version += 1;

                        db.SyncHistories.Add(new SyncHistory
                        {
                            EventId = localEvent.Id,
                            EventTitle = localEvent.Title,
                            ChangeDescription = "Modificado desde Google Calendar",
                            ModifiedAt = DateTime.UtcNow,
                            ModifiedBy = "Google Mobile User",
                            Source = "Google"
                        });
                    }
                    anyChanges = true;
                }
            }

            // Save the next sync token
            syncState.NextSyncToken = googleEvents.NextSyncToken;
            
            if (anyChanges)
            {
                await db.SaveChangesAsync(ct);
            }

            return anyChanges;
        }
    }
}
