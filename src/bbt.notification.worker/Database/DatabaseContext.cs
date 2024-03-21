using Microsoft.EntityFrameworkCore;

public class DatabaseContext : DbContext
{
    public DbSet<Source> Sources { get; set; }
    public DbSet<Consumer> Consumers { get; set; }
    public DbSet<SourceService> SourceServices { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<MessageNotificationLog> MessageNotificationLogs { get; set; }
    public DbSet<ReminderDefinition> ReminderDefinitions { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<ProductCode> ProductCodes { get; set; }
    public string DbPath { get; private set; }
    public DatabaseContext()
    {
        DbPath = $"notification.db";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{GetEnviroment()}.json", false, true)
                .AddEnvironmentVariables()
                .Build();

        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        options.EnableSensitiveDataLogging();
    }

    string? GetEnviroment()
    {
        return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Consumer>().OwnsOne(e => e.Phone);

        builder.Entity<Source>()
            .HasOne(s => s.Parent)
            .WithMany(s => s.Children)
            .HasForeignKey(s => s.ParentId);

        builder.Entity<Consumer>()
           .Property<long>("$id")
           .ValueGeneratedOnAdd();

        builder.Entity<Consumer>()
            .HasKey(c => c.Id)
            .IsClustered(false);

        builder.Entity<Consumer>()
          .HasIndex("$id")
          .IsUnique()
          .IsClustered(true);

        builder.Entity<SourceParameter>().HasKey(pc => new { pc.SourceId, pc.JsonPath, pc.Type });

        builder.Entity<Consumer>(c =>
        {
            c.HasData(
            new
            {
                Id = new Guid("2e15d57c-26e3-4e78-94f9-8649b3302555"),
                Client = (long)0,
                User = (long)0,
                SourceId = 1,
                IsPushEnabled = false,
                IsSmsEnabled = true,
                IsEmailEnabled = false,
                IsStaff = false,
                DefinitionCode = "accountMoneyEntry",
                DeviceKey = "",
                Email = "",
                Filter = "",

            });

        });

        builder.Entity<Log>()
                    .Property(f => f.Id)
                    .ValueGeneratedOnAdd();

        builder.Entity<MessageNotificationLog>()
                  .Property(f => f.Id)
                  .ValueGeneratedOnAdd();

        builder.Entity<ReminderDefinition>()
                 .Property(f => f.Id)
                 .ValueGeneratedOnAdd();

        builder.Entity<NotificationLog>()
           .Property(f => f.Id)
           .ValueGeneratedOnAdd();

        builder.Entity<NotificationLog>()
                 .ToTable("NotificationLogs", b => b.IsTemporal());

        builder.Entity<ProductCode>()
                  .Property(f => f.Id)
                  .ValueGeneratedOnAdd();
    }
}