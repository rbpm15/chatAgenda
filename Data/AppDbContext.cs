using Microsoft.EntityFrameworkCore;
using ChatAgenda.Models;
using ChatAgenda.Services;
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.IsKeyless) continue;

                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

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
