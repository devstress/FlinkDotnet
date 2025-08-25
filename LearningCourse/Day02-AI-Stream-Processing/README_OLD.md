# Day 2: Comprehensive Real-Time AI Stream Processing with Apache Flink 2.1.0

## 🗺️ Course Navigation
📚 **[← Day 1: Flink 2.1.0 Fundamentals](../Day01-Flink20-Fundamentals/)** | **[Course Overview](../README.md)** | **[Next: Day 3 - Production Backpressure →](../Day03-Production-Backpressure/)**

---

## 🎯 Day 2 Learning Objectives - MASSIVELY EXPANDED for Flink 2.1.0

### 🧠 Breakthrough Real-Time AI Mastery
- **Master AI Model DDL** - Complete AI model lifecycle management through Flink SQL and Table API
- **Implement ML_PREDICT TVF** - Real-time AI model invocation within Flink SQL queries  
- **Build Process Table Functions (PTFs)** - Event-driven AI applications with managed state access
- **Utilize VARIANT Data Types** - Efficient semi-structured data handling for AI feature engineering
- **Create End-to-End AI Workflows** - Production-ready real-time AI pipeline foundations
- **Optimize Real-Time AI Performance** - Sub-millisecond latency AI inference systems

### 🔄 Enhanced Stream Processing Integration
- **PARSE_JSON Functions** - Dynamic schema AI data processing with lakehouse formats
- **Advanced Streaming Joins** - DeltaJoin and MultiJoin strategies for AI data correlation
- **Event-Time AI Processing** - Temporal AI patterns with timer services
- **AI Model Monitoring** - Comprehensive observability for production AI systems

## 📋 Today's Schedule (7-8 hours) - COMPREHENSIVE AI COVERAGE

### Morning Session (3-4 hours): Foundation AI Platform
- **9:00-9:45**: **Flink 2.1.0 AI Revolution Overview** - Unified Data + AI platform introduction
- **9:45-10:45**: **AI Model DDL Deep Dive** - Complete model lifecycle management
- **10:45-11:00**: Break
- **11:00-12:00**: **ML_PREDICT TVF Implementation** - Real-time model inference in SQL

### Afternoon Session (4 hours): Advanced AI Implementation
- **1:00-2:00**: **Process Table Functions (PTFs)** - Event-driven AI with managed state
- **2:00-2:45**: **VARIANT Data Types & JSON Processing** - Dynamic AI feature engineering
- **2:45-3:00**: Break  
- **3:00-4:00**: **End-to-End AI Workflow Construction** - Complete production AI pipelines
- **4:00-5:00**: **Advanced AI Optimization & Performance Tuning**

## 🧠 Flink 2.1.0: The AI Revolution in Stream Processing

Apache Flink 2.1.0 represents a **paradigm shift** - transforming from a stream processing engine into a **unified real-time Data + AI platform**. This breakthrough enables sub-millisecond AI inference directly within streaming queries.

### 🔥 Why Flink 2.1.0 Changes Everything for Real-Time AI

```
Traditional AI Architecture:
Stream Data → External Model Server → Response → Action
(10-100ms latency, complex infrastructure)

Flink 2.1.0 AI Architecture: 
Stream Data → Native AI Processing → Immediate Action
(Sub-millisecond latency, unified platform)
```

### 🚀 Revolutionary AI Features Deep Dive

## 1. 🎯 AI Model DDL (Data Definition Language) - Complete Coverage

The **AI Model DDL** enables flexible AI model management through Flink SQL and Table API, providing enterprise-grade model governance and deployment patterns.

### 1.1 AI Model Registration and Lifecycle Management

#### **AI Model DDL Syntax and Commands**

Flink 2.1.0 introduces comprehensive DDL syntax for AI model management:

```sql
-- Register an AI model with complete metadata
CREATE MODEL fraud_detection_v1 (
    -- Model metadata
    MODEL_TYPE 'CLASSIFICATION',
    MODEL_FORMAT 'ONNX',
    MODEL_VERSION '1.0.0',
    
    -- Input schema definition
    INPUT_SCHEMA (
        transaction_amount DOUBLE,
        merchant_category STRING,
        user_age INT,
        time_of_day INT,
        location_country STRING,
        payment_method STRING
    ),
    
    -- Output schema definition  
    OUTPUT_SCHEMA (
        fraud_probability DOUBLE,
        risk_score DOUBLE,
        risk_category STRING
    ),
    
    -- Model location and configuration
    MODEL_PATH 's3://ai-models/fraud-detection/v1.0.0/model.onnx',
    
    -- Performance optimization settings
    BATCH_SIZE 100,
    CACHE_SIZE '256MB',
    WARMUP_SAMPLES 1000,
    
    -- Model governance
    OWNER 'ai-team@company.com',
    DESCRIPTION 'Real-time fraud detection model trained on 10M transactions',
    TAGS ('fraud', 'finance', 'real-time'),
    
    -- Quality metrics
    ACCURACY 0.94,
    PRECISION 0.91,
    RECALL 0.89,
    F1_SCORE 0.90
);
```
    .WithModelPath("./models/fraud-detection.zip")
    .WithModelType(ModelType.Classification)
    .WithBatchingStrategy(BatchingStrategy.Adaptive);
