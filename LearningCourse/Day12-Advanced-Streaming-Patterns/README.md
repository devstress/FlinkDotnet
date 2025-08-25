# Day 12: Advanced Streaming Patterns - Event Sourcing, CQRS, and Sagas

## Overview
Master advanced architectural patterns for complex business workflows including Event Sourcing, Command Query Responsibility Segregation (CQRS), and Saga patterns in distributed streaming systems.

## Learning Objectives
- Implement Event Sourcing with streaming event stores
- Design CQRS architectures with real-time read models
- Build distributed saga patterns for long-running transactions
- Create event-driven microservices with eventual consistency
- Implement complex business workflows with compensation patterns

## Real-World Context
Microsoft's Xbox Live platform uses event sourcing to track 3 billion gaming events daily, CQRS for real-time leaderboards and achievements, and saga patterns for complex multiplayer game orchestration. Their architecture handles millions of concurrent players with sub-100ms response times.

## Technical Deep Dive

### Event Sourcing with Streaming Event Store
```csharp
// Netflix-style event sourcing for user viewing history
public class ViewingHistoryEventStore : KeyedProcessFunction<string, ViewingEvent, ViewingHistorySnapshot>
{
    private ListState<ViewingEvent> eventStore;
    private ValueState<ViewingHistorySnapshot> snapshotState;
    private ValueState<long> lastSnapshotVersion;
    
    private readonly int snapshotFrequency = 100; // Snapshot every 100 events
    
    public override void Open(Configuration parameters)
    {
        var eventStoreDescriptor = new ListStateDescriptor<ViewingEvent>(
            "viewing-events", TypeInformation.Of<ViewingEvent>());
        eventStore = GetRuntimeContext().GetListState(eventStoreDescriptor);
        
        var snapshotDescriptor = new ValueStateDescriptor<ViewingHistorySnapshot>(
            "viewing-snapshot", TypeInformation.Of<ViewingHistorySnapshot>());
        snapshotState = GetRuntimeContext().GetState(snapshotDescriptor);
        
        var versionDescriptor = new ValueStateDescriptor<long>(
            "snapshot-version", TypeInformation.Of<long>());
        lastSnapshotVersion = GetRuntimeContext().GetState(versionDescriptor);
    }
    
    public override void ProcessElement(
        ViewingEvent viewingEvent, 
        Context context, 
        ICollector<ViewingHistorySnapshot> output)
    {
        var userId = context.GetCurrentKey();
        
        // Append event to event store (immutable log)
        eventStore.Add(viewingEvent);
        
        // Get current snapshot or rebuild from events
        var currentSnapshot = snapshotState.Value() ?? RebuildFromEvents();
        
        // Apply event to snapshot (projection)
        var updatedSnapshot = ApplyEvent(currentSnapshot, viewingEvent);
        
        // Check if we should create a new snapshot
        var eventCount = GetEventCount();
        var lastSnapshot = lastSnapshotVersion.Value();
        
        if (eventCount - lastSnapshot >= snapshotFrequency)
        {
            // Create snapshot and compact event store
            snapshotState.Update(updatedSnapshot);
            lastSnapshotVersion.Update(eventCount);
            
            // Keep only events after snapshot for recovery
            CompactEventStore(eventCount - 10); // Keep last 10 events for safety
        }
        
        // Emit updated projection
        output.Collect(updatedSnapshot);
        
        // Publish domain events for other bounded contexts
        PublishDomainEvents(viewingEvent, updatedSnapshot);
    }
    
    private ViewingHistorySnapshot ApplyEvent(ViewingHistorySnapshot snapshot, ViewingEvent viewingEvent)
    {
        return viewingEvent.EventType switch
        {
            ViewingEventType.VideoStarted => snapshot.WithVideoStarted(viewingEvent),
            ViewingEventType.VideoCompleted => snapshot.WithVideoCompleted(viewingEvent),
            ViewingEventType.VideoPaused => snapshot.WithVideoPaused(viewingEvent),
            ViewingEventType.VideoResumed => snapshot.WithVideoResumed(viewingEvent),
            ViewingEventType.VideoSeeked => snapshot.WithVideoSeeked(viewingEvent),
            _ => snapshot
        };
    }
    
    private ViewingHistorySnapshot RebuildFromEvents()
    {
        var events = eventStore.Get();
        var snapshot = new ViewingHistorySnapshot();
        
        foreach (var evt in events.OrderBy(e => e.Timestamp))
        {
            snapshot = ApplyEvent(snapshot, evt);
        }
        
        return snapshot;
    }
}
```

