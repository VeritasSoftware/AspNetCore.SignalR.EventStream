### Event Sourcing

Event sourcing stores the state of a database object as a sequence of events – essentially a new event for each time the object changed state, from the beginning of the object’s existence.

**Event Sourcing Pattern** is useful in many scenarios.

Example:

Microservice architecture is used to structure an application as a set of micro services.  

These micro services are loosly coupled and each service can be developed independently.

and each service has it's own database. 

Then how to implement a transaction which spans multiple services.

In such a scenario, 

Event Sourcing Pattern can be used for inter service communication. 

In this type of communication, each service persists the events in event database for every action taken.

So, the state changes are also tracked.

Each service can subscribe to these events and perform any action needed. 

Consider a case of Order Service and Procurement Service. 

A procurement service can subscribe to events published by order service and perform any actions as needed.

![Event Sourcing Pattern UML](/Docs/EventSourcingPattern.jpg)