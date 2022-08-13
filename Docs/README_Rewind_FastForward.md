### Event Stream Rewind/Fast Forward

You can rewind/fast forward events in a stream for a subscriber.

By making a **http PUT** call to endpoint **api/EventStream/subscribers/{id}**.

You must provide the **Subscriber Id**.

![Event Stream rewind/fast forward](/Docs/Rewind_FastForward.jpg)

The body of the request can be like below:

```javascript
{
  "lastAccessedEventAt": "2022-08-13T02:31:19.973Z",
  "lastAccessedEventAtFromEventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**lastAccessedEventAtFromEventId** is the Event Id (Guid) of the Event, you want to rewind/fast forward to.

**lastAccessedEventAt** is the timestamp, you want to rewind/fast forward to.

Either one of these 2 properties must be specified in the body.