### CQRS with Real-time Read Models
```csharp
// LinkedIn-style CQRS with real-time profile read models
public class ProfileReadModelUpdater : KeyedProcessFunction<string, ProfileCommand, ProfileReadModel>
{
    private ValueState<ProfileReadModel> readModelState;
    private readonly IEventPublisher eventPublisher;
    private readonly IReadModelStore readModelStore;
    
    public override void ProcessElement(
        ProfileCommand command, 
        Context context, 
        ICollector<ProfileReadModel> output)
    {
        var profileId = context.GetCurrentKey();
        var currentReadModel = readModelState.Value() ?? new ProfileReadModel(profileId);
        
        try
        {
            // Validate command
            var validationResult = ValidateCommand(command, currentReadModel);
            if (!validationResult.IsValid)
            {
                PublishCommandRejected(command, validationResult.Errors);
                return;
            }
            
            // Execute command and generate events
            var domainEvents = ExecuteCommand(command, currentReadModel);
            
            // Apply events to read model
            var updatedReadModel = currentReadModel;
            foreach (var domainEvent in domainEvents)
            {
                updatedReadModel = ApplyEventToReadModel(updatedReadModel, domainEvent);
                
                // Publish event for other bounded contexts
                await eventPublisher.PublishAsync(domainEvent);
            }
            
            // Update state and external read model store
            readModelState.Update(updatedReadModel);
            await readModelStore.UpdateAsync(updatedReadModel);
            
            // Emit updated read model
            output.Collect(updatedReadModel);
            
        }
        catch (Exception ex)
        {
            PublishCommandFailed(command, ex);
            throw;
        }
    }
    
    private List<IDomainEvent> ExecuteCommand(ProfileCommand command, ProfileReadModel currentState)
    {
        return command switch
        {
            UpdateBasicInfoCommand cmd => ExecuteUpdateBasicInfo(cmd, currentState),
            AddSkillCommand cmd => ExecuteAddSkill(cmd, currentState),
            AddExperienceCommand cmd => ExecuteAddExperience(cmd, currentState),
            UpdateConnectionsCommand cmd => ExecuteUpdateConnections(cmd, currentState),
            _ => throw new NotSupportedException($"Command type {command.GetType()} not supported")
        };
    }
    
    private List<IDomainEvent> ExecuteUpdateBasicInfo(UpdateBasicInfoCommand command, ProfileReadModel current)
    {
        var events = new List<IDomainEvent>();
        
        if (command.Name != current.Name)
        {
            events.Add(new ProfileNameUpdatedEvent
            {
                ProfileId = current.ProfileId,
                PreviousName = current.Name,
                NewName = command.Name,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        
        if (command.Industry != current.Industry)
        {
            events.Add(new ProfileIndustryUpdatedEvent
            {
                ProfileId = current.ProfileId,
                PreviousIndustry = current.Industry,
                NewIndustry = command.Industry,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        
        return events;
    }
}

// Real-time read model materialization for complex queries
public class ProfileSearchReadModelBuilder : KeyedProcessFunction<string, IDomainEvent, ProfileSearchDocument>
{
    private ValueState<ProfileSearchDocument> searchDocumentState;
    private final IElasticsearchClient elasticsearchClient;
    
    public override void ProcessElement(
        IDomainEvent domainEvent, 
        Context context, 
        ICollector<ProfileSearchDocument> output)
    {
        var profileId = ExtractProfileId(domainEvent);
        var currentDocument = searchDocumentState.Value() ?? new ProfileSearchDocument(profileId);
        
        // Project domain event to search document
        var updatedDocument = domainEvent switch
        {
            ProfileNameUpdatedEvent evt => currentDocument.WithName(evt.NewName),
            ProfileIndustryUpdatedEvent evt => currentDocument.WithIndustry(evt.NewIndustry),
            SkillAddedEvent evt => currentDocument.WithAddedSkill(evt.Skill),
            ExperienceAddedEvent evt => currentDocument.WithAddedExperience(evt.Experience),
            ConnectionAddedEvent evt => currentDocument.WithIncrementedConnections(),
            _ => currentDocument
        };
        
        // Update Elasticsearch for real-time search
        await elasticsearchClient.IndexAsync(updatedDocument);
        
        // Update state
        searchDocumentState.Update(updatedDocument);
        
        // Emit for downstream processing
        output.Collect(updatedDocument);
    }
}
```

