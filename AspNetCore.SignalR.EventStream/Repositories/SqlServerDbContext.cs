using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public class SqlServerDbContext : DbContext, IDbContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Entities.EventStream> EventsStream { get; set; }
        public DbSet<EventStreamAssociation> EventStreamsAssociation { get; set; }
        public DbSet<EventStreamSubscriber> Subscribers { get; set; }

        public SqlServerDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>().HasKey(e => e.Id);
            modelBuilder.Entity<Event>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();

            modelBuilder.Entity<Entities.EventStream>().HasKey(x => x.Id);
            modelBuilder.Entity<Entities.EventStream>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();

            modelBuilder.Entity<EventStreamAssociation>().HasKey(x => x.Id);
            modelBuilder.Entity<EventStreamAssociation>().Property(x => x.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<EventStreamAssociation>().HasAlternateKey(x => new { x.StreamId, x.AssociatedStreamId });
            modelBuilder.Entity<EventStreamAssociation>().HasOne(x => x.AssociatedStream).WithMany().OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EventStreamAssociation>().HasOne(x => x.Stream).WithMany().OnDelete(DeleteBehavior.NoAction);
        }
    }
}
