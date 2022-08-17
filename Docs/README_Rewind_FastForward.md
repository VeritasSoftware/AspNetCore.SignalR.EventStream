### Event Stream Replay Events

You can replay events in a stream for a subscriber.

By making a **http PUT** call to endpoint **api/EventStream/subscribers/{id}**.

You must provide the **Subscriber Id**.

![Event Stream replay Events](/Docs/Rewind_FastForward.jpg)

The body of the request must be like below:

```javascript
{
  "lastAccessedEventId": 11
}
```

**lastAccessedEventId** is the Event Id (integer) of the Event, you want to rewind/fast forward to.