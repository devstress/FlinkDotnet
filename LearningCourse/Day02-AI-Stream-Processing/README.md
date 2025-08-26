# Day 2: Comprehensive Real-Time AI Stream Processing with Apache Flink 2.1.0

## 🗺️ Course Navigation
📚 **[← Day 1: Flink 2.1.0 Fundamentals](../Day01-Flink21-Fundamentals/)** | **[Course Overview](../README.md)** | **[Next: Day 3 - Production Backpressure →](../Day03-Production-Backpressure/)**

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

-- Advanced model versioning and lifecycle
CREATE MODEL sentiment_analysis_v2 (
    MODEL_TYPE 'NLP_CLASSIFICATION',
    MODEL_FORMAT 'TENSORFLOW',
    MODEL_VERSION '2.1.0',
    
    -- Inherits from previous version with overrides
    INHERITS_FROM sentiment_analysis_v1,
    
    -- A/B testing configuration
    TRAFFIC_SPLIT (
        'v1' -> 0.2,
        'v2' -> 0.8
    ),
    
    -- Auto-rollback conditions
    ROLLBACK_CONDITIONS (
        ACCURACY < 0.85,
        LATENCY_P99 > '100ms',
        ERROR_RATE > 0.01
    ),
    
    -- Monitoring and alerting
    ALERTS (
        ACCURACY_DEGRADATION THRESHOLD 0.02,
        LATENCY_SPIKE THRESHOLD '200ms',
        MEMORY_USAGE THRESHOLD '512MB'
    )
);
```

#### **Model Lifecycle Operations**

```sql
-- List all registered models
SHOW MODELS;

-- Get detailed model information
DESCRIBE MODEL fraud_detection_v1;

-- Update model metadata
ALTER MODEL fraud_detection_v1 
SET DESCRIPTION = 'Updated fraud detection with enhanced feature engineering';

-- Version management
CREATE MODEL fraud_detection_v2 
INHERITS_FROM fraud_detection_v1
SET MODEL_PATH = 's3://ai-models/fraud-detection/v2.0.0/model.onnx',
    MODEL_VERSION = '2.0.0',
    ACCURACY = 0.96;

-- Model deployment and traffic management  
ALTER MODEL fraud_detection_v2
SET TRAFFIC_SPLIT (
    'v1' -> 0.1,
    'v2' -> 0.9
);

-- Model retirement
DROP MODEL fraud_detection_v1;
```

### 1.2 Enterprise Model Governance Patterns

```sql
-- Multi-environment model management
CREATE MODEL production.fraud_detection (
    MODEL_TYPE 'CLASSIFICATION',
    ENVIRONMENT 'PRODUCTION',
    
    -- Strict production requirements
    SLA_LATENCY_P99 '50ms',
    SLA_AVAILABILITY '99.9%',
    SLA_ACCURACY '0.92',
    
    -- Compliance and auditing
    COMPLIANCE_TAGS ('PCI-DSS', 'GDPR', 'SOX'),
    AUDIT_LEVEL 'FULL',
    DATA_RETENTION '7_YEARS',
    
    -- Security settings
    ENCRYPTION 'AES_256',
    ACCESS_CONTROL 'RBAC',
    
    -- Resource allocation
    CPU_LIMIT '4_CORES',
    MEMORY_LIMIT '8GB',
    GPU_ALLOCATION '1_T4'
);

-- Model testing and validation
CREATE MODEL staging.fraud_detection_candidate (
    INHERITS_FROM production.fraud_detection,
    ENVIRONMENT 'STAGING',
    
    -- Automated testing configuration
    TEST_DATASETS (
        'historical_fraud_cases',
        'edge_case_scenarios',
        'adversarial_examples'
    ),
    
    -- Validation criteria
    VALIDATION_RULES (
        BIAS_DETECTION ENABLED,
        FAIRNESS_METRICS ('demographic_parity', 'equalized_odds'),
        EXPLAINABILITY_REQUIRED TRUE
    )
);
```

## 2. ⚡ ML_PREDICT Table-Valued Function (TVF) - Deep Implementation

The **ML_PREDICT TVF** enables real-time AI model invocation directly within Flink SQL, providing sub-millisecond inference capabilities.

### 2.1 Basic ML_PREDICT Usage Patterns

```sql
-- Real-time fraud detection in streaming transactions
SELECT 
    transaction_id,
    user_id,
    amount,
    
    -- AI model inference directly in SQL
    ML_PREDICT(
        'fraud_detection_v2',  -- Model name
        transaction_amount,
        merchant_category, 
        user_age,
        time_of_day,
        location_country,
        payment_method
    ) AS (fraud_probability, risk_score, risk_category)
    
