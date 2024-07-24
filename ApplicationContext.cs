using Microsoft.EntityFrameworkCore;

namespace appman.DataModels;

public class ApplicationContext : DbContext {
    public DbSet<User> Users { get; private set; } = null!;
    public DbSet<Credentials> Credentials { get; private set; } = null!;
    public DbSet<Invite> Invites { get; private set; } = null!;
    public DbSet<Application> Applications { get; private set; } = null!;
    public DbSet<AppBranch> Branches { get; private set; } = null!;
    public DbSet<AppAccess> AppAccesses { get; private set; } = null!;
    public DbSet<AppBuild> AppBuilds { get; private set; } = null!;
    public DbSet<CrashLog> CrashLogs { get; private set; } = null!;

    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlite("Data Source=appman.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>().HasData(
            User.GetSuperUser(Program.Setting["SuperUserName"])
        );
        modelBuilder.Entity<Credentials>().HasData(
            new Credentials { Id = 1, Password = Cryptography.ComputeSHA256(Program.Setting["SuperUserPassword"]) }
        );
    }
}