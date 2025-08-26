# Day 5: Temporal Workflow Orchestration & Durable Execution

## 🗺️ Course Navigation
**[← Day 4: Enterprise Observability](../Day04-Enterprise-Observability/)** | **[Course Overview](../README.md)** | **[Next: Day 6 - Advanced Windows & Joins →](../Day06-Advanced-Windows-Joins/)**

---

## 🎯 Real-World Learning Objectives

Master **Temporal's durable execution platform** to orchestrate complex, long-running business processes with fault tolerance, state management, and scalable workflow patterns used by Uber, Netflix, and Snapchat.

**Time:** 7-8 hours | **Reference:** [Temporal Patterns & Best Practices](https://docs.temporal.io/dev-guide)

## 📚 Real-World Reference Foundation

This module implements **enterprise workflow orchestration patterns** from industry leaders:

### 🏛️ Industry Reference Standards
- **[Temporal at Uber](https://temporal.io/case-study/uber/)** - Large-scale workflow orchestration for ride scheduling and payments
- **[Netflix's Conductor vs Temporal](https://netflixtechblog.com/conductor-a-microservices-orchestrator-2e8d4771bf40)** - Microservices orchestration patterns
- **[Snapchat's Workflow Engine](https://temporal.io/case-study/snapchat/)** - Media processing and user engagement workflows
- **[Microsoft's Durable Functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/)** - Serverless workflow patterns (similar concepts)

### 🔧 Enterprise Workflow Patterns
- **[Saga Pattern](https://microservices.io/patterns/data/saga.html)** - Distributed transaction management
- **[Process Manager Pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ProcessManager.html)** - Long-running process coordination
- **[Event Choreography vs Orchestration](https://temporal.io/blog/to-choreograph-or-orchestrate-your-saga-that-is-the-question/)** - Workflow design decisions

## 🌟 Temporal's Revolutionary Approach

Temporal solves the **hardest problems in distributed systems** through durable execution:

### Core Principles

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      TEMPORAL DURABLE EXECUTION ARCHITECTURE                    │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐ │
│  │    WORKFLOW         │    │     ACTIVITIES      │    │   TEMPORAL SERVER   │ │
│  │   (ORCHESTRATOR)    │    │    (WORKERS)        │    │    (COORDINATOR)    │ │
│  │                     │    │                     │    │                     │ │
│  │ • Business Logic    │───▶│ • External APIs     │───▶│ • Event History     │ │
│  │ • Decision Making   │    │ • Database Ops      │    │ • State Management  │ │
│  │ • Error Handling    │    │ • File Processing   │    │ • Task Queues       │ │
│  │ • Compensation      │    │ • Notifications     │    │ • Fault Recovery    │ │
│  └─────────────────────┘    └─────────────────────┘    └─────────────────────┘ │
│           │                           │                           │            │
│           │              ┌─────────────────────────────────────────────────────┤
│           │              │                CAPABILITIES                         │ │
│           │              │                                                     │ │
│           └──────────────│ • Automatic Retries with Exponential Backoff      │ │
│                          │ • Timeouts and Cancellation                       │ │
│                          │ • Versioning and Rolling Deployments              │ │
│                          │ • Visibility and Debugging                        │ │
│                          │ • Testing and Simulation                          │ │
│                          │ • Multi-language SDKs (.NET, Java, Go, Python)   │ │
│                          └─────────────────────────────────────────────────────┘ │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Key Advantages Over Traditional Solutions

| Traditional Approach | Temporal Approach | Enterprise Benefit |
|---------------------|-------------------|-------------------|
| **Manual State Management** | Automatic state persistence | Zero data loss, reliable recovery |
| **Complex Retry Logic** | Built-in retry with backoff | Simplified error handling |
| **Monitoring Challenges** | Native observability | Full workflow visibility |
| **Deployment Complexity** | Versioning support | Zero-downtime deployments |
| **Testing Difficulties** | Deterministic testing | Reliable CI/CD pipelines |

## 🏗️ Your Production Temporal Environment

Your LocalTesting environment provides a **complete Temporal platform** identical to enterprise deployments:

### Infrastructure Overview

| Component | URL | Enterprise Pattern | Production Use Case |
|-----------|-----|-------------------|-------------------|
| **Temporal Server** | http://localhost:7233 | Uber's workflow backend | Durable execution engine |
| **Temporal Web UI** | http://localhost:8084 | Workflow monitoring dashboard | Operational visibility |
| **PostgreSQL** | localhost:5432 | Persistent workflow state | Event history storage |
| **Worker Processes** | .NET Applications | Distributed workers | Activity execution |

### Current Setup Validation

```bash
# Verify Temporal connectivity
curl -s http://localhost:8084/api/v1/namespaces | jq '.namespaces[].namespaceInfo.name'
# Expected: ["default"]

# Check server health
curl -s http://localhost:7233/api/v1/namespaces/default | jq '.namespaceInfo.state'
# Expected: "Registered"

# Validate PostgreSQL backend  
docker exec temporal-postgres psql -U temporal -d temporal -c "\dt"
# Expected: Multiple temporal_ tables
```

## 🚀 Enterprise Workflow Implementation

Let's build sophisticated workflows that demonstrate real-world enterprise patterns:

### Step 1: Production E-commerce Order Processing Workflow

Create `Day05_EnterpriseOrderWorkflow.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Temporalio.Workflows;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Worker;
using Temporalio.Common;
using System.Text.Json;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LearningCourse.Day05
{
    /// <summary>
    /// Enterprise-grade order processing workflow demonstrating:
    /// - Saga pattern implementation for distributed transactions
    /// - Multi-step business process orchestration  
    /// - Comprehensive error handling and compensation
    /// - Real-world integration patterns with external systems
    /// - Production monitoring and observability
    /// 
    /// References:
    /// - Temporal Best Practices Guide
    /// - Uber's Order Management Workflows
    /// - Saga Pattern Implementation
    /// - Microservices Orchestration Patterns
    /// </summary>

    // === WORKFLOW DEFINITIONS ===

    [Workflow("OrderProcessingWorkflow")]
    public class OrderProcessingWorkflow
    {
        private static readonly ActivityOptions DefaultActivityOptions = new()
        {
            StartToCloseTimeout = TimeSpan.FromMinutes(5),
            RetryPolicy = new RetryPolicy
            {
                InitialInterval = TimeSpan.FromSeconds(1),
                MaximumInterval = TimeSpan.FromSeconds(30),
                BackoffCoefficient = 2.0,
                MaximumAttempts = 3,
                NonRetryableErrorTypes = { "InvalidOrderException", "PaymentDeclinedException" }
            }
        };

        private static readonly ActivityOptions PaymentActivityOptions = new()
        {
            StartToCloseTimeout = TimeSpan.FromMinutes(2),
            RetryPolicy = new RetryPolicy
            {
                InitialInterval = TimeSpan.FromSeconds(2),
                MaximumInterval = TimeSpan.FromMinutes(1),
                BackoffCoefficient = 1.5,
                MaximumAttempts = 5,
                NonRetryableErrorTypes = { "PaymentDeclinedException", "InsufficientFundsException" }
            }
        };

        [WorkflowRun]
        public async Task<OrderResult> ProcessOrderAsync(OrderRequest request)
        {
            var logger = Workflow.Logger;
            var workflowId = Workflow.Info.WorkflowId;
            
            logger.LogInformation("🛒 Starting order processing workflow for Order {OrderId}", request.OrderId);

            var result = new OrderResult
            {
                OrderId = request.OrderId,
                WorkflowId = workflowId,
                Status = OrderStatus.Processing,
                ProcessingSteps = new List<ProcessingStep>(),
                StartTime = Workflow.UtcNow
            };

            try
            {
                // Step 1: Validate Order
                logger.LogInformation("📋 Step 1: Validating order {OrderId}", request.OrderId);
                var validationResult = await Workflow.ExecuteActivityAsync(
                    () => OrderActivities.ValidateOrderAsync(request),
                    DefaultActivityOptions);
                
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    StepName = "OrderValidation",
                    Status = "Completed",
                    CompletedAt = Workflow.UtcNow,
                    Details = $"Order validated successfully. Total: ${validationResult.TotalAmount:F2}"
                });

                // Step 2: Reserve Inventory
                logger.LogInformation("📦 Step 2: Reserving inventory for order {OrderId}", request.OrderId);
                var inventoryReservation = await Workflow.ExecuteActivityAsync(
                    () => OrderActivities.ReserveInventoryAsync(request),
                    DefaultActivityOptions);
                
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    StepName = "InventoryReservation", 
                    Status = "Completed",
                    CompletedAt = Workflow.UtcNow,
                    Details = $"Reserved {inventoryReservation.ReservedItems.Count} items"
                });

                // Step 3: Process Payment (Critical Path)
                logger.LogInformation("💳 Step 3: Processing payment for order {OrderId}", request.OrderId);
                var paymentResult = await Workflow.ExecuteActivityAsync(
                    () => OrderActivities.ProcessPaymentAsync(request, validationResult.TotalAmount),
                    PaymentActivityOptions);
                
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    StepName = "PaymentProcessing",
                    Status = "Completed", 
                    CompletedAt = Workflow.UtcNow,
                    Details = $"Payment processed: ${paymentResult.AmountCharged:F2} via {paymentResult.PaymentMethod}"
                });

                // Step 4: Create Shipment
                logger.LogInformation("📤 Step 4: Creating shipment for order {OrderId}", request.OrderId);
                var shipmentResult = await Workflow.ExecuteActivityAsync(
                    () => OrderActivities.CreateShipmentAsync(request, inventoryReservation),
                    DefaultActivityOptions);
                
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    StepName = "ShipmentCreation",
                    Status = "Completed",
                    CompletedAt = Workflow.UtcNow,
                    Details = $"Shipment created: {shipmentResult.TrackingNumber}"
                });

                // Step 5: Send Notifications
                logger.LogInformation("📧 Step 5: Sending notifications for order {OrderId}", request.OrderId);
                await Workflow.ExecuteActivityAsync(
                    () => OrderActivities.SendOrderConfirmationAsync(request, paymentResult, shipmentResult),
                    DefaultActivityOptions);
                
                result.ProcessingSteps.Add(new ProcessingStep
                {
                    StepName = "NotificationSent",
                    Status = "Completed",
                    CompletedAt = Workflow.UtcNow,
                    Details = "Customer and warehouse notifications sent"
                });

                // Step 6: Update Analytics
                logger.LogInformation("📊 Step 6: Updating analytics for order {OrderId}", request.OrderId);
                await Workflow.ExecuteActivityAsync(
                    () => OrderActivities.UpdateAnalyticsAsync(request, validationResult.TotalAmount),
                    DefaultActivityOptions);

                result.Status = OrderStatus.Completed;
                result.CompletedAt = Workflow.UtcNow;
                result.TotalAmount = validationResult.TotalAmount;
                result.PaymentMethod = paymentResult.PaymentMethod;
                result.TrackingNumber = shipmentResult.TrackingNumber;

                logger.LogInformation("✅ Order {OrderId} processed successfully in {Duration}ms", 
                    request.OrderId, 
                    (Workflow.UtcNow - result.StartTime).TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Order processing failed for {OrderId}: {Error}", request.OrderId, ex.Message);
                
                // Implement compensation/rollback logic
                result.Status = OrderStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.CompletedAt = Workflow.UtcNow;

                // Compensate in reverse order
                await CompensateOrderProcessingAsync(request, result, logger);

                return result;
            }
        }

        private async Task CompensateOrderProcessingAsync(OrderRequest request, OrderResult result, ILogger logger)
        {
            logger.LogWarning("🔄 Starting compensation for failed order {OrderId}", request.OrderId);

            // Reverse the steps that were completed
            foreach (var step in result.ProcessingSteps.AsEnumerable().Reverse())
            {
                try
                {
                    switch (step.StepName)
                    {
                        case "PaymentProcessing":
                            logger.LogInformation("💸 Refunding payment for order {OrderId}", request.OrderId);
                            await Workflow.ExecuteActivityAsync(
                                () => OrderActivities.RefundPaymentAsync(request),
                                DefaultActivityOptions);
                            break;

                        case "InventoryReservation":
                            logger.LogInformation("📦 Releasing inventory reservation for order {OrderId}", request.OrderId);
                            await Workflow.ExecuteActivityAsync(
                                () => OrderActivities.ReleaseInventoryAsync(request),
                                DefaultActivityOptions);
                            break;

                        case "ShipmentCreation":
                            logger.LogInformation("📤 Canceling shipment for order {OrderId}", request.OrderId);
                            await Workflow.ExecuteActivityAsync(
                                () => OrderActivities.CancelShipmentAsync(request),
                                DefaultActivityOptions);
                            break;
                    }

                    step.Status = "Compensated";
                    step.Details += " [COMPENSATED]";
                }
                catch (Exception compensationEx)
                {
                    logger.LogError(compensationEx, "❌ Compensation failed for step {StepName} in order {OrderId}", 
                        step.StepName, request.OrderId);
                    
                    step.Status = "CompensationFailed";
                    step.Details += $" [COMPENSATION FAILED: {compensationEx.Message}]";
                }
            }

            logger.LogInformation("🔄 Compensation completed for order {OrderId}", request.OrderId);
        }
    }

    // === CHILD WORKFLOWS FOR COMPLEX PROCESSES ===

    [Workflow("PaymentRetryWorkflow")]
    public class PaymentRetryWorkflow
    {
        [WorkflowRun]
        public async Task<PaymentResult> ProcessPaymentWithRetriesAsync(PaymentRequest request)
        {
            var logger = Workflow.Logger;
            var attempts = 0;
            var maxAttempts = 3;

            while (attempts < maxAttempts)
            {
                attempts++;
                logger.LogInformation("💳 Payment attempt {Attempt}/{MaxAttempts} for order {OrderId}", 
                    attempts, maxAttempts, request.OrderId);

                try
                {
                    return await Workflow.ExecuteActivityAsync(
                        () => OrderActivities.ProcessPaymentAsync(new OrderRequest { OrderId = request.OrderId }, request.Amount),
                        new ActivityOptions
                        {
                            StartToCloseTimeout = TimeSpan.FromMinutes(1),
                            RetryPolicy = new RetryPolicy { MaximumAttempts = 1 } // No activity-level retries
                        });
                }
                catch (Exception ex) when (attempts < maxAttempts)
                {
                    logger.LogWarning("💳 Payment attempt {Attempt} failed: {Error}. Retrying in {Delay}s...", 
                        attempts, ex.Message, Math.Pow(2, attempts));
                    
                    // Exponential backoff delay
                    await Workflow.DelayAsync(TimeSpan.FromSeconds(Math.Pow(2, attempts)));
                }
            }

            throw new PaymentProcessingException($"Payment failed after {maxAttempts} attempts");
        }
    }

    // === ACTIVITY IMPLEMENTATIONS ===

    public class OrderActivities
    {
        private static readonly ActivitySource ActivitySource = new("FlinkDotNet.Day05.OrderActivities");
        private static readonly Meter ApplicationMeter = new("FlinkDotNet.Day05.Activities");
        
        private static readonly Counter<long> OrdersProcessed = ApplicationMeter.CreateCounter<long>(
            "orders_processed_total", description: "Total orders processed");
        private static readonly Histogram<double> ActivityDuration = ApplicationMeter.CreateHistogram<double>(
            "activity_duration_ms", description: "Activity execution duration");
        private static readonly Counter<long> ActivityErrors = ApplicationMeter.CreateCounter<long>(
            "activity_errors_total", description: "Total activity errors");

        [Activity("ValidateOrder")]
        public static async Task<OrderValidationResult> ValidateOrderAsync(OrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("ValidateOrder");
            activity?.SetTag("order.id", request.OrderId);
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                Activity.Current?.Logger.LogInformation("📋 Validating order {OrderId}", request.OrderId);
                
                // Simulate realistic validation logic
                await Task.Delay(Random.Shared.Next(100, 300)); // 100-300ms validation time
                
                // Validation rules
                if (string.IsNullOrEmpty(request.CustomerId))
                    throw new InvalidOrderException("Customer ID is required");
                
                if (request.Items == null || request.Items.Count == 0)
                    throw new InvalidOrderException("Order must contain at least one item");

                var totalAmount = request.Items.Sum(item => item.Price * item.Quantity);
                
                if (totalAmount <= 0)
                    throw new InvalidOrderException("Order total must be greater than zero");

                if (totalAmount > 10000) // High-value order validation
                {
                    Activity.Current?.Logger.LogWarning("🚨 High-value order detected: ${Amount:F2}", totalAmount);
                    await Task.Delay(500); // Additional fraud checks
                }

                var result = new OrderValidationResult
                {
                    IsValid = true,
                    TotalAmount = totalAmount,
                    ValidatedAt = DateTime.UtcNow,
                    ValidationDetails = $"Order validated: {request.Items.Count} items, ${totalAmount:F2} total"
                };

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                ActivityDuration.Record(duration, new KeyValuePair<string, object?>("activity", "validate_order"));
                OrdersProcessed.Add(1, new KeyValuePair<string, object?>("step", "validation"));

                activity?.SetTag("validation.total_amount", totalAmount);
                activity?.SetTag("validation.item_count", request.Items.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                ActivityErrors.Add(1, new KeyValuePair<string, object?>("activity", "validate_order"));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [Activity("ReserveInventory")]
        public static async Task<InventoryReservationResult> ReserveInventoryAsync(OrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("ReserveInventory");
            activity?.SetTag("order.id", request.OrderId);
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                Activity.Current?.Logger.LogInformation("📦 Reserving inventory for order {OrderId}", request.OrderId);
                
                // Simulate inventory system interaction
                await Task.Delay(Random.Shared.Next(200, 500)); // 200-500ms inventory check
                
                var reservedItems = new List<ReservedItem>();
                
                foreach (var item in request.Items)
                {
                    // Simulate inventory availability check
                    var availableQuantity = Random.Shared.Next(0, 100); // Simulate inventory levels
                    
                    if (availableQuantity < item.Quantity)
                    {
                        throw new InsufficientInventoryException(
                            $"Insufficient inventory for item {item.ProductId}. " +
                            $"Requested: {item.Quantity}, Available: {availableQuantity}");
                    }
                    
                    reservedItems.Add(new ReservedItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        ReservationId = Guid.NewGuid().ToString(),
                        WarehouseLocation = $"WH-{Random.Shared.Next(1, 5)}"
                    });
                }

                var result = new InventoryReservationResult
                {
                    ReservationId = Guid.NewGuid().ToString(),
                    ReservedItems = reservedItems,
                    ReservedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30) // 30-minute reservation window
                };

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                ActivityDuration.Record(duration, new KeyValuePair<string, object?>("activity", "reserve_inventory"));

                activity?.SetTag("reservation.id", result.ReservationId);
                activity?.SetTag("reservation.item_count", reservedItems.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                ActivityErrors.Add(1, new KeyValuePair<string, object?>("activity", "reserve_inventory"));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [Activity("ProcessPayment")]
        public static async Task<PaymentResult> ProcessPaymentAsync(OrderRequest request, decimal amount)
        {
            using var activity = ActivitySource.StartActivity("ProcessPayment");
            activity?.SetTag("order.id", request.OrderId);
            activity?.SetTag("payment.amount", amount);
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                Activity.Current?.Logger.LogInformation("💳 Processing payment for order {OrderId}: ${Amount:F2}", 
                    request.OrderId, amount);
                
                // Simulate payment gateway interaction
                await Task.Delay(Random.Shared.Next(500, 1500)); // 500-1500ms payment processing
                
                // Simulate payment scenarios
                var scenario = Random.Shared.NextDouble();
                
                if (scenario < 0.05) // 5% payment declined
                {
                    throw new PaymentDeclinedException("Payment was declined by the payment provider");
                }
                
                if (scenario < 0.02) // 2% insufficient funds
                {
                    throw new InsufficientFundsException("Insufficient funds in customer account");
                }
                
                if (scenario < 0.01) // 1% payment gateway timeout
                {
                    throw new PaymentGatewayException("Payment gateway timeout - please retry");
                }

                var paymentMethods = new[] { "Credit Card", "Debit Card", "PayPal", "Apple Pay", "Google Pay" };
                var selectedMethod = paymentMethods[Random.Shared.Next(paymentMethods.Length)];

                var result = new PaymentResult
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    PaymentMethod = selectedMethod,
                    AmountCharged = amount,
                    ProcessedAt = DateTime.UtcNow,
                    Status = "Completed",
                    GatewayReference = $"GTW-{Random.Shared.Next(100000, 999999)}"
                };

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                ActivityDuration.Record(duration, new KeyValuePair<string, object?>("activity", "process_payment"));

                activity?.SetTag("payment.transaction_id", result.TransactionId);
                activity?.SetTag("payment.method", selectedMethod);
                activity?.SetTag("payment.gateway_reference", result.GatewayReference);
                
                return result;
            }
            catch (Exception ex)
            {
                ActivityErrors.Add(1, new KeyValuePair<string, object?>("activity", "process_payment"));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [Activity("CreateShipment")]
        public static async Task<ShipmentResult> CreateShipmentAsync(OrderRequest request, InventoryReservationResult reservation)
        {
            using var activity = ActivitySource.StartActivity("CreateShipment");
            activity?.SetTag("order.id", request.OrderId);
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                Activity.Current?.Logger.LogInformation("📤 Creating shipment for order {OrderId}", request.OrderId);
                
                // Simulate shipping system interaction
                await Task.Delay(Random.Shared.Next(300, 800)); // 300-800ms shipping creation
                
                var carriers = new[] { "FedEx", "UPS", "DHL", "USPS", "Amazon Logistics" };
                var selectedCarrier = carriers[Random.Shared.Next(carriers.Length)];
                
                var result = new ShipmentResult
                {
                    ShipmentId = Guid.NewGuid().ToString(),
                    TrackingNumber = $"{selectedCarrier.ToUpper()}-{Random.Shared.Next(100000000, 999999999)}",
                    Carrier = selectedCarrier,
                    EstimatedDelivery = DateTime.UtcNow.AddDays(Random.Shared.Next(2, 7)), // 2-7 days delivery
                    ShippingAddress = request.ShippingAddress,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Created"
                };

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                ActivityDuration.Record(duration, new KeyValuePair<string, object?>("activity", "create_shipment"));

                activity?.SetTag("shipment.id", result.ShipmentId);
                activity?.SetTag("shipment.carrier", selectedCarrier);
                activity?.SetTag("shipment.tracking_number", result.TrackingNumber);
                
                return result;
            }
            catch (Exception ex)
            {
                ActivityErrors.Add(1, new KeyValuePair<string, object?>("activity", "create_shipment"));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [Activity("SendOrderConfirmation")]
        public static async Task SendOrderConfirmationAsync(OrderRequest request, PaymentResult payment, ShipmentResult shipment)
        {
            using var activity = ActivitySource.StartActivity("SendOrderConfirmation");
            activity?.SetTag("order.id", request.OrderId);
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                Activity.Current?.Logger.LogInformation("📧 Sending order confirmation for {OrderId}", request.OrderId);
                
                // Simulate notification system interaction
                await Task.Delay(Random.Shared.Next(100, 400)); // 100-400ms notification sending
                
                var notificationChannels = new[] { "Email", "SMS", "Push Notification", "In-App" };
                
                foreach (var channel in notificationChannels)
                {
                    Activity.Current?.Logger.LogInformation("📱 Sending {Channel} notification for order {OrderId}", 
                        channel, request.OrderId);
                    
                    await Task.Delay(50); // Simulate per-channel delay
                }

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                ActivityDuration.Record(duration, new KeyValuePair<string, object?>("activity", "send_notifications"));

                activity?.SetTag("notifications.channels", string.Join(",", notificationChannels));
            }
            catch (Exception ex)
            {
                ActivityErrors.Add(1, new KeyValuePair<string, object?>("activity", "send_notifications"));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [Activity("UpdateAnalytics")]
        public static async Task UpdateAnalyticsAsync(OrderRequest request, decimal totalAmount)
        {
            using var activity = ActivitySource.StartActivity("UpdateAnalytics");
            activity?.SetTag("order.id", request.OrderId);
            
            try
            {
                Activity.Current?.Logger.LogInformation("📊 Updating analytics for order {OrderId}", request.OrderId);
                
                // Simulate analytics system updates
                await Task.Delay(Random.Shared.Next(50, 200)); // 50-200ms analytics update
                
                // Record business metrics
                OrdersProcessed.Add(1, 
                    new KeyValuePair<string, object?>("step", "completed"),
                    new KeyValuePair<string, object?>("customer_id", request.CustomerId));
                
                activity?.SetTag("analytics.total_amount", totalAmount);
                activity?.SetTag("analytics.customer_id", request.CustomerId);
            }
            catch (Exception ex)
            {
                ActivityErrors.Add(1, new KeyValuePair<string, object?>("activity", "update_analytics"));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        // Compensation Activities
        
        [Activity("RefundPayment")]
        public static async Task RefundPaymentAsync(OrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("RefundPayment");
            activity?.SetTag("order.id", request.OrderId);
            
            try
            {
                Activity.Current?.Logger.LogInformation("💸 Processing refund for order {OrderId}", request.OrderId);
                await Task.Delay(Random.Shared.Next(300, 800));
                
                activity?.SetTag("refund.status", "completed");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [Activity("ReleaseInventory")]
        public static async Task ReleaseInventoryAsync(OrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("ReleaseInventory");
            activity?.SetTag("order.id", request.OrderId);
            
            try
            {
                Activity.Current?.Logger.LogInformation("📦 Releasing inventory for order {OrderId}", request.OrderId);
                await Task.Delay(Random.Shared.Next(100, 300));
                
                activity?.SetTag("inventory.status", "released");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        [Activity("CancelShipment")]
        public static async Task CancelShipmentAsync(OrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("CancelShipment");
            activity?.SetTag("order.id", request.OrderId);
            
            try
            {
                Activity.Current?.Logger.LogInformation("📤 Canceling shipment for order {OrderId}", request.OrderId);
                await Task.Delay(Random.Shared.Next(200, 500));
                
                activity?.SetTag("shipment.status", "cancelled");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
    }

    // === DATA MODELS ===

    public class OrderRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderResult
    {
        public string OrderId { get; set; } = string.Empty;
        public string WorkflowId { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public List<ProcessingStep> ProcessingSteps { get; set; } = new();
    }

    public class ProcessingStep
    {
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public enum OrderStatus
    {
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    // Activity Result Models
    
    public class OrderValidationResult
    {
        public bool IsValid { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime ValidatedAt { get; set; }
        public string ValidationDetails { get; set; } = string.Empty;
    }

    public class InventoryReservationResult
    {
        public string ReservationId { get; set; } = string.Empty;
        public List<ReservedItem> ReservedItems { get; set; } = new();
        public DateTime ReservedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class ReservedItem
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string ReservationId { get; set; } = string.Empty;
        public string WarehouseLocation { get; set; } = string.Empty;
    }

    public class PaymentResult
    {
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal AmountCharged { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string GatewayReference { get; set; } = string.Empty;
    }

    public class PaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class ShipmentResult
    {
        public string ShipmentId { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public DateTime EstimatedDelivery { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Custom Exceptions
    
    public class InvalidOrderException : Exception
    {
        public InvalidOrderException(string message) : base(message) { }
    }

    public class InsufficientInventoryException : Exception
    {
        public InsufficientInventoryException(string message) : base(message) { }
    }

    public class PaymentDeclinedException : Exception
    {
        public PaymentDeclinedException(string message) : base(message) { }
    }

    public class InsufficientFundsException : Exception
    {
        public InsufficientFundsException(string message) : base(message) { }
    }

    public class PaymentGatewayException : Exception
    {
        public PaymentGatewayException(string message) : base(message) { }
    }

    public class PaymentProcessingException : Exception
    {
        public PaymentProcessingException(string message) : base(message) { }
    }

    // === MAIN APPLICATION ===

    public class TemporalWorkflowShowcase
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔄 Temporal Workflow Orchestration Showcase");
            Console.WriteLine("===========================================");
            Console.WriteLine("🌐 Temporal UI:  http://localhost:8084");
            Console.WriteLine("📊 Grafana:      http://localhost:3000");
            Console.WriteLine("🔗 Traces:       http://localhost:18888");
            Console.WriteLine();

            // Create Temporal client
            var client = await TemporalClient.ConnectAsync(new TemporalClientConnectOptions
            {
                TargetHost = "localhost",
                TargetPort = 7233,
                Namespace = "default"
            });

            // Create and start worker
            using var worker = new TemporalWorker(
                client,
                new TemporalWorkerOptions("order-processing-queue")
                    .AddWorkflow<OrderProcessingWorkflow>()
                    .AddWorkflow<PaymentRetryWorkflow>()
                    .AddActivitiesInstance(new OrderActivities()));

            var workerTask = worker.ExecuteAsync();

            // Run workflow demonstrations
            await DemonstrateOrderProcessingWorkflows(client);

            // Allow some time for workflows to complete
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Graceful shutdown
            worker.Shutdown();
            await workerTask;
        }

        private static async Task DemonstrateOrderProcessingWorkflows(TemporalClient client)
        {
            Console.WriteLine("🛒 Starting order processing workflow demonstrations...");

            var workflows = new List<Task>();

            // Demonstrate successful orders
            for (int i = 1; i <= 5; i++)
            {
                workflows.Add(ProcessSampleOrder(client, i, OrderScenario.Success));
            }

            // Demonstrate payment failures
            for (int i = 6; i <= 7; i++)
            {
                workflows.Add(ProcessSampleOrder(client, i, OrderScenario.PaymentFailure));
            }

            // Demonstrate inventory failures  
            workflows.Add(ProcessSampleOrder(client, 8, OrderScenario.InventoryFailure));

            // Demonstrate high-value orders
            workflows.Add(ProcessSampleOrder(client, 9, OrderScenario.HighValue));

            // Wait for all workflows to complete
            await Task.WhenAll(workflows);

            Console.WriteLine("✅ All workflow demonstrations completed");
        }

        private static async Task ProcessSampleOrder(TemporalClient client, int orderNumber, OrderScenario scenario)
        {
            var orderId = $"ORDER-{orderNumber:D4}";
            var workflowId = $"order-workflow-{orderId}";

            try
            {
                var orderRequest = GenerateOrderRequest(orderId, scenario);

                Console.WriteLine($"🚀 Starting workflow for {orderId} ({scenario})");

                var handle = await client.StartWorkflowAsync(
                    (OrderProcessingWorkflow workflow) => workflow.ProcessOrderAsync(orderRequest),
                    new WorkflowOptions
                    {
                        Id = workflowId,
                        TaskQueue = "order-processing-queue",
                        ExecutionTimeout = TimeSpan.FromMinutes(10)
                    });

                var result = await handle.GetResultAsync();

                Console.WriteLine($"✅ {orderId} completed: {result.Status} | ${result.TotalAmount:F2} | {result.ProcessingSteps.Count} steps");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {orderId} failed: {ex.Message}");
            }
        }

        private static OrderRequest GenerateOrderRequest(string orderId, OrderScenario scenario)
        {
            var request = new OrderRequest
            {
                OrderId = orderId,
                CustomerId = $"CUST-{Random.Shared.Next(1000, 9999)}",
                ShippingAddress = "123 Main St, Anytown, ST 12345, USA",
                PaymentMethod = "Credit Card",
                Items = new List<OrderItem>()
            };

            switch (scenario)
            {
                case OrderScenario.Success:
                    request.Items.AddRange(new[]
                    {
                        new OrderItem { ProductId = "PROD-001", ProductName = "Laptop", Quantity = 1, Price = 999.99m },
                        new OrderItem { ProductId = "PROD-002", ProductName = "Mouse", Quantity = 2, Price = 29.99m }
                    });
                    break;

                case OrderScenario.HighValue:
                    request.Items.AddRange(new[]
                    {
                        new OrderItem { ProductId = "PROD-PREMIUM", ProductName = "Enterprise Server", Quantity = 1, Price = 15000.00m }
                    });
                    break;

                case OrderScenario.PaymentFailure:
                    request.Items.AddRange(new[]
                    {
                        new OrderItem { ProductId = "PROD-003", ProductName = "Keyboard", Quantity = 1, Price = 79.99m }
                    });
                    request.PaymentMethod = "Declined Card"; // Will trigger payment failure
                    break;

                case OrderScenario.InventoryFailure:
                    request.Items.AddRange(new[]
                    {
                        new OrderItem { ProductId = "PROD-OUT-OF-STOCK", ProductName = "Popular Item", Quantity = 999, Price = 49.99m }
                    });
                    break;
            }

            return request;
        }

        private enum OrderScenario
        {
            Success,
            PaymentFailure,
            InventoryFailure,
            HighValue
        }
    }
}
```

## 🎯 Day 5 Exercises

### Exercise 5.1: Workflow Execution and Monitoring

**Objective**: Execute workflows and monitor them using Temporal's UI

1. **Start the Application**:
   ```bash
   cd LearningCourse/Day05-Temporal-Workflows
   dotnet build
   dotnet run
   ```

2. **Monitor in Temporal UI** (http://localhost:8084):
   - View workflow execution history
   - Examine activity execution details
   - Analyze failure scenarios and compensation

3. **Understand Workflow States**:
   - Running workflows
   - Completed workflows  
   - Failed workflows with compensation

### Exercise 5.2: Custom Workflow Implementation

**Objective**: Build a custom workflow for your business domain

```csharp
// Example: User Onboarding Workflow
[Workflow("UserOnboardingWorkflow")]
public class UserOnboardingWorkflow
{
    [WorkflowRun]
    public async Task<OnboardingResult> OnboardUserAsync(OnboardingRequest request)
    {
        // Step 1: Create user account
        var account = await Workflow.ExecuteActivityAsync(
            () => UserActivities.CreateAccountAsync(request));
        
        // Step 2: Send welcome email
        await Workflow.ExecuteActivityAsync(
            () => UserActivities.SendWelcomeEmailAsync(request, account));
        
        // Step 3: Schedule follow-up (demonstrate timers)
        await Workflow.DelayAsync(TimeSpan.FromDays(7));
        
        // Step 4: Send follow-up email
        await Workflow.ExecuteActivityAsync(
            () => UserActivities.SendFollowUpEmailAsync(request, account));
        
        return new OnboardingResult { Success = true, UserId = account.UserId };
    }
}
```

### Exercise 5.3: Long-Running Process Simulation

**Objective**: Implement workflows with realistic timing and state management

```csharp
// Demonstrate long-running processes with timers and signals
[Workflow("DocumentProcessingWorkflow")]
public class DocumentProcessingWorkflow
{
    private bool _approvalReceived = false;
    
    [WorkflowRun]
    public async Task<ProcessingResult> ProcessDocumentAsync(DocumentRequest request)
    {
        // Step 1: Initial processing
        await Workflow.ExecuteActivityAsync(() => DocumentActivities.ValidateDocumentAsync(request));
        
        // Step 2: Wait for approval (up to 48 hours)
        var approvalTimeout = TimeSpan.FromHours(48);
        
        using var cts = new CancellationTokenSource();
        var approvalTask = Workflow.WaitConditionAsync(() => _approvalReceived, cts.Token);
        var timeoutTask = Workflow.DelayAsync(approvalTimeout, cts.Token);
        
        var completedTask = await Task.WhenAny(approvalTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            // Handle timeout - escalate or reject
            throw new WorkflowTimeoutException("Document approval timed out after 48 hours");
        }
        
        // Step 3: Final processing after approval
        return await Workflow.ExecuteActivityAsync(() => DocumentActivities.FinalizeDocumentAsync(request));
    }
    
    [WorkflowSignal]
    public async Task ApproveDocumentAsync()
    {
        _approvalReceived = true;
    }
}
```

### Exercise 5.4: Saga Pattern Implementation

**Objective**: Implement a distributed transaction using the Saga pattern

```csharp
// Multi-service transaction with compensation
[Workflow("BookingWorkflow")]
public class BookingWorkflow
{
    [WorkflowRun]
    public async Task<BookingResult> ProcessBookingAsync(BookingRequest request)
    {
        var compensations = new List<Func<Task>>();
        
        try
        {
            // Step 1: Reserve hotel
            var hotelReservation = await Workflow.ExecuteActivityAsync(
                () => BookingActivities.ReserveHotelAsync(request));
            compensations.Add(() => BookingActivities.CancelHotelAsync(hotelReservation.Id));
            
            // Step 2: Reserve flight
            var flightReservation = await Workflow.ExecuteActivityAsync(
                () => BookingActivities.ReserveFlightAsync(request));
            compensations.Add(() => BookingActivities.CancelFlightAsync(flightReservation.Id));
            
            // Step 3: Charge payment
            var paymentResult = await Workflow.ExecuteActivityAsync(
                () => BookingActivities.ChargePaymentAsync(request));
            compensations.Add(() => BookingActivities.RefundPaymentAsync(paymentResult.TransactionId));
            
            // Step 4: Confirm all reservations
            await Workflow.ExecuteActivityAsync(
                () => BookingActivities.ConfirmBookingAsync(hotelReservation, flightReservation));
                
            return new BookingResult { Success = true, BookingId = Guid.NewGuid().ToString() };
        }
        catch (Exception)
        {
            // Execute compensations in reverse order
            compensations.Reverse();
            foreach (var compensation in compensations)
            {
                try
                {
                    await compensation();
                }
                catch (Exception compensationEx)
                {
                    Workflow.Logger.LogError(compensationEx, "Compensation failed");
                }
            }
            throw;
        }
    }
}
```

### Exercise 5.5: Advanced Patterns

**Objective**: Implement advanced Temporal patterns

1. **Child Workflows**: Break complex processes into manageable sub-workflows
2. **Continue-As-New**: Handle infinite loops and long-running processes
3. **Signals and Queries**: External interaction with running workflows
4. **Timers and Delays**: Scheduled operations and timeouts
5. **Side Effects**: Non-deterministic operations handling

## 📊 Expected Temporal Results

After completing Day 5, you should see:

### Temporal UI Dashboard
- **Workflow Executions**: 10+ completed workflows with various outcomes
- **Activity Details**: Complete execution traces and timing information
- **Error Handling**: Failed workflows with compensation patterns
- **Performance Metrics**: Execution times, retry patterns, and success rates

### Workflow Patterns Implemented
- **Saga Pattern**: Distributed transaction with compensation
- **Long-Running Processes**: Multi-day workflows with timers
- **Error Recovery**: Automatic retries and manual compensation
- **State Management**: Complex workflow state transitions

### Integration Validation
- **Temporal + Flink**: Workflow triggers from streaming events
- **Temporal + Kafka**: Event-driven workflow initiation
- **Temporal + Observability**: Complete workflow monitoring

## 🎯 Day 5 Assessment

### Knowledge Check
1. What are the key advantages of Temporal over traditional workflow solutions?
2. How does the Saga pattern handle distributed transactions?
3. What is the difference between activities and workflows in Temporal?
4. How do you handle long-running processes that span days or weeks?
5. What are the best practices for workflow versioning and deployment?

### Practical Assessment
Build a comprehensive workflow system that:
1. Implements a multi-step business process with error handling
2. Uses the Saga pattern for distributed transactions
3. Includes long-running processes with timers and signals
4. Demonstrates proper compensation and rollback logic
5. Integrates with the observability stack for monitoring

## 🎯 Day 5 Completion Checklist

- [ ] Successfully implemented enterprise order processing workflow
- [ ] Built custom workflows for your business domain
- [ ] Demonstrated Saga pattern with compensation logic
- [ ] Implemented long-running processes with timers and signals
- [ ] Validated workflow monitoring and debugging capabilities
- [ ] Integrated workflows with Flink streaming applications
- [ ] Tested error scenarios and compensation patterns
- [ ] Documented workflow patterns and troubleshooting procedures

## 📚 Preparation for Day 6

Tomorrow: **Stream Processing Patterns** - Advanced DataStream operations and window functions

**References to review:**
- [Stream Processing with Apache Flink - Chapter 6](https://www.oreilly.com/library/view/stream-processing-with/9781491974285/)
- [Event-Driven Architecture Patterns](https://martinfowler.com/articles/201701-event-driven.html)

## 🎉 Congratulations!

You've mastered **enterprise workflow orchestration** with Temporal's durable execution platform! You now have:

- ✅ **Production-grade workflows** with comprehensive error handling
- ✅ **Saga pattern implementation** for distributed transactions
- ✅ **Long-running process management** with timers and signals
- ✅ **Complete observability integration** for workflow monitoring
- ✅ **Compensation patterns** for reliable rollback scenarios
- ✅ **Real-world business process automation** capabilities

**Tomorrow**: We'll combine these workflow patterns with advanced Flink stream processing!

---

**Next**: [Day 6: Advanced Stream Processing Patterns →](../Day06-Advanced-Stream-Processing/README.md)
---

## 🗺️ Course Navigation
**[← Day 4: Enterprise Observability](../Day04-Enterprise-Observability/)** | **[Course Overview](../README.md)** | **[Next: Day 6 - Advanced Windows & Joins →](../Day06-Advanced-Windows-Joins/)**

**Course Progress**: Day 5 of 14 Complete ✅