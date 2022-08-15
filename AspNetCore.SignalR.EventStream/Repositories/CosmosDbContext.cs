using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public class CosmosDbContext : DbContext
    {
        public DbSet<CosmosEvent> Events { get; set; }
        public DbSet<Entities.EventStream> EventsStream { get; set; }
        public DbSet<EventStreamAssociation> EventStreamsAssociation { get; set; }
        public DbSet<EventStreamSubscriber> Subscribers { get; set; }

        public CosmosDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultContainer("EventStreamContainer");

            modelBuilder.Ignore<CosmosEvent>();
            modelBuilder.Ignore<Entities.EventStream>();

            modelBuilder.Entity<CosmosEvent>(b => {
                b.HasNoDiscriminator();
                b.ToContainer(nameof(Events));
                b.HasPartitionKey(o => o.PartitionKey);
                b.HasKey(o => o.Id);
            });

            modelBuilder.Entity<Entities.EventStream>(b =>
            {
                b.HasNoDiscriminator();
                b.ToContainer(nameof(EventsStream));
                b.HasPartitionKey(o => o.Name);
                b.HasKey(o => o.Id);
            });

            modelBuilder.Entity<EventStreamAssociation>(b =>
            {
                b.HasNoDiscriminator();
                b.ToContainer(nameof(EventStreamsAssociation));
                b.HasKey(o => o.Id);
            });

            modelBuilder.Entity<EventStreamSubscriber>(b =>
            {
                b.HasNoDiscriminator();
                b.ToContainer(nameof(Subscribers));
                b.HasKey(o => o.Id);
            });
        }
    }
}