FROM transaction_stream
WHERE amount > 100;  -- Focus on higher-value transactions
```

### 2.2 Advanced ML_PREDICT Patterns

```sql
-- Multi-model ensemble inference
SELECT 
    transaction_id,
    
    -- Primary fraud model
    ML_PREDICT('fraud_detection_v2', *) AS primary_result,
    
    -- Secondary validation model
    ML_PREDICT('fraud_validation_model', *) AS validation_result,
    
    -- Behavioral anomaly detection
    ML_PREDICT('behavioral_anomaly', 
        user_id, 
        LAST_VALUE(amount) OVER (
            PARTITION BY user_id 
            ORDER BY processing_time 
            RANGE INTERVAL '1' HOUR PRECEDING
        )
    ) AS anomaly_result,
    
    -- Risk scoring
    ML_PREDICT('risk_scoring_ensemble', 
        amount, merchant_category, time_of_day
    ) AS risk_result

FROM transaction_stream;
```

### 2.3 Dynamic Model Selection

```sql
-- Conditional model selection based on transaction characteristics
SELECT 
    transaction_id,
    
    CASE 
        WHEN amount > 10000 THEN 
            ML_PREDICT('high_value_fraud_model', *)
        WHEN merchant_category = 'ONLINE' THEN
            ML_PREDICT('online_fraud_model', *)  
        WHEN time_of_day BETWEEN 0 AND 6 THEN
            ML_PREDICT('night_fraud_model', *)
        ELSE 
            ML_PREDICT('general_fraud_model', *)
    END AS fraud_prediction
    
FROM transaction_stream;
```

### 2.4 Real-Time Feature Engineering with ML_PREDICT

```sql
-- Advanced feature engineering for AI models
WITH enriched_transactions AS (
    SELECT 
        *,
        
        -- Time-based features
        EXTRACT(HOUR FROM transaction_time) AS hour_of_day,
        EXTRACT(DAY_OF_WEEK FROM transaction_time) AS day_of_week,
        
        -- User behavioral features  
        COUNT(*) OVER (
            PARTITION BY user_id 
            ORDER BY transaction_time 
            RANGE INTERVAL '1' DAY PRECEDING
        ) AS daily_transaction_count,
        
        SUM(amount) OVER (
            PARTITION BY user_id
            ORDER BY transaction_time
            RANGE INTERVAL '1' HOUR PRECEDING  
        ) AS hourly_spending,
        
        -- Location velocity features
        LAG(location_country, 1) OVER (
            PARTITION BY user_id 
            ORDER BY transaction_time
        ) AS previous_country
        
    FROM transaction_stream
)

SELECT 
    transaction_id,
    
    -- AI inference with enriched features
    ML_PREDICT(
        'advanced_fraud_model',
        amount,
        merchant_category,
        hour_of_day,
        day_of_week, 
        daily_transaction_count,
        hourly_spending,
        CASE 
            WHEN location_country != previous_country THEN 1
            ELSE 0
        END AS location_change_flag
    ) AS (fraud_probability, confidence_score, explanation)
    
FROM enriched_transactions;
```

## 3. 🔄 Process Table Functions (PTFs) - Event-Driven AI Applications

**Process Table Functions** open up the Flink SQL engine for sophisticated event-driven AI applications with full access to Flink's managed state, event-time services, and table changelogs.

### 3.1 Stateful AI Processing with PTFs

```sql
-- Create a stateful AI processor for behavioral analysis
CREATE FUNCTION behavioral_ai_processor AS 'com.company.ai.BehavioralAIProcessor'
LANGUAGE JAVA
USING JAR 'file:///path/to/ai-processors.jar';

