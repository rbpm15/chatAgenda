using Microsoft.AspNetCore.SignalR;
using ChatAgenda.Data;
using ChatAgenda.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace ChatAgenda.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        
        // Maps connection ID to user details (UserId, Username, DisplayName)
        private static readonly ConcurrentDictionary<string, (string UserId, string Username, string DisplayName)> UserConnections = new();

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        // Returns list of currently connected user IDs
        public static string[] GetOnlineUsers()
        {
            return UserConnections.Values.Select(v => v.UserId).Distinct().ToArray();
        }

        public async Task RegisterUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.IsActive)
            {
                UserConnections[Context.ConnectionId] = (user.Id, user.Username, user.DisplayName);
                
                // Add to groups based on role and department
                await Groups.AddToGroupAsync(Context.ConnectionId, "General");
                if (!string.IsNullOrEmpty(user.Department))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, user.Department);
                }

                // Notify all about updated user status
                await Clients.All.SendAsync("UserPresenceUpdate", GetOnlineUsers());
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (UserConnections.TryRemove(Context.ConnectionId, out var userInfo))
            {
                // Notify all about updated user status
                await Clients.All.SendAsync("UserPresenceUpdate", GetOnlineUsers());
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendDirectMessage(string receiverId, string text, string? fileName = null, string? filePath = null)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out var sender))
            {
                var message = new Message
                {
                    SenderId = sender.UserId,
                    SenderDisplayName = sender.DisplayName,
                    ReceiverId = receiverId,
                    Text = text,
                    FileName = fileName,
                    FilePath = filePath,
                    Timestamp = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Find receiver's connection IDs
                var receiverConnections = UserConnections
                    .Where(x => x.Value.UserId == receiverId)
                    .Select(x => x.Key)
                    .ToList();

                // Send to receiver connections
                foreach (var connId in receiverConnections)
                {
                    await Clients.Client(connId).SendAsync("ReceiveMessage", message);
                }

                // Send back to all connections of the sender (to sync multiple tabs/sessions)
                var senderConnections = UserConnections
                    .Where(x => x.Value.UserId == sender.UserId)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var connId in senderConnections)
                {
                    await Clients.Client(connId).SendAsync("ReceiveMessage", message);
                }
            }
        }

        public async Task SendGroupMessage(string groupName, string text, string? fileName = null, string? filePath = null)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out var sender))
            {
                var message = new Message
                {
                    SenderId = sender.UserId,
                    SenderDisplayName = sender.DisplayName,
                    GroupId = groupName,
                    Text = text,
                    FileName = fileName,
                    FilePath = filePath,
                    Timestamp = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Broadcast to the SignalR group
                await Clients.Group(groupName).SendAsync("ReceiveMessage", message);
            }
        }

        public async Task SendTypingStatus(string receiverId, bool isTyping)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out var sender))
            {
                var receiverConnections = UserConnections
                    .Where(x => x.Value.UserId == receiverId)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var connId in receiverConnections)
                {
                    await Clients.Client(connId).SendAsync("UserTyping", sender.UserId, isTyping);
                }
            }
        }

        public async Task SendGroupTypingStatus(string groupName, bool isTyping)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out var sender))
            {
                // Send to group excluding sender
                await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("GroupTyping", groupName, sender.UserId, sender.DisplayName, isTyping);
            }
        }

        public async Task JoinGroupChannel(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
    }
}
