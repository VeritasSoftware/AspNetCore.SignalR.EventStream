### Event Stream Replay Events

You can replay events in a stream for a subscriber.

By making a **http PUT** call to endpoint **api/EventStream/subscribers/{id}**.

You must provide the **Subscriber Id**.

![Event Stream replay Events](/Docs/Rewind_FastForward.jpg)

The body of the request must be like below:

```javascript
{
  "lastAccessedFromEventId": 17,
  "lastAccessedToEventId": 19
}
```

**lastAccessedFromEventId** is the Event Id (integer) of the Event, you want to replay from. The value 0 means start of stream.

**lastAccessedToEventId** is the Event Id (integer) of the Event, you want to replay to. If not provided means till end of stream.