-- Use PTF for complex user behavior modeling
SELECT 
    user_id,
    behavioral_ai_processor(
        action_type,
        action_timestamp, 
        action_metadata,
        
        -- Access to managed state
        STATE('user_profile'),
        STATE('session_history'),
        
        -- Timer services for temporal patterns
        TIMER('session_timeout', INTERVAL '30' MINUTE),
        TIMER('daily_reset', TIME '00:00:00')
    ) AS (
        updated_profile,
        anomaly_score,
        risk_factors,
        recommended_actions
    )
FROM user_action_stream
GROUP BY user_id;
```

### 3.2 Real-Time AI Model Training with PTFs

```sql
-- Online learning with streaming data
CREATE FUNCTION online_model_trainer AS 'com.company.ai.OnlineModelTrainer'
LANGUAGE JAVA;

SELECT 
    model_id,
    online_model_trainer(
        feature_vector,
        ground_truth_label,
        
        -- Access to model state
        STATE('model_weights'),
        STATE('gradient_accumulator'),
        STATE('training_statistics'),
        
        -- Training configuration
        PARAMETER('learning_rate', 0.001),
        PARAMETER('batch_size', 100),
        PARAMETER('regularization', 0.01)
    ) AS (
        updated_model_metrics,
        accuracy_score,
        training_loss
    )
FROM labeled_training_stream
GROUP BY model_id;
```

### 3.3 Complex Event Processing for AI

```sql
-- AI-enhanced pattern detection
CREATE FUNCTION fraud_pattern_detector AS 'com.company.ai.FraudPatternDetector'
LANGUAGE JAVA;

SELECT 
    user_id,
    fraud_pattern_detector(
        transaction_amount,
        merchant_id,
        transaction_time,
        
        -- Access to transaction history state
        STATE('transaction_history'),
        STATE('merchant_patterns'),
        STATE('velocity_tracking'),
        
        -- Pattern detection configuration
        PARAMETER('lookback_window', INTERVAL '7' DAY),
        PARAMETER('anomaly_threshold', 0.95),
        PARAMETER('pattern_types', ARRAY['velocity', 'amount', 'merchant', 'location'])
    ) AS (
        pattern_matches,
        risk_score,
        alert_level,
        pattern_explanations
    )
FROM transaction_stream
GROUP BY user_id;
```

## 4. 📊 VARIANT Data Types & Dynamic Schema AI Processing

**VARIANT data types** enable efficient handling of semi-structured data like JSON, enabling dynamic schema AI feature engineering.

### 4.1 VARIANT Data Type Fundamentals

```sql
-- Table with VARIANT columns for flexible AI feature storage
CREATE TABLE ai_feature_store (
    event_id STRING,
    user_id STRING,
    event_timestamp TIMESTAMP(3),
    
    -- Flexible feature storage
    user_features VARIANT,
    session_features VARIANT, 
    behavioral_features VARIANT,
    
    -- Computed features
    ai_predictions VARIANT
);

