using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public class SqliteDbContext : DbContext
    {
        public virtual DbSet<Event> Events { get; set; }
        public virtual DbSet<Entities.EventStream> EventsStream { get; set; }
        public virtual DbSet<EventStreamAssociation> EventStreamsAssociation { get; set; }
        public virtual DbSet<EventStreamSubscriber> Subscribers { get; set; }

        public SqliteDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>().HasKey(e => e.Id);
            modelBuilder.Entity<Event>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();

            modelBuilder.Entity<Entities.EventStream>().HasKey(x => x.Id);
            modelBuilder.Entity<Entities.EventStream>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Entities.EventStream>().HasMany(x => x.Events).WithOne(x => x.Stream).HasForeignKey(x => x.StreamId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventStreamAssociation>().HasKey(x => x.Id);
            modelBuilder.Entity<EventStreamAssociation>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<EventStreamAssociation>().HasAlternateKey(x => new { x.StreamId, x.AssociatedStreamId });

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
