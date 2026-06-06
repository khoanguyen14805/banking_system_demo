using Microsoft.EntityFrameworkCore;

namespace Banking_System.Models.Context
{
    public class BankDbContext : DbContext
    {
        public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<ATMCard> ATMCards { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Loan> Loans { get; set; }

        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // Configure UserRole Primary key
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // Configure relationship from UserRole to User
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            // Configure relationship from UserRole to Role
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // 1. Configure relationships and cascade delete behaviors to prevent cycles or multiple cascade paths
            // Prevent error "introducing FOREIGN KEY constraint ... may cause cycles or multiple cascade paths"
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.SourceBankAccount)
                .WithMany(b => b.SentTransactions)
                .HasForeignKey(t => t.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.DestinationBankAccount)
                .WithMany(b => b.ReceivedTransactions)
                .HasForeignKey(t => t.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Assure uniqueness for fields that should be unique (e.g., Username, Email, AccountNumber, CardNumber, CitizenId)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<BankAccount>()
                .HasIndex(b => b.AccountNumber)
                .IsUnique();

            modelBuilder.Entity<ATMCard>()
                .HasIndex(c => c.CardNumber)
                .IsUnique();

            modelBuilder.Entity<CustomerProfile>()
                .HasIndex(p => p.CitizenId)
                .IsUnique();

            // 3. Seed initial data for Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Customer" }
            );


        }
    }
}