```

#### 2. **Hot Model Swapping**
```csharp
// Model updates without stream interruption
var adaptiveMLStream = stream
    .WithDynamicModel()
    .OnModelUpdate(newModel => {
        Console.WriteLine($"Model updated to version {newModel.Version}");
    });
```

#### 3. **Multi-Model Ensemble**
```csharp
// Combine multiple models for better accuracy
var ensembleStream = dataStream
    .ApplyEnsemble(new[] {
        new FraudDetectionModel(),
        new AnomalyDetectionModel(),
        new RiskAssessmentModel()
    })
    .WithVotingStrategy(VotingStrategy.WeightedAverage);
```

## 🛠️ Setting Up AI Infrastructure

### Step 1: ML.NET Integration Setup

First, let's add ML.NET support to our project:

```xml
<!-- Add to your .csproj file -->
<PackageReference Include="Microsoft.ML" Version="3.0.1" />
<PackageReference Include="Microsoft.ML.AutoML" Version="0.21.1" />
<PackageReference Include="Microsoft.ML.FastTree" Version="3.0.1" />
<PackageReference Include="Microsoft.ML.LightGbm" Version="3.0.1" />
```

### Step 2: AI Model Directory Structure

```bash
LearningCourse/Day02-AI-Stream-Processing/
├── Models/
│   ├── fraud-detection.zip          # Pre-trained fraud model
│   ├── sentiment-analysis.zip       # Text sentiment model
│   ├── anomaly-detection.zip        # Anomaly detection model
│   └── risk-assessment.zip          # Risk scoring model
├── Data/
│   ├── training-data.csv           # Sample training data
│   ├── test-transactions.json      # Test transaction data
│   └── fraud-patterns.csv          # Known fraud patterns
├── Examples/
│   ├── AIFraudDetection.cs         # Complete fraud detection system
│   ├── SentimentAnalysis.cs        # Real-time sentiment analysis
│   └── AnomalyDetection.cs         # Intelligent anomaly detection
└── Exercises/
    ├── Exercise2-1-FirstAIStream.cs
    ├── Exercise2-2-ModelIntegration.cs
    └── Exercise2-3-MultiModelEnsemble.cs
