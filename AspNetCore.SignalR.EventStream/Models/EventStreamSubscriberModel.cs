﻿namespace AspNetCore.SignalR.EventStream.Models
{
    public class EventStreamSubscriberModel
    {
        public long Id { get; set; }
        public long StreamId { get; set; }
        public virtual EventStreamModel Stream { get; set; }
        public Guid SubscriberId { get; set; }
        public string? ConnectionId { get; set; }
        public string? ReceiveMethod { get; set; }
        public long? LastAccessedCurrentEventId { get; set; }
        public long? LastAccessedFromEventId { get; set; }
        public long? LastAccessedToEventId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public class EventStreamSubscriberModelResult
    {
        public long Id { get; set; }
        public long StreamId { get; set; }
        public virtual EventStreamModelResult Stream { get; set; }
        public Guid SubscriberId { get; set; }
        public string? ConnectionId { get; set; }
        public string? ReceiveMethod { get; set; }
        public long? LastAccessedEventId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
