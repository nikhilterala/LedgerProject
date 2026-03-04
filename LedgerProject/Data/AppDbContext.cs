using LedgerProject.Models;
using Microsoft.EntityFrameworkCore;

namespace LedgerProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<LedgerEntry> LedgerEntries { get; set; }
        public DbSet<BalanceSnapshot> BalanceSnapshots { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<BalanceSnapshot>()
                .HasKey(b => b.AccountId);

            modelBuilder.Entity<BalanceSnapshot>()
            .HasOne(bs => bs.Account)
            .WithOne()
            .HasForeignKey<BalanceSnapshot>(bs => bs.AccountId);

            modelBuilder.Entity<Account>()
            .Property(a => a.Status)
            .HasConversion<int>();

            modelBuilder.Entity<AuditEvent>()
            .HasKey(a => a.EventId);

            modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
        }
    }
}