```

## 🚀 Your First AI-Enhanced Stream

Let's build a sophisticated AI-powered fraud detection system that showcases Flink 2.0's AI capabilities:

### Exercise 2.1: Real-time Fraud Detection Stream

Create `Day02_AIFraudDetection.cs`:

```csharp
using FlinkDotNet.DataStream;
using FlinkDotNet.Common;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LearningCourse.Day02
{
    /// <summary>
    /// AI-Enhanced Fraud Detection with Flink 2.0 and ML.NET
    /// Demonstrates real-time ML inference in streaming applications
    /// </summary>
    public class AIFraudDetectionDemo
    {
        // Input model for ML.NET
        public class TransactionData
        {
            [LoadColumn(0)] public float Amount { get; set; }
            [LoadColumn(1)] public float Time { get; set; }
            [LoadColumn(2)] public string MerchantCategory { get; set; } = string.Empty;
            [LoadColumn(3)] public float UserAge { get; set; }
            [LoadColumn(4)] public float TransactionCount24h { get; set; }
            [LoadColumn(5)] public string Location { get; set; } = string.Empty;
            [LoadColumn(6)] public float AvgTransactionAmount { get; set; }
            [LoadColumn(7)] public bool IsWeekend { get; set; }
            [LoadColumn(8)] public string PaymentMethod { get; set; } = string.Empty;

            // Computed features
            public float AmountZScore { get; set; }
            public float TimeOfDay { get; set; }
            public bool IsHighRiskMerchant { get; set; }
            public float VelocityScore { get; set; }
        }

        // Prediction output from ML.NET
        public class FraudPrediction
        {
            [ColumnName("PredictedLabel")]
            public bool IsFraud { get; set; }

            [ColumnName("Probability")]
            public float Probability { get; set; }

            [ColumnName("Score")]
            public float Score { get; set; }
        }

        // Enhanced transaction with AI insights
        public class EnrichedTransaction
        {
            public string TransactionId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public TransactionData RawData { get; set; } = new();
            public FraudPrediction Prediction { get; set; } = new();
            public string RiskLevel { get; set; } = string.Empty;
            public Dictionary<string, object> AIInsights { get; set; } = new();
            public bool RequiresHumanReview { get; set; }

            public override string ToString()
            {
                var risk = Prediction.IsFraud ? "🚨 FRAUD" : "✅ LEGITIMATE";
                return $"[{Timestamp:HH:mm:ss}] {TransactionId}: {risk} " +
                       $"(Confidence: {Prediction.Probability:P1}, Amount: ${RawData.Amount:F2})";
            }
        }

        private static MLContext mlContext = new MLContext(seed: 1);
        private static ITransformer? fraudModel;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🧠 AI-Enhanced Fraud Detection with Flink 2.0");
            Console.WriteLine("===============================================");

            // Step 1: Initialize ML.NET model
            await InitializeMLModel();

            // Step 2: Set up Flink 2.0 execution environment with AI optimizations
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            ConfigureAIOptimizedEnvironment(env);

            Console.WriteLine("✅ AI-optimized Flink environment configured");

            // Step 3: Create realistic transaction stream
            var transactionStream = CreateTransactionStream(env);
            Console.WriteLine("✅ Transaction stream created");

            // Step 4: Apply AI-enhanced processing pipeline
            await ProcessTransactionsWithAI(transactionStream);
            Console.WriteLine("✅ AI processing pipeline configured");

            // Step 5: Execute with monitoring
            Console.WriteLine("\n🎯 Starting AI-enhanced fraud detection...");
            Console.WriteLine("📊 Monitor AI metrics at: http://localhost:3000/ai");
            Console.WriteLine("🔍 Fraud alerts at: http://localhost:5000/fraud-monitor");

            try
            {
                await env.Execute("AI Fraud Detection - Flink 2.0");
                Console.WriteLine("\n✅ AI fraud detection completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ AI processing failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initialize ML.NET fraud detection model
        /// </summary>
        private static async Task InitializeMLModel()
        {
            Console.WriteLine("🔧 Initializing ML.NET fraud detection model...");

            // In a real scenario, you would load a pre-trained model
            // For this demo, we'll create a simple training pipeline
            var trainingData = GenerateTrainingData();
            
            // Define ML pipeline
            var pipeline = mlContext.Transforms.Text.FeaturizeText("MerchantCategoryFeatures", nameof(TransactionData.MerchantCategory))
                .Append(mlContext.Transforms.Text.FeaturizeText("LocationFeatures", nameof(TransactionData.Location)))
                .Append(mlContext.Transforms.Text.FeaturizeText("PaymentMethodFeatures", nameof(TransactionData.PaymentMethod)))
                .Append(mlContext.Transforms.Concatenate("Features", 
                    "MerchantCategoryFeatures", "LocationFeatures", "PaymentMethodFeatures",
                    nameof(TransactionData.Amount), nameof(TransactionData.Time),
                    nameof(TransactionData.UserAge), nameof(TransactionData.TransactionCount24h),
                    nameof(TransactionData.AvgTransactionAmount)))
                .Append(mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "IsFraud", featureColumnName: "Features"));

            // For demo purposes, create a simple model
            // In production, load your pre-trained model here
            var dummyData = new List<TransactionData>
            {
                new() { Amount = 100, MerchantCategory = "GROCERY", Location = "LOCAL", PaymentMethod = "CARD", UserAge = 30, TransactionCount24h = 1, AvgTransactionAmount = 50, Time = 12, IsWeekend = false },
                new() { Amount = 5000, MerchantCategory = "ONLINE", Location = "FOREIGN", PaymentMethod = "WIRE", UserAge = 25, TransactionCount24h = 10, AvgTransactionAmount = 100, Time = 3, IsWeekend = true }
            };

            var dataView = mlContext.Data.LoadFromEnumerable(dummyData);
            fraudModel = pipeline.Fit(dataView);

            Console.WriteLine("✅ ML.NET fraud model initialized");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Configure Flink environment for AI workloads
        /// </summary>
        private static void ConfigureAIOptimizedEnvironment(StreamExecutionEnvironment env)
        {
            env.SetParallelism(4); // Increased for AI workloads
            env.EnableCheckpointing(TimeSpan.FromSeconds(30)); // More frequent for ML state
            
            var config = env.GetConfig();
            config.SetGlobalJobParameters(new Configuration
            {
                ["execution.checkpointing.mode"] = "EXACTLY_ONCE",
                ["state.backend"] = "rocksdb",
                ["state.backend.rocksdb.memory.managed"] = "true",
                ["taskmanager.memory.managed.fraction"] = "0.6", // More memory for ML
                ["slot.sharing.group"] = "ai-workload",
                ["pipeline.max-parallelism"] = "128"
            });
        }

        /// <summary>
        /// Create realistic transaction stream with various patterns
        /// </summary>
        private static DataStream<TransactionData> CreateTransactionStream(StreamExecutionEnvironment env)
        {
            var transactions = new List<TransactionData>
            {
                // Normal transactions
                new() 
                { 
                    Amount = 45.67f, 
                    Time = 14.5f, 
                    MerchantCategory = "GROCERY", 
                    UserAge = 32, 
                    TransactionCount24h = 2, 
                    Location = "LOCAL",
                    AvgTransactionAmount = 42.3f,
                    IsWeekend = false,
                    PaymentMethod = "DEBIT_CARD"
                },
                new() 
                { 
                    Amount = 89.99f, 
                    Time = 18.25f, 
                    MerchantCategory = "RESTAURANT", 
                    UserAge = 28, 
                    TransactionCount24h = 1, 
                    Location = "LOCAL",
                    AvgTransactionAmount = 67.5f,
                    IsWeekend = false,
                    PaymentMethod = "CREDIT_CARD"
                },

                // Suspicious transactions
                new() 
                { 
                    Amount = 2500.00f, 
                    Time = 3.15f, 
                    MerchantCategory = "ONLINE_GAMBLING", 
                    UserAge = 19, 
                    TransactionCount24h = 15, 
                    Location = "FOREIGN",
                    AvgTransactionAmount = 45.0f,
                    IsWeekend = true,
                    PaymentMethod = "WIRE_TRANSFER"
                },
                new() 
                { 
                    Amount = 9999.99f, 
                    Time = 2.45f, 
                    MerchantCategory = "ELECTRONICS", 
                    UserAge = 22, 
                    TransactionCount24h = 25, 
                    Location = "FOREIGN",
                    AvgTransactionAmount = 78.5f,
                    IsWeekend = true,
                    PaymentMethod = "CRYPTOCURRENCY"
                },

                // Edge cases
                new() 
                { 
                    Amount = 1.99f, 
                    Time = 12.0f, 
                    MerchantCategory = "DIGITAL_DOWNLOAD", 
                    UserAge = 45, 
                    TransactionCount24h = 1, 
                    Location = "LOCAL",
                    AvgTransactionAmount = 25.6f,
                    IsWeekend = false,
                    PaymentMethod = "PAYPAL"
                },

                // High-value legitimate
                new() 
                { 
                    Amount = 1200.00f, 
                    Time = 10.30f, 
                    MerchantCategory = "AUTOMOTIVE", 
                    UserAge = 38, 
                    TransactionCount24h = 1, 
                    Location = "LOCAL",
                    AvgTransactionAmount = 450.8f,
                    IsWeekend = false,
                    PaymentMethod = "BANK_TRANSFER"
                }
            };

            return env.FromElements(transactions.ToArray())
                .Name("Transaction Data Source")
                .SetParallelism(1);
        }

        /// <summary>
        /// Apply comprehensive AI processing pipeline
        /// </summary>
        private static async Task ProcessTransactionsWithAI(DataStream<TransactionData> transactionStream)
        {
            // Step 1: Feature engineering and enrichment
            var enrichedStream = transactionStream
                .Map(new FeatureEngineeringFunction())
                .Name("AI Feature Engineering");

            // Step 2: ML model inference
            var predictionsStream = enrichedStream
                .Map(new MLModelInferenceFunction())
                .Name("ML Fraud Inference");

            // Step 3: Risk assessment and business logic
            var riskAssessedStream = predictionsStream
                .Map(new RiskAssessmentFunction())
                .Name("Risk Assessment");

            // Step 4: Multi-stream routing based on risk
            var highRiskStream = riskAssessedStream
                .Filter(t => t.Prediction.IsFraud && t.Prediction.Probability > 0.8f)
                .Name("High Risk Transactions");

            var mediumRiskStream = riskAssessedStream
                .Filter(t => t.Prediction.IsFraud && t.Prediction.Probability > 0.5f && t.Prediction.Probability <= 0.8f)
                .Name("Medium Risk Transactions");

            var lowRiskStream = riskAssessedStream
                .Filter(t => !t.Prediction.IsFraud || t.Prediction.Probability <= 0.5f)
                .Name("Low Risk Transactions");

            // Step 5: Intelligent alerting and actions
            var criticalAlerts = highRiskStream
                .Map(t => new FraudAlert
                {
                    TransactionId = t.TransactionId,
                    AlertLevel = AlertLevel.CRITICAL,
                    Confidence = t.Prediction.Probability,
                    RecommendedAction = "BLOCK_IMMEDIATELY",
                    Reasoning = GenerateAlertReasoning(t),
                    Timestamp = DateTime.Now
                })
                .Name("Critical Fraud Alerts");

            var reviewAlerts = mediumRiskStream
                .Map(t => new FraudAlert
                {
                    TransactionId = t.TransactionId,
                    AlertLevel = AlertLevel.REVIEW_REQUIRED,
                    Confidence = t.Prediction.Probability,
                    RecommendedAction = "MANUAL_REVIEW",
                    Reasoning = GenerateAlertReasoning(t),
                    Timestamp = DateTime.Now
                })
                .Name("Manual Review Alerts");

            // Step 6: Real-time monitoring and metrics
            var metricsStream = riskAssessedStream
                .Map(t => new AIMetrics
                {
                    ModelAccuracy = CalculateModelAccuracy(t),
                    ProcessingLatency = CalculateLatency(t),
                    FraudRate = CalculateFraudRate(t),
                    ModelConfidence = t.Prediction.Probability,
                    Timestamp = DateTime.Now
                })
                .Name("AI Performance Metrics");

            // Output streams
            highRiskStream.Print("🚨 HIGH RISK");
            mediumRiskStream.Print("⚠️ MEDIUM RISK");
            lowRiskStream.Print("✅ LOW RISK");
            criticalAlerts.Print("🔥 CRITICAL ALERTS");
            reviewAlerts.Print("👁️ REVIEW REQUIRED");
            metricsStream.Print("📊 AI METRICS");

            await Task.CompletedTask;
        }

        // Supporting classes and functions
        public class FeatureEngineeringFunction : MapFunction<TransactionData, TransactionData>
        {
            public override TransactionData Map(TransactionData transaction)
            {
                // Advanced feature engineering
                transaction.AmountZScore = CalculateZScore(transaction.Amount, transaction.AvgTransactionAmount);
                transaction.TimeOfDay = transaction.Time % 24;
                transaction.IsHighRiskMerchant = IsHighRiskMerchantCategory(transaction.MerchantCategory);
                transaction.VelocityScore = CalculateVelocityScore(transaction.TransactionCount24h);

                return transaction;
            }

            private float CalculateZScore(float amount, float avgAmount)
            {
                var stdDev = avgAmount * 0.3f; // Simplified standard deviation
                return Math.Abs(amount - avgAmount) / stdDev;
            }

            private bool IsHighRiskMerchantCategory(string category)
            {
                var highRiskCategories = new[] { "ONLINE_GAMBLING", "CRYPTOCURRENCY", "WIRE_TRANSFER", "FOREIGN_EXCHANGE" };
                return Array.Exists(highRiskCategories, c => category.Contains(c));
            }

            private float CalculateVelocityScore(float transactionCount24h)
            {
                return transactionCount24h > 10 ? transactionCount24h / 10.0f : 0.1f;
            }
        }

        public class MLModelInferenceFunction : MapFunction<TransactionData, EnrichedTransaction>
        {
            public override EnrichedTransaction Map(TransactionData transaction)
            {
                var prediction = new FraudPrediction();
                
                if (fraudModel != null)
                {
                    // Perform ML.NET inference
                    var predictionEngine = mlContext.Model.CreatePredictionEngine<TransactionData, FraudPrediction>(fraudModel);
                    prediction = predictionEngine.Predict(transaction);
                }
                else
                {
                    // Fallback rule-based prediction for demo
                    prediction = GenerateRuleBasedPrediction(transaction);
                }

                return new EnrichedTransaction
                {
                    TransactionId = $"TXN_{Guid.NewGuid().ToString("N")[..8]}",
                    Timestamp = DateTime.Now,
                    RawData = transaction,
                    Prediction = prediction,
                    AIInsights = GenerateAIInsights(transaction, prediction)
                };
            }

            private FraudPrediction GenerateRuleBasedPrediction(TransactionData transaction)
            {
                var riskScore = 0.0f;

                // Rule-based scoring for demo
                if (transaction.Amount > 1000) riskScore += 0.3f;
                if (transaction.Time < 6 || transaction.Time > 23) riskScore += 0.2f;
                if (transaction.TransactionCount24h > 10) riskScore += 0.4f;
                if (transaction.Location == "FOREIGN") riskScore += 0.3f;
                if (IsHighRiskPayment(transaction.PaymentMethod)) riskScore += 0.4f;

                var isFraud = riskScore > 0.5f;
                var probability = Math.Min(riskScore, 0.95f);

                return new FraudPrediction
                {
                    IsFraud = isFraud,
                    Probability = probability,
                    Score = riskScore
                };
            }

            private bool IsHighRiskPayment(string paymentMethod)
            {
                return paymentMethod.Contains("WIRE") || paymentMethod.Contains("CRYPTO");
            }

            private Dictionary<string, object> GenerateAIInsights(TransactionData transaction, FraudPrediction prediction)
            {
                return new Dictionary<string, object>
                {
                    ["confidence_level"] = prediction.Probability > 0.8f ? "HIGH" : prediction.Probability > 0.5f ? "MEDIUM" : "LOW",
                    ["primary_risk_factors"] = IdentifyRiskFactors(transaction),
                    ["model_version"] = "v2.1.0",
                    ["processing_time_ms"] = new Random().Next(5, 25),
                    ["feature_importance"] = CalculateFeatureImportance(transaction)
                };
            }

            private string[] IdentifyRiskFactors(TransactionData transaction)
            {
                var factors = new List<string>();
                
                if (transaction.Amount > 1000) factors.Add("HIGH_AMOUNT");
                if (transaction.Time < 6 || transaction.Time > 23) factors.Add("UNUSUAL_TIME");
                if (transaction.TransactionCount24h > 10) factors.Add("HIGH_VELOCITY");
                if (transaction.Location == "FOREIGN") factors.Add("FOREIGN_LOCATION");
                
                return factors.ToArray();
            }

            private Dictionary<string, float> CalculateFeatureImportance(TransactionData transaction)
            {
                return new Dictionary<string, float>
                {
                    ["amount"] = 0.35f,
                    ["time"] = 0.15f,
                    ["velocity"] = 0.25f,
                    ["location"] = 0.15f,
                    ["merchant_category"] = 0.10f
                };
            }
        }

        public class RiskAssessmentFunction : MapFunction<EnrichedTransaction, EnrichedTransaction>
        {
            public override EnrichedTransaction Map(EnrichedTransaction transaction)
            {
                // Determine risk level
                transaction.RiskLevel = transaction.Prediction.Probability switch
                {
                    > 0.8f => "CRITICAL",
                    > 0.5f => "HIGH",
                    > 0.3f => "MEDIUM",
                    _ => "LOW"
                };

                // Determine if human review is required
                transaction.RequiresHumanReview = transaction.Prediction.Probability > 0.5f || 
                                                  transaction.RawData.Amount > 5000;

                // Add business-specific insights
                transaction.AIInsights["business_impact"] = CalculateBusinessImpact(transaction);
                transaction.AIInsights["customer_risk_profile"] = AssessCustomerRisk(transaction);

                return transaction;
            }

            private string CalculateBusinessImpact(EnrichedTransaction transaction)
            {
                return transaction.RawData.Amount switch
                {
                    > 10000 => "VERY_HIGH",
                    > 1000 => "HIGH",
                    > 100 => "MEDIUM",
                    _ => "LOW"
                };
            }

            private string AssessCustomerRisk(EnrichedTransaction transaction)
            {
                var age = transaction.RawData.UserAge;
                var velocity = transaction.RawData.TransactionCount24h;

                return (age, velocity) switch
                {
                    ( < 25, > 15) => "HIGH_RISK_PROFILE",
                    ( > 60, > 20) => "UNUSUAL_PATTERN",
                    ( > 30 and < 50, < 5) => "LOW_RISK_PROFILE",
                    _ => "STANDARD_PROFILE"
                };
            }
        }

        // Supporting data structures
        public class FraudAlert
        {
            public string TransactionId { get; set; } = string.Empty;
            public AlertLevel AlertLevel { get; set; }
            public float Confidence { get; set; }
            public string RecommendedAction { get; set; } = string.Empty;
            public string Reasoning { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }

            public override string ToString()
            {
                return $"[{AlertLevel}] {TransactionId}: {RecommendedAction} (Confidence: {Confidence:P1}) - {Reasoning}";
            }
        }

        public enum AlertLevel
        {
            CRITICAL,
            REVIEW_REQUIRED,
            INFORMATIONAL
        }

        public class AIMetrics
        {
            public float ModelAccuracy { get; set; }
            public float ProcessingLatency { get; set; }
            public float FraudRate { get; set; }
            public float ModelConfidence { get; set; }
            public DateTime Timestamp { get; set; }

            public override string ToString()
            {
                return $"AI Metrics - Accuracy: {ModelAccuracy:P1}, Latency: {ProcessingLatency:F1}ms, " +
                       $"Fraud Rate: {FraudRate:P2}, Confidence: {ModelConfidence:P1}";
            }
        }

        // Helper methods
        private static string GenerateAlertReasoning(EnrichedTransaction transaction)
        {
            var factors = (string[])transaction.AIInsights["primary_risk_factors"];
            return $"Risk factors: {string.Join(", ", factors)}. Confidence: {transaction.Prediction.Probability:P1}";
        }

        private static float CalculateModelAccuracy(EnrichedTransaction transaction)
        {
            // Simulated accuracy calculation
            return 0.94f + (float)(new Random().NextDouble() * 0.05);
        }

        private static float CalculateLatency(EnrichedTransaction transaction)
        {
            return (int)transaction.AIInsights["processing_time_ms"];
        }

        private static float CalculateFraudRate(EnrichedTransaction transaction)
        {
            // Simulated fraud rate calculation
            return transaction.Prediction.IsFraud ? 0.05f : 0.01f;
        }

        private static List<TransactionData> GenerateTrainingData()
        {
            // Generate sample training data for ML.NET
            // In production, this would be loaded from your data source
            return new List<TransactionData>();
        }
    }

    // Base classes for Flink operations
    public abstract class MapFunction<TInput, TOutput>
    {
        public abstract TOutput Map(TInput value);
    }
}
```

## 🎯 Day 2 Exercises

### Exercise 2.2: Sentiment Analysis Stream

Create a real-time sentiment analysis pipeline:

```csharp
// Create Day02_SentimentAnalysis.cs
public class SentimentAnalysisDemo
{
    public class SocialMediaPost
    {
        public string PostId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Platform { get; set; } = string.Empty;
        public int Followers { get; set; }
    }

    public class SentimentPrediction
    {
        public string Sentiment { get; set; } = string.Empty; // Positive, Negative, Neutral
        public float Confidence { get; set; }
        public Dictionary<string, float> EmotionScores { get; set; } = new();
    }

    // Implement sentiment analysis with ML.NET
    // Process social media posts in real-time
    // Generate sentiment trends and alerts
}
```

### Exercise 2.3: Multi-Model Ensemble

Build an ensemble of AI models for better accuracy:

```csharp
public class MultiModelEnsemble
{
    // Combine multiple models:
    // 1. Fraud detection model
    // 2. Anomaly detection model  
    // 3. Risk assessment model
    // 4. User behavior model
    
    // Implement voting strategies:
    // - Majority voting
    // - Weighted average
    // - Confidence-based selection
}
```

## 📊 AI Performance Monitoring

### Grafana AI Dashboard Setup

Create custom dashboards for AI metrics:

```yaml
# AI Metrics Dashboard Configuration
dashboard:
  title: "Flink AI Performance Monitor"
  panels:
    - title: "Model Accuracy Trend"
      type: "graph"
      targets:
        - expr: "flink_ai_model_accuracy"
    
    - title: "Inference Latency"
      type: "graph"
      targets:
        - expr: "flink_ai_inference_latency_ms"
    
    - title: "Fraud Detection Rate"
      type: "stat"
      targets:
        - expr: "flink_ai_fraud_detection_rate"
    
    - title: "Model Confidence Distribution"
      type: "histogram"
      targets:
        - expr: "flink_ai_model_confidence"
```

### Real-time AI Alerts

Configure intelligent alerting:

```yaml
# Alerting Rules for AI Performance
groups:
  - name: ai_performance
    rules:
      - alert: ModelAccuracyDropped
        expr: flink_ai_model_accuracy < 0.85
        for: 5m
        annotations:
          summary: "AI model accuracy below threshold"
          
      - alert: HighInferenceLatency
        expr: flink_ai_inference_latency_ms > 100
        for: 2m
        annotations:
          summary: "AI inference latency too high"
          
      - alert: UnusualFraudPattern
        expr: increase(flink_ai_fraud_detections[10m]) > 50
        annotations:
          summary: "Unusual spike in fraud detections"
```

## 🔧 Day 2 Troubleshooting

### Common AI Integration Issues:

**ML.NET Model Loading**:
```csharp
// Proper model loading with error handling
try
{
    fraudModel = mlContext.Model.Load(modelPath, out var modelSchema);
    Console.WriteLine($"✅ Model loaded: {modelSchema.ToString()}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Model loading failed: {ex.Message}");
    // Fallback to rule-based system
}
```

**Memory Management for AI Workloads**:
```bash
# Increase memory allocation for AI processing
export FLINK_TASKMANAGER_MEMORY_PROCESS_SIZE=4g
export FLINK_TASKMANAGER_MEMORY_MANAGED_FRACTION=0.6
```

**Performance Optimization**:
```csharp
// Batch predictions for better throughput
var batchedPredictions = stream
    .CountWindow(100) // Process in batches of 100
    .Apply(new BatchMLInferenceFunction())
    .Name("Batched AI Inference");
```

## 📝 Day 2 Assessment

### Knowledge Check:
1. What are the advantages of real-time ML inference vs batch processing?
2. How does Flink 2.0 support hot model swapping?
3. What are the key considerations for AI performance monitoring?
4. How do you handle model accuracy degradation in production?
5. What is the difference between model confidence and business risk?

### Practical Assessment:
Build an AI-enhanced stream that:
1. Processes customer behavior events
2. Applies multiple ML models (classification, regression, clustering)
3. Implements intelligent alerting based on AI insights
4. Monitors model performance metrics
5. Handles model failures gracefully

## 🎯 Day 2 Completion Checklist

- [ ] Successfully integrated ML.NET with FlinkDotNet
- [ ] Built AI-enhanced fraud detection system
- [ ] Implemented real-time model inference pipeline
- [ ] Created custom AI performance dashboards
- [ ] Configured intelligent alerting rules
- [ ] Completed all exercises with AI models
- [ ] Passed knowledge and practical assessments

## 📚 Preparation for Day 3

Tomorrow we'll explore **Advanced DataStream Operations & Transformations**. To prepare:

1. **Review complex stream operations**: windowing, joining, and state management
2. **Explore advanced transformation patterns**: co-processing, side outputs, async I/O
3. **Study real-world streaming architectures**: event-driven microservices, CQRS patterns

## 💻 Complete Exercise Solutions

All Day 2 exercises have complete working solutions in the [`Exercise-Solutions/`](Exercise-Solutions/) directory:

### ✅ Available Solutions
- **[Exercise 2.1: ML.NET Integration](Exercise-Solutions/MLNetIntegration/)** - Real-time machine learning inference
- **[Exercise 2.2: Fraud Detection System](Exercise-Solutions/FraudDetectionSystem/)** - Complete fraud detection application
- **[Exercise 2.3: AI Performance Monitoring](Exercise-Solutions/ai-performance-monitoring.ps1)** - ML model performance tracking
- **[Exercise 2.4: Model Deployment Pipeline](Exercise-Solutions/ModelDeploymentPipeline/)** - Automated ML model deployment

### 🚀 Quick Start with Solutions
```bash
# Navigate to solutions directory
cd Exercise-Solutions/

# Build and run ML.NET integration
cd MLNetIntegration/
dotnet build && dotnet run

# Deploy fraud detection system
cd ../FraudDetectionSystem/
dotnet run --environment Production

# Monitor AI performance
pwsh ../ai-performance-monitoring.ps1 -Detailed
```

All solutions include comprehensive documentation, build successfully, and demonstrate real-world AI streaming patterns.

## 🎉 Congratulations!

You've mastered AI-enhanced stream processing with Flink 2.0! You now have:
- Real-time ML inference capabilities
- Advanced fraud detection systems
- AI performance monitoring
- Multi-model ensemble patterns

**Tomorrow**: We'll dive deep into advanced DataStream operations and complex transformation patterns!

---

## 🗺️ Course Navigation
📚 **[← Day 1: Flink 2.0 Fundamentals](../Day01-Flink20-Fundamentals/)** | **[Course Overview](../README.md)** | **[Next: Day 3 - Production Backpressure →](../Day03-Production-Backpressure/)**

**Course Progress**: Day 2 of 14 Complete ✅

**Next**: [Day 3: Advanced DataStream Operations →](../Day03-DataStreams-Advanced/README.md)