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
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // Get active users list for chat contact lists
        [HttpGet]
        public async Task<IActionResult> GetActiveUsers()
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var users = await _context.Users
                .Where(u => u.IsActive && u.Id != currentUserId)
                .OrderBy(u => u.DisplayName)
                .ToListAsync();

            return Ok(users);
        }

        // Get all users (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Username)
                .ToListAsync();
            return Ok(users);
        }

        public class CreateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Role { get; set; } = "Employee";
            public string Department { get; set; } = "General";
        }

        // Create new user (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Usuario y contraseña requeridos." });
            }

            var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
            if (exists)
            {
                return BadRequest(new { message = "El nombre de usuario ya está registrado." });
            }

            var user = new User
            {
                Username = request.Username.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username : request.DisplayName.Trim(),
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                Role = request.Role,
                Department = request.Department,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }

        public class UpdateUserRequest
        {
            public string DisplayName { get; set; } = string.Empty;
            public string Role { get; set; } = "Employee";
            public string Department { get; set; } = "General";
            public bool IsActive { get; set; } = true;
        }

        // Update user properties (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "Usuario no encontrado." });

            // Prevent self-deactivation or self-demotion from Admin
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (user.Id == currentUserId && (!request.IsActive || request.Role != "Admin"))
            {
                return BadRequest(new { message = "No puedes desactivarte o cambiar tu propio rol de Administrador." });
            }

            user.DisplayName = request.DisplayName.Trim();
            user.Role = request.Role;
            user.Department = request.Department;
            user.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return Ok(user);
        }

        public class ChangePasswordRequest
        {
            public string? CurrentPassword { get; set; } // Nullable if admin resets
            public string NewPassword { get; set; } = string.Empty;
        }

        // Change password (self or admin override)
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (currentUserId != id && !isAdmin)
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "Usuario no encontrado." });

            // If not admin, must provide current password
            if (!isAdmin)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword) || 
                    !PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "La contraseña actual es incorrecta." });
                }
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 4)
            {
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 4 caracteres." });
            }

            user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada exitosamente." });
        }

        public class UpdateProfileRequest
        {
            public string DisplayName { get; set; } = string.Empty;
            public string Department { get; set; } = "General";
        }

        // Update current user's profile display name & department (Self only)
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null) return NotFound(new { message = "Usuario no encontrado." });

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return BadRequest(new { message = "El nombre de visualización no puede estar vacío." });
            }

            user.DisplayName = request.DisplayName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Department))
            {
                user.Department = request.Department.Trim();
            }

            await _context.SaveChangesAsync();
            return Ok(user);
        }
    }
}