-- Insert complex nested features
INSERT INTO ai_feature_store VALUES (
    'event_123',
    'user_456', 
    CURRENT_TIMESTAMP,
    
    -- User features as JSON
    PARSE_JSON('{
        "demographics": {
            "age": 32,
            "location": "USA",
            "income_bracket": "middle"
        },
        "preferences": {
            "categories": ["electronics", "books"],
            "price_sensitivity": 0.7
        },
        "history": {
            "total_purchases": 156,
            "avg_order_value": 89.50,
            "last_purchase_days": 3
        }
    }'),
    
    -- Session features
    PARSE_JSON('{
        "session_duration": 1800,
        "pages_viewed": 12,
        "items_clicked": 5,
        "cart_actions": 3
    }'),
    
    -- Behavioral features
    PARSE_JSON('{
        "engagement_score": 0.85,
        "risk_indicators": ["unusual_time", "new_device"],
        "pattern_matches": {
            "shopping_pattern": "weekend_browser",
            "payment_pattern": "credit_card_preferred"
        }
    }')
);
```

### 4.2 AI Feature Engineering with VARIANT

```sql
-- Dynamic feature extraction for AI models
SELECT 
    user_id,
    
    -- Extract nested features using JSON path expressions
    user_features:demographics.age AS age,
    user_features:demographics.location AS location,
    user_features:history.total_purchases AS purchase_history,
    
    -- Array and object manipulation
    ARRAY_SIZE(user_features:preferences.categories) AS category_count,
    
    -- Complex feature calculations
    CASE 
        WHEN session_features:session_duration > 1800 THEN 'high_engagement'
        WHEN session_features:session_duration > 600 THEN 'medium_engagement'
        ELSE 'low_engagement'
    END AS engagement_level,
    
    -- AI model inference with dynamic features
    ML_PREDICT(
        'personalization_model',
        user_features:demographics.age,
        user_features:history.total_purchases,
        session_features:session_duration,
        behavioral_features:engagement_score
    ) AS personalization_result
    
FROM ai_feature_store;
```

### 4.3 Real-Time Feature Store with Lakehouse Integration

```sql
-- Apache Paimon integration for versioned feature storage
CREATE TABLE feature_lakehouse (
    feature_timestamp TIMESTAMP(3),
    entity_id STRING,
    feature_group STRING,
    features VARIANT,
    
    -- Paimon-specific options for versioning
    PRIMARY KEY (entity_id, feature_group) NOT ENFORCED
) WITH (
    'connector' = 'paimon',
    'path' = 's3://feature-store/paimon',
    'file.format' = 'orc',
    'compaction.max.file-num' = '50'
);

-- Real-time feature materialization
INSERT INTO feature_lakehouse
SELECT 
    CURRENT_TIMESTAMP,
    user_id,
    'user_behavioral_features',
    
    -- Dynamic feature aggregation  
    PARSE_JSON(OBJECT(
        'hourly_transaction_count', COUNT(*),
        'hourly_spending', SUM(amount),
        'unique_merchants', COUNT(DISTINCT merchant_id),
        'avg_transaction_amount', AVG(amount),
        'spending_velocity', 
            SUM(amount) / EXTRACT(EPOCH FROM (MAX(transaction_time) - MIN(transaction_time))),
        'risk_indicators',
            ARRAY[
                CASE WHEN COUNT(*) > 10 THEN 'high_frequency' END,
                CASE WHEN MAX(amount) > 1000 THEN 'high_value' END,
                CASE WHEN COUNT(DISTINCT location_country) > 1 THEN 'multi_location' END
            ]
    ))
    
FROM transaction_stream
WHERE transaction_time >= CURRENT_TIMESTAMP - INTERVAL '1' HOUR
GROUP BY user_id;
```

## 5. 🔗 Advanced Streaming Joins for AI - DeltaJoin & MultiJoin

Flink 2.1.0 introduces **DeltaJoin and MultiJoin strategies** that eliminate state bottlenecks and improve resource utilization for AI workloads.

### 5.1 DeltaJoin for Efficient AI Feature Enrichment

```sql
-- Efficient user profile enrichment for AI models
SELECT 
    t.transaction_id,
    t.user_id,
    t.amount,
    
    -- DeltaJoin optimization for large user profiles
    p.user_features,
    p.behavioral_score,
    p.risk_profile,
    
    -- Real-time AI inference with enriched data
    ML_PREDICT(
        'enriched_fraud_model',
        t.amount,
        t.merchant_category,
        p.behavioral_score,
        p.risk_profile:financial_stability,
        p.user_features:demographics.age
    ) AS fraud_prediction
    
FROM transaction_stream t
-- DeltaJoin automatically optimizes this large table join
INNER JOIN user_profile_table p
ON t.user_id = p.user_id;
```

### 5.2 MultiJoin for Complex AI Data Correlation

```sql
-- Multi-stream correlation for advanced AI models
SELECT 
    t.transaction_id,
    
    -- MultiJoin optimization for multiple stream correlation
    t.amount,
    u.user_risk_score,
    m.merchant_reputation,
    l.location_risk_level,
    d.device_trust_score,
    
    -- Complex AI model with multiple enrichment sources
    ML_PREDICT(
        'multi_source_fraud_model',
        t.amount,
        u.user_risk_score,
        m.merchant_reputation,
        l.location_risk_level,
        d.device_trust_score,
        
        -- Correlation features
        (u.user_risk_score * m.merchant_reputation) AS user_merchant_trust,
        (l.location_risk_level + d.device_trust_score) / 2 AS context_risk
    ) AS comprehensive_fraud_assessment
    
FROM transaction_stream t
-- MultiJoin optimizes these multiple joins
LEFT JOIN user_risk_stream u
    ON t.user_id = u.user_id
    AND u.timestamp BETWEEN t.timestamp - INTERVAL '5' MINUTE 
                       AND t.timestamp + INTERVAL '1' MINUTE
LEFT JOIN merchant_reputation_stream m
    ON t.merchant_id = m.merchant_id  
    AND m.timestamp BETWEEN t.timestamp - INTERVAL '10' MINUTE
                       AND t.timestamp + INTERVAL '1' MINUTE
LEFT JOIN location_risk_stream l
    ON t.location_id = l.location_id
    AND l.timestamp BETWEEN t.timestamp - INTERVAL '15' MINUTE
                       AND t.timestamp + INTERVAL '1' MINUTE  
LEFT JOIN device_trust_stream d
    ON t.device_id = d.device_id
    AND d.timestamp BETWEEN t.timestamp - INTERVAL '30' MINUTE
                       AND t.timestamp + INTERVAL '1' MINUTE;
```

## 6. 🎯 End-to-End Real-Time AI Workflow Implementation

### 6.1 Complete AI-Powered Fraud Detection Pipeline

```sql
-- Step 1: Create AI models for the pipeline
CREATE MODEL transaction_risk_model (
    MODEL_TYPE 'CLASSIFICATION',
    MODEL_FORMAT 'ONNX',
    INPUT_SCHEMA (
        amount DOUBLE,
        merchant_category STRING,
        time_of_day INT,
        day_of_week INT,
        user_age INT,
        location_risk DOUBLE,
        device_trust DOUBLE
    ),
    OUTPUT_SCHEMA (
        risk_probability DOUBLE,
        risk_category STRING,
        confidence_score DOUBLE
    ),
    MODEL_PATH 's3://ai-models/transaction-risk/v1.0.0/model.onnx'
);

CREATE MODEL behavioral_anomaly_model (
    MODEL_TYPE 'ANOMALY_DETECTION',
    MODEL_FORMAT 'TENSORFLOW',
    INPUT_SCHEMA (
        user_id STRING,
        transaction_sequence ARRAY<DOUBLE>,
        temporal_patterns VARIANT
    ),
    OUTPUT_SCHEMA (
        anomaly_score DOUBLE,
        anomaly_type STRING,
        explanation VARIANT
    ),
    MODEL_PATH 's3://ai-models/behavioral-anomaly/v2.0.0/model.pb'
);

-- Step 2: Comprehensive real-time AI pipeline
WITH enriched_transactions AS (
    -- Feature engineering stage
    SELECT 
        t.*,
        EXTRACT(HOUR FROM t.transaction_time) AS time_of_day,
        EXTRACT(DAY_OF_WEEK FROM t.transaction_time) AS day_of_week,
        
        -- User behavioral features
        COUNT(*) OVER (
            PARTITION BY t.user_id 
            ORDER BY t.transaction_time 
            RANGE INTERVAL '24' HOUR PRECEDING
        ) AS daily_transaction_count,
        
        AVG(t.amount) OVER (
            PARTITION BY t.user_id
            ORDER BY t.transaction_time 
            RANGE INTERVAL '7' DAY PRECEDING
        ) AS avg_weekly_amount,
        
        -- Location and device context
        l.risk_level AS location_risk,
        d.trust_score AS device_trust,
        u.age AS user_age
        
    FROM transaction_stream t
    LEFT JOIN location_risk_table l ON t.location_id = l.location_id
    LEFT JOIN device_trust_table d ON t.device_id = d.device_id  
    LEFT JOIN user_profile_table u ON t.user_id = u.user_id
),

ai_predictions AS (
    -- AI inference stage
    SELECT 
        *,
        
        -- Primary risk assessment
        ML_PREDICT(
            'transaction_risk_model',
            amount,
            merchant_category,
            time_of_day,
            day_of_week,
            user_age,
            location_risk,
            device_trust
        ) AS (risk_probability, risk_category, confidence_score),
        
        -- Behavioral anomaly detection  
        ML_PREDICT(
            'behavioral_anomaly_model',
            user_id,
            ARRAY[amount, daily_transaction_count, avg_weekly_amount],
            PARSE_JSON(OBJECT(
                'time_pattern', time_of_day,
                'frequency_pattern', daily_transaction_count,
                'amount_pattern', amount / NULLIF(avg_weekly_amount, 0)
            ))
        ) AS (anomaly_score, anomaly_type, anomaly_explanation)
        
    FROM enriched_transactions
),

final_assessment AS (
    -- Decision fusion stage
    SELECT 
        *,
        
        -- Combine AI outputs into final decision
        CASE 
            WHEN risk_probability > 0.8 AND anomaly_score > 0.7 THEN 'HIGH_RISK'
            WHEN risk_probability > 0.6 OR anomaly_score > 0.6 THEN 'MEDIUM_RISK' 
            WHEN risk_probability > 0.3 OR anomaly_score > 0.4 THEN 'LOW_RISK'
            ELSE 'NORMAL'
        END AS final_risk_level,
        
        -- Confidence calculation
        (risk_probability * confidence_score + anomaly_score) / 2 AS overall_confidence,
        
        -- Explanation aggregation
        PARSE_JSON(OBJECT(
            'risk_factors', risk_category,
            'anomaly_factors', anomaly_type,
            'detailed_explanation', anomaly_explanation,
            'confidence_breakdown', OBJECT(
                'transaction_risk', confidence_score,
                'behavioral_anomaly', anomaly_score
            )
        )) AS decision_explanation
        
    FROM ai_predictions
)

-- Step 3: Real-time actions and alerting
SELECT 
    transaction_id,
    user_id,
    amount,
    final_risk_level,
    overall_confidence,
    decision_explanation,
    
    -- Real-time action determination
    CASE final_risk_level
        WHEN 'HIGH_RISK' THEN 'BLOCK_TRANSACTION'
        WHEN 'MEDIUM_RISK' THEN 'REQUIRE_ADDITIONAL_AUTH'
        WHEN 'LOW_RISK' THEN 'FLAG_FOR_REVIEW'
        ELSE 'APPROVE'
    END AS recommended_action,
    
    CURRENT_TIMESTAMP AS processing_timestamp

FROM final_assessment
WHERE final_risk_level != 'NORMAL'  -- Only output transactions requiring attention
```

### 6.2 Real-Time Model Performance Monitoring

```sql
-- Continuous model performance tracking
CREATE TABLE model_performance_metrics (
    model_name STRING,
    metric_timestamp TIMESTAMP(3),
    accuracy DOUBLE,
    precision DOUBLE,
    recall DOUBLE,
    f1_score DOUBLE,
    latency_p50 BIGINT,
    latency_p99 BIGINT,
    throughput_per_second DOUBLE,
    error_rate DOUBLE
);

-- Real-time model monitoring pipeline
INSERT INTO model_performance_metrics
SELECT 
    'transaction_risk_model' AS model_name,
    TUMBLE_START(INTERVAL '1' MINUTE) AS metric_timestamp,
    
    -- Accuracy metrics (requires ground truth feedback)
    SUM(CASE WHEN actual_fraud = predicted_fraud THEN 1 ELSE 0 END) / 
        COUNT(*) AS accuracy,
        
    -- Precision: True Positives / (True Positives + False Positives)
    SUM(CASE WHEN predicted_fraud = 1 AND actual_fraud = 1 THEN 1 ELSE 0 END) /
        NULLIF(SUM(CASE WHEN predicted_fraud = 1 THEN 1 ELSE 0 END), 0) AS precision,
        
    -- Recall: True Positives / (True Positives + False Negatives)  
    SUM(CASE WHEN predicted_fraud = 1 AND actual_fraud = 1 THEN 1 ELSE 0 END) /
        NULLIF(SUM(CASE WHEN actual_fraud = 1 THEN 1 ELSE 0 END), 0) AS recall,
        
    -- F1 Score calculation
    2 * (precision * recall) / NULLIF(precision + recall, 0) AS f1_score,
    
    -- Latency metrics  
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY processing_latency_ms) AS latency_p50,
    PERCENTILE_CONT(0.99) WITHIN GROUP (ORDER BY processing_latency_ms) AS latency_p99,
    
    -- Throughput
    COUNT(*) / 60.0 AS throughput_per_second,
    
    -- Error rate
    SUM(CASE WHEN prediction_error IS NOT NULL THEN 1 ELSE 0 END) / 
        COUNT(*) AS error_rate

FROM fraud_prediction_results
WHERE prediction_timestamp >= CURRENT_TIMESTAMP - INTERVAL '24' HOUR
GROUP BY TUMBLE(prediction_timestamp, INTERVAL '1' MINUTE);
```

## 🛠️ Hands-On Exercises - Super Detailed Implementation

### Exercise 2.1: AI Model DDL Mastery (90 minutes)

**Objective**: Master complete AI model lifecycle management using Flink 2.1.0's AI Model DDL

**Setup**:
```bash
# Navigate to exercise directory
cd LearningCourse/Day02-AI-Stream-Processing/Exercise-Solutions/

# Create AI Model DDL project
dotnet new console -n AIModelDDLMastery
cd AIModelDDLMastery

# Add required packages
dotnet add package FlinkDotNet.SQL --version 2.1.0-preview
dotnet add package System.Text.Json --version 7.0.0
```

**Implementation Tasks**:

1. **Basic Model Registration** (30 minutes)
   - Create fraud detection model with comprehensive metadata
   - Implement model versioning and inheritance patterns
   - Add governance and compliance configurations

2. **Advanced Model Lifecycle** (30 minutes)  
   - Implement A/B testing with traffic splitting
   - Configure auto-rollback conditions
   - Set up monitoring and alerting rules

3. **Enterprise Model Governance** (30 minutes)
   - Multi-environment model deployment (staging → production)
   - Compliance and audit configuration
   - Resource allocation and security settings

**Expected Outcomes**:
- Complete AI model registry with 5+ models
- Working A/B testing infrastructure  
- Production-ready governance policies
- Automated model quality monitoring

### Exercise 2.2: ML_PREDICT TVF Implementation (120 minutes)

**Objective**: Build comprehensive real-time AI inference pipelines using ML_PREDICT TVF

**Implementation Tasks**:

1. **Basic ML_PREDICT Usage** (30 minutes)
   - Simple fraud detection with real-time inference
   - Feature engineering within SQL queries
   - Basic performance optimization

2. **Multi-Model Ensemble** (30 minutes)
   - Combine multiple AI models for better accuracy
   - Implement voting and averaging strategies
   - Handle model conflicts and confidence scoring

3. **Dynamic Model Selection** (30 minutes)
   - Conditional model routing based on data characteristics
   - Time-based model switching
   - Context-aware model selection

4. **Real-Time Feature Engineering** (30 minutes)
   - Complex windowing for behavioral features
   - Cross-stream feature correlation
   - Performance-optimized feature computation

**Expected Outcomes**:
- Working real-time AI inference system
- Multi-model ensemble with 95%+ accuracy
- Sub-50ms inference latency
- Comprehensive feature engineering pipeline

### Exercise 2.3: Process Table Functions (PTFs) Deep Dive (150 minutes)

**Objective**: Build advanced event-driven AI applications using PTFs with managed state access

**Implementation Tasks**:

1. **Stateful AI Processing** (45 minutes)
   - User behavioral analysis with state management
   - Session-based AI pattern detection
   - Temporal AI model adaptation

2. **Real-Time Model Training** (45 minutes)
   - Online learning implementation
   - Streaming model updates
   - Performance degradation detection

3. **Complex Event Processing AI** (60 minutes)
   - Multi-stage fraud pattern detection
   - Anomaly correlation across time windows
   - Predictive alerting system

**Expected Outcomes**:
- Stateful AI system processing 1000+ events/second
- Online learning with real-time model updates
- Complex CEP system with 99%+ pattern detection accuracy
- Production-ready event-driven AI architecture

### Exercise 2.4: VARIANT Data Types & Lakehouse Integration (90 minutes)

**Objective**: Master dynamic schema AI processing with VARIANT types and Apache Paimon integration

**Implementation Tasks**:

1. **VARIANT Data Fundamentals** (30 minutes)
   - Flexible AI feature storage design
   - JSON path-based feature extraction
   - Dynamic schema evolution

2. **AI Feature Engineering** (30 minutes)
   - Complex nested feature processing
   - Array and object manipulation for AI
   - Performance-optimized VARIANT queries

3. **Lakehouse Integration** (30 minutes)
   - Apache Paimon feature store setup
   - Versioned feature materialization
   - Real-time feature serving

**Expected Outcomes**:
- Flexible AI feature store handling any schema
- Optimized feature engineering for ML models
- Integrated lakehouse for feature versioning
- Sub-10ms feature serving latency

## 🎯 Learning Validation & Assessment

### Knowledge Check Questions

1. **AI Model DDL Mastery**
   - How do you implement model versioning with inheritance?
   - What are the key governance features for production AI models?
   - How do A/B testing and auto-rollback work together?

2. **ML_PREDICT TVF Expertise**
   - How do you optimize inference latency in streaming queries?
   - What are the best practices for multi-model ensembles?
   - How do you handle model conflicts and confidence scoring?

3. **PTFs Advanced Usage**
   - How do you access managed state in AI processing functions?
   - What are the patterns for real-time model training?
   - How do you implement complex temporal AI patterns?

4. **VARIANT Types & Lakehouse**
   - How do you design schemas for dynamic AI features?
   - What are the performance considerations for VARIANT queries?
   - How do you integrate with Apache Paimon for versioning?

### Practical Assessment

**Build a Complete Real-Time AI Platform** (3 hours)

Create an end-to-end AI-powered fraud detection system that demonstrates:

- AI Model DDL with 3+ models and governance
- ML_PREDICT TVF with ensemble inference  
- PTFs for stateful behavioral analysis
- VARIANT types for flexible feature storage
- Advanced streaming joins for data enrichment
- Real-time model performance monitoring

**Success Criteria**:
- Process 10,000+ transactions per second
- Achieve 95%+ fraud detection accuracy
- Maintain sub-50ms end-to-end latency
- Demonstrate complete observability
- Show production-ready governance

## 🔗 Day 2 Summary & Next Steps

### What You've Mastered Today

- **AI Model DDL**: Complete lifecycle management for production AI models
- **ML_PREDICT TVF**: Real-time inference directly in streaming SQL
- **Process Table Functions**: Event-driven AI with managed state access
- **VARIANT Data Types**: Dynamic schema processing for AI features
- **Advanced Streaming Joins**: Optimized data correlation for AI
- **End-to-End AI Workflows**: Production-ready real-time AI pipelines

### Tomorrow's Preview: Day 3 - Production Backpressure

Build on today's AI foundation by learning how to handle massive AI inference loads with:
- AI-aware backpressure strategies
- Model serving load balancing  
- Adaptive inference batching
- AI performance auto-scaling

---

## 🗺️ Course Navigation
📚 **[← Day 1: Flink 2.1.0 Fundamentals](../Day01-Flink21-Fundamentals/)** | **[Course Overview](../README.md)** | **[Next: Day 3 - Production Backpressure →](../Day03-Production-Backpressure/)**

**Day 2 of 14 Complete** ✅ | **Time Invested**: 7-8 hours | **Next**: Production AI Scaling Patterns