### Distributed Saga Pattern
```csharp
// Uber-style saga orchestration for ride booking workflow
public class RideBookingSaga : KeyedProcessFunction<string, SagaEvent, SagaState>
{
    private ValueState<RideBookingSagaState> sagaState;
    private readonly ISagaCommandSender commandSender;
    private readonly ISagaEventPublisher eventPublisher;
    
    public override void ProcessElement(
        SagaEvent sagaEvent, 
        Context context, 
        ICollector<SagaState> output)
    {
        var sagaId = context.GetCurrentKey();
        var currentState = sagaState.Value() ?? new RideBookingSagaState(sagaId);
        
        // Process saga event and determine next steps
        var (nextState, commands) = ProcessSagaStep(currentState, sagaEvent);
        
        // Send commands to participating services
        foreach (var command in commands)
        {
            await commandSender.SendAsync(command);
        }
        
        // Handle saga completion or compensation
        if (nextState.IsCompleted)
        {
            await HandleSagaCompletion(nextState);
        }
        else if (nextState.RequiresCompensation)
        {
            await StartCompensation(nextState);
        }
        
        // Update saga state
        sagaState.Update(nextState);
        output.Collect(nextState);
    }
    
    private (RideBookingSagaState nextState, List<ISagaCommand> commands) ProcessSagaStep(
        RideBookingSagaState currentState, 
        SagaEvent sagaEvent)
    {
        var commands = new List<ISagaCommand>();
        var nextState = currentState;
        
        switch (currentState.CurrentStep, sagaEvent)
        {
            case (SagaStep.Started, RideRequestedEvent evt):
                // Step 1: Reserve driver
                commands.Add(new ReserveDriverCommand 
                { 
                    SagaId = currentState.SagaId,
                    RideId = evt.RideId,
                    Location = evt.PickupLocation,
                    RequiredVehicleType = evt.VehicleType
                });
                nextState = currentState.WithStep(SagaStep.DriverReservationPending);
                break;
                
            case (SagaStep.DriverReservationPending, DriverReservedEvent evt):
                // Step 2: Create payment hold
                commands.Add(new CreatePaymentHoldCommand
                {
                    SagaId = currentState.SagaId,
                    CustomerId = currentState.CustomerId,
                    Amount = evt.EstimatedFare,
                    PaymentMethodId = currentState.PaymentMethodId
                });
                nextState = currentState
                    .WithStep(SagaStep.PaymentHoldPending)
                    .WithDriverId(evt.DriverId)
                    .WithEstimatedFare(evt.EstimatedFare);
                break;
                
            case (SagaStep.PaymentHoldPending, PaymentHoldCreatedEvent evt):
                // Step 3: Confirm ride
                commands.Add(new ConfirmRideCommand
                {
                    SagaId = currentState.SagaId,
                    RideId = currentState.RideId,
                    DriverId = currentState.DriverId,
                    PaymentHoldId = evt.PaymentHoldId
                });
                nextState = currentState
                    .WithStep(SagaStep.RideConfirmationPending)
                    .WithPaymentHoldId(evt.PaymentHoldId);
                break;
                
            case (SagaStep.RideConfirmationPending, RideConfirmedEvent evt):
                // Saga completed successfully
                nextState = currentState.WithStep(SagaStep.Completed);
                break;
                
            // Compensation scenarios
            case (SagaStep.PaymentHoldPending, PaymentHoldFailedEvent evt):
                // Compensate: Release driver reservation
                commands.Add(new ReleaseDriverReservationCommand
                {
                    SagaId = currentState.SagaId,
                    DriverId = currentState.DriverId,
                    Reason = "Payment hold failed"
                });
                nextState = currentState.WithCompensationRequired();
                break;
                
            case (SagaStep.DriverReservationPending, DriverReservationFailedEvent evt):
                // No compensation needed - saga failed early
                nextState = currentState.WithStep(SagaStep.Failed);
                break;
        }
        
        return (nextState, commands);
    }
    
    private async Task StartCompensation(RideBookingSagaState sagaState)
    {
        var compensationCommands = GenerateCompensationCommands(sagaState);
        
        foreach (var command in compensationCommands.Reverse()) // Reverse order for compensation
        {
            await commandSender.SendAsync(command);
        }
        
        await eventPublisher.PublishAsync(new SagaCompensationStartedEvent
        {
            SagaId = sagaState.SagaId,
            CompensationSteps = compensationCommands.Count,
            StartedAt = DateTimeOffset.UtcNow
        });
    }
}
```

### Event-Driven Microservices Integration
```csharp
// Google-style event-driven integration between microservices
public class EventDrivenIntegrationHub : BroadcastProcessFunction<DomainEvent, IntegrationEvent>
{
    private readonly Dictionary<string, List<string>> eventSubscriptions;
    private readonly IIntegrationEventPublisher publisher;
    private readonly IDeduplicationService deduplicationService;
    
    public override void ProcessElement(
        DomainEvent domainEvent, 
        ReadOnlyContext context, 
        ICollector<IntegrationEvent> output)
    {
        // Check for duplicate events
        if (deduplicationService.IsDuplicate(domainEvent.EventId))
        {
            LogDuplicateEvent(domainEvent);
            return;
        }
        
        // Transform domain event to integration events for different bounded contexts
        var integrationEvents = TransformToIntegrationEvents(domainEvent);
        
        foreach (var integrationEvent in integrationEvents)
        {
            // Route to appropriate downstream services
            var subscribedServices = GetSubscribedServices(integrationEvent.EventType);
            
            foreach (var service in subscribedServices)
            {
                var routedEvent = RouteEventToService(integrationEvent, service);
                
                // Emit for downstream processing
                output.Collect(routedEvent);
                
                // Publish to external message broker for cross-system integration
                await publisher.PublishAsync(routedEvent, service);
            }
        }
        
        // Mark event as processed for deduplication
        deduplicationService.MarkProcessed(domainEvent.EventId);
    }
    
    private List<IntegrationEvent> TransformToIntegrationEvents(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            UserRegisteredEvent evt => new List<IntegrationEvent>
            {
                new UserAccountCreatedEvent { UserId = evt.UserId, Email = evt.Email },
                new WelcomeEmailTriggeredEvent { UserId = evt.UserId, Email = evt.Email },
                new UserAnalyticsInitializedEvent { UserId = evt.UserId }
            },
            
            OrderCompletedEvent evt => new List<IntegrationEvent>
            {
                new PaymentProcessedEvent { OrderId = evt.OrderId, Amount = evt.TotalAmount },
                new InventoryUpdatedEvent { Items = evt.Items },
                new ShippingRequestedEvent { OrderId = evt.OrderId, Address = evt.ShippingAddress },
                new OrderConfirmationEmailTriggeredEvent { OrderId = evt.OrderId, CustomerEmail = evt.CustomerEmail }
            },
            
            ProductPriceChangedEvent evt => new List<IntegrationEvent>
            {
                new PricingAnalyticsUpdatedEvent { ProductId = evt.ProductId, NewPrice = evt.NewPrice },
                new RecommendationEngineTriggeredEvent { ProductId = evt.ProductId },
                new CompetitorPriceCheckTriggeredEvent { ProductId = evt.ProductId }
            },
            
            _ => new List<IntegrationEvent>()
        };
    }
}
```

## Hands-On Exercises

### Exercise 1: E-commerce Order Saga
Build a complete e-commerce order processing saga that:
- Orchestrates inventory reservation, payment, and shipping
- Implements compensation for failed steps
- Handles partial failures and timeouts
- Provides order status tracking and customer notifications

### Exercise 2: Banking Event Sourcing
Create a banking account system using event sourcing that:
- Tracks all account transactions as immutable events
- Builds real-time account balance projections
- Implements audit trails for regulatory compliance
- Handles account transfers with saga patterns

### Exercise 3: Social Media CQRS Platform
Design a social media platform with CQRS that:
- Separates write operations (posts, comments, likes) from read models
- Builds real-time feeds and notification streams
- Implements eventually consistent friend connections
- Provides analytics and trending topic calculations

## Testing Event-Driven Systems

### Saga Testing Framework
```csharp
[Test]
public async Task TestRideBookingSagaHappyPath()
{
    var sagaTester = new SagaTestHarness<RideBookingSaga>();
    var sagaId = Guid.NewGuid().ToString();
    
    // Arrange: Initialize saga
    await sagaTester.Start(sagaId);
    
    // Act & Assert: Step through saga
    
    // Step 1: Request ride
    await sagaTester.SendEvent(new RideRequestedEvent
    {
        SagaId = sagaId,
        RideId = "ride-123",
        CustomerId = "customer-456",
        PickupLocation = new Location(37.7749, -122.4194),
        VehicleType = VehicleType.Standard
    });
    
    // Verify driver reservation command sent
    sagaTester.AssertCommandSent<ReserveDriverCommand>(cmd => 
        cmd.SagaId == sagaId && cmd.RideId == "ride-123");
    
    // Step 2: Driver reserved
    await sagaTester.SendEvent(new DriverReservedEvent
    {
        SagaId = sagaId,
        DriverId = "driver-789",
        EstimatedFare = 25.50m
    });
    
    // Verify payment hold command sent
    sagaTester.AssertCommandSent<CreatePaymentHoldCommand>(cmd => 
        cmd.Amount == 25.50m);
    
    // Step 3: Payment hold created
    await sagaTester.SendEvent(new PaymentHoldCreatedEvent
    {
        SagaId = sagaId,
        PaymentHoldId = "hold-999"
    });
    
    // Verify ride confirmation command sent
    sagaTester.AssertCommandSent<ConfirmRideCommand>();
    
    // Step 4: Ride confirmed
    await sagaTester.SendEvent(new RideConfirmedEvent
    {
        SagaId = sagaId,
        RideId = "ride-123"
    });
    
    // Verify saga completed
    var finalState = await sagaTester.GetSagaState();
    Assert.That(finalState.CurrentStep, Is.EqualTo(SagaStep.Completed));
    Assert.That(finalState.IsCompleted, Is.True);
}

[Test]
public async Task TestSagaCompensation()
{
    var sagaTester = new SagaTestHarness<RideBookingSaga>();
    var sagaId = Guid.NewGuid().ToString();
    
    // ... initial steps successful ...
    
    // Simulate payment failure
    await sagaTester.SendEvent(new PaymentHoldFailedEvent
    {
        SagaId = sagaId,
        Reason = "Insufficient funds"
    });
    
    // Verify compensation commands sent
    sagaTester.AssertCommandSent<ReleaseDriverReservationCommand>(cmd => 
        cmd.Reason == "Payment hold failed");
    
    var finalState = await sagaTester.GetSagaState();
    Assert.That(finalState.RequiresCompensation, Is.True);
}
```

## Architecture Integration
- Connect with Temporal for saga orchestration
- Use Kafka for event streaming between bounded contexts
- Implement Redis for event deduplication and caching
- Configure OpenTelemetry for distributed tracing across sagas

## Performance Optimization
- Implement event store sharding strategies
- Optimize read model update performance
- Use materialized views for complex projections
- Implement event store compaction and archiving

## References
- [Microsoft .NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/)
- [Event Sourcing Pattern - Microsoft](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- [CQRS Pattern - Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [Saga Pattern - Microservices.io](https://microservices.io/patterns/data/saga.html)
- [Domain-Driven Design by Eric Evans](https://www.amazon.com/Domain-Driven-Design-Tackling-Complexity-Software/dp/0321125215)

## Next Steps
Day 13 focuses on advanced testing strategies including chaos engineering, property-based testing, and production testing techniques.