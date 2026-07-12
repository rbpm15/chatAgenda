using Microsoft.EntityFrameworkCore;
using ChatAgenda.Models;
using ChatAgenda.Services;
using System;

namespace ChatAgenda.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<SyncHistory> SyncHistories { get; set; } = null!;
        public DbSet<GoogleSyncState> GoogleSyncStates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique Username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Seed initial users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = "u-admin-1111",
                    Username = "admin",
                    DisplayName = "Administrador de Oficina",
                    PasswordHash = PasswordHasher.HashPassword("admin"),
                    Role = "Admin",
                    Department = "Sistemas",
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime()
                },
                new User
                {
                    Id = "u-ana-2222",
                    Username = "ana",
                    DisplayName = "Ana López",
                    PasswordHash = PasswordHasher.HashPassword("ana123"),
                    Role = "Supervisor",
                    Department = "Ventas",
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime()
                },
                new User
                {
                    Id = "u-juan-3333",
                    Username = "juan",
                    DisplayName = "Juan Pérez",
                    PasswordHash = PasswordHasher.HashPassword("juan123"),
                    Role = "Employee",
                    Department = "Ventas",
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime()
                },
                new User
                {
                    Id = "u-pedro-4444",
                    Username = "pedro",
                    DisplayName = "Pedro Gómez",
                    PasswordHash = PasswordHasher.HashPassword("pedro123"),
                    Role = "Employee",
                    Department = "Administración",
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime()
                }
            );

            // Seed default Google Calendar sync state
            modelBuilder.Entity<GoogleSyncState>().HasData(
                new GoogleSyncState
                {
                    Id = 1,
                    CalendarId = "primary",
                    IsEnabled = false,
                    LastSyncTime = DateTime.Parse("2026-01-01T00:00:00Z").ToUniversalTime()
                }
            );
        }
    }
}
