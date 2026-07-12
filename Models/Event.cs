using System;

namespace ChatAgenda.Models
{
    public class Event
    {
        public int Id { get; set; }
        
        // Maps to Google Calendar's event ID
        public string? GoogleEventId { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string LastModifiedBy { get; set; } = string.Empty;
        
        // SyncStatus can be: "Synced", "PendingUpdate", "PendingDelete", "Conflict"
        public string SyncStatus { get; set; } = "Synced";
        
        // Version used for optimistic concurrency / sync conflict management
        public int Version { get; set; } = 1;
    }
}
