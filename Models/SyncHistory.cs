using System;

namespace ChatAgenda.Models
{
    public class SyncHistory
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string ChangeDescription { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string ModifiedBy { get; set; } = string.Empty;
        
        // Indicates if it was modified via "Google" or "Local"
        public string Source { get; set; } = "Local"; 
    }
}
