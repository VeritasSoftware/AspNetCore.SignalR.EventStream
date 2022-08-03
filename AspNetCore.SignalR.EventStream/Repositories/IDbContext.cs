using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public interface IDbContext
    {
        DbSet<Event> Events { get; set; }
        DbSet<Entities.EventStream> EventsStream { get; set; }
        DbSet<EventStreamAssociation> EventStreamsAssociation { get; set; }
        DbSet<EventStreamSubscriber> Subscribers { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        DatabaseFacade Database { get; }
        EntityEntry<TEntity> Update<TEntity>(TEntity entity)
            where TEntity: class;
        EntityEntry<TEntity> Remove<TEntity>(TEntity entity)
            where TEntity:class;
    }
}
