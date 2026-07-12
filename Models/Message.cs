using System;

namespace ChatAgenda.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = string.Empty;
        
        // Nullable for group chats
        public string? ReceiverId { get; set; }
        
        // Nullable for direct chats
        public string? GroupId { get; set; } // e.g., "Ventas", "General", "Soporte"
        
        public string Text { get; set; } = string.Empty;
        
        // Optional file sharing
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
