using System;

namespace ChatAgenda.Models
{
    public class GoogleSyncState
    {
        public int Id { get; set; }
        
        // Google Calendar ID (e.g., "primary" or "agenda.empresa@gmail.com")
        public string CalendarId { get; set; } = "primary";
        
        // Token received from Google to perform incremental syncs
        public string? NextSyncToken { get; set; }
        
        public DateTime LastSyncTime { get; set; } = DateTime.MinValue;
        
        // Indicates if Google integration is currently active and authenticated
        public bool IsEnabled { get; set; } = false;
        
        // Store service account credentials path or encrypted json locally
        public string? CredentialsJson { get; set; }
    }
}
