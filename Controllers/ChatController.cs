using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAgenda.Data;
using ChatAgenda.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace ChatAgenda.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ChatController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Get direct message history
        [HttpGet("direct/{contactId}")]
        public async Task<IActionResult> GetDirectMessages(string contactId, [FromQuery] int limit = 100)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == contactId) ||
                            (m.SenderId == contactId && m.ReceiverId == currentUserId))
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();

            // Reverse to chronological order
            messages.Reverse();
            return Ok(messages);
        }

        // Get group message history
        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetGroupMessages(string groupId, [FromQuery] int limit = 100)
        {
            var messages = await _context.Messages
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();

            messages.Reverse();
            return Ok(messages);
        }

        // Upload attachment
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Ningún archivo fue seleccionado." });
            }

            try
            {
                var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatAgenda");
                var uploadsFolder = Path.Combine(dataDir, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate safe unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var relativePath = $"/uploads/{uniqueFileName}";
                return Ok(new 
                { 
                    fileName = file.FileName,
                    filePath = relativePath 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al subir el archivo: {ex.Message}" });
            }
        }

        // Get list of active group names from database
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _context.Messages
                .Where(m => m.GroupId != null && m.GroupId != "")
                .Select(m => m.GroupId)
                .Distinct()
                .ToListAsync();

            return Ok(groups);
        }
    }
}
