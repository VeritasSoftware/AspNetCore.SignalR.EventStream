using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AspNetCore.SignalR.EventStream
{
    public class SqliteDbContext : DbContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Entities.EventStream> EventsStream { get; set; }
        public DbSet<EventStreamSubscriber> Subscribers { get; set; }

        public string DbPath { get; }

        public SqliteDbContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "eventstream.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
                => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>().HasKey(e => e.Id);
            modelBuilder.Entity<Event>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();

            modelBuilder.Entity<Entities.EventStream>().HasKey(x => x.Id);
            modelBuilder.Entity<Entities.EventStream>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                            || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}
