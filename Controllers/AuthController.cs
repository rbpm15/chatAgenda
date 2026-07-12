using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatAgenda.Data;
using ChatAgenda.Models;
using ChatAgenda.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ChatAgenda.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        public class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Usuario y contraseña requeridos." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.ToLower());
            if (user == null || !user.IsActive || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Usuario o contraseña incorrectos, o cuenta inactiva." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("DisplayName", user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Department", user.Department)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok(user);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Sesión cerrada correctamente." });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // 1. Get client IP address
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            string ipAddress = remoteIp?.ToString() ?? "127.0.0.1";
            
            // Normalize IPv6 loopback to IPv4
            if (ipAddress == "::1") ipAddress = "127.0.0.1";

            // 2. Determine if the request is local to the server host (loopback)
            bool isLocal = remoteIp == null || 
                           System.Net.IPAddress.IsLoopback(remoteIp) || 
                           ipAddress == "127.0.0.1";

            // 3. Find or create the user matching this IP address
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == ipAddress);
            
            if (user == null)
            {
                // Auto-create user for this machine IP
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = ipAddress,
                    DisplayName = "", // Starts empty so the UI knows to ask for a nickname
                    PasswordHash = "",
                    Role = isLocal ? "Admin" : "Employee",
                    Department = "General",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                // If it is localhost, ensure their role is Admin so they can configure sync
                if (isLocal && user.Role != "Admin")
                {
                    user.Role = "Admin";
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }

            // 4. Auto-sign-in the user via cookie so subsequent requests and SignalR are authenticated
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("DisplayName", user.DisplayName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Department", user.Department)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok(user);
        }
    }
}
