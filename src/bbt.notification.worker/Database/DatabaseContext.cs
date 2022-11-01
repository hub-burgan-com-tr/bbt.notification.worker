


using Microsoft.EntityFrameworkCore;

public class DatabaseContext : DbContext
    {
        public DbSet<Source> Sources { get; set; }
        public DbSet<Consumer> Consumers { get; set; }
        public DbSet<SourceService> SourceServices { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<MessageNotificationLog> MessageNotificationLogs { get; set; }
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

            /* TODO: SQL Edge not supporting memory optimized tables
            builder.Entity<Consumer>(c =>
            {
                c.OwnsOne(e => e.Phone).IsMemoryOptimized();
                c.IsMemoryOptimized();
            });
            */

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

            builder.Entity<Source>().HasData(
              new Source
              {
                  Id = 1,
                  Title_TR = "CashBackTR",
                  Title_EN = "CashBackEN",
                  DisplayType = SourceDisplayType.NotDisplay,
                  Topic = "CAMPAIGN_CASHBACK_ACCOUNTING_INFO",
                  KafkaUrl = "x",
                  ApiKey = "",
                  Secret = "",
                  PushServiceReference = "notify",
                  SmsServiceReference = "",
                  EmailServiceReference = "",
                  ClientIdJsonPath="",
                  ParentId=1,
                  KafkaCertificate="x",
              });

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
                    DeviceKey = "",
                    Email = "",
                    Filter = "",
                    IsStaff=false,

                });
               // c.OwnsOne(e => e.Phone).HasData(new { ConsumerId = new Guid("2e15d57c-26e3-4e78-94f9-8649b3302555"), CountryCode = 90, Prefix = 530, Number = 3855206 });
            });

            builder.Entity<Consumer>(c =>
            {
                c.HasData(
                new Consumer
                {
                    Id = new Guid("3e15d57c-26e3-4e78-94f9-8649b3302555"),
                    Client = (long)0,
                    User = (long)0,
                    SourceId = 1,
                    Filter = "",
                    IsPushEnabled = false,
                    IsSmsEnabled = true,
                    IsEmailEnabled = false,
                    DeviceKey="",
                    Email="",
                    IsStaff = false,

                });
              //  c.OwnsOne(e => e.Phone).HasData(new { ConsumerId = new Guid("3e15d57c-26e3-4e78-94f9-8649b3302555"), CountryCode = 90, Prefix = 530, Number = 3855206 });
            });
            builder.Entity<SourceService>().HasData(
              new SourceService
              {
                  Id = 1,
                  SourceId = 1,
                  ServiceUrl = "X",

              });
            builder.Entity<Log>()
                        .Property(f => f.Id)
                        .ValueGeneratedOnAdd();

            builder.Entity<MessageNotificationLog>()
                      .Property(f => f.Id)
                      .ValueGeneratedOnAdd();

        }
    }