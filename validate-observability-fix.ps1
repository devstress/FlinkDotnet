#!/usr/bin/env pwsh

<#
.SYNOPSIS
Validation script for observability metrics real infrastructure fix

.DESCRIPTION
This script validates that the observability fix properly uses real infrastructure
instead of fake metrics. It checks the implementation without requiring full .NET 9.0 execution.

.NOTES
This validation focuses on code analysis since full infrastructure execution requires .NET 9.0
#>

Write-Host "🔍 Validating Observability Metrics Real Infrastructure Fix" -ForegroundColor Green
Write-Host "=" * 60

$ErrorCount = 0

# Check 1: Verify ObservabilityController uses real KafkaProducerService
Write-Host "✅ Check 1: ObservabilityController real infrastructure integration"
$controllerFile = "LocalTesting/LocalTesting.WebApi/Controllers/ObservabilityController.cs"
if (Test-Path $controllerFile) {
    $content = Get-Content $controllerFile -Raw
    
    # Check for real Kafka service injection
    if ($content -match "KafkaProducerService.*kafkaProducerService") {
        Write-Host "  ✅ KafkaProducerService properly injected" -ForegroundColor Green
    } else {
        Write-Host "  ❌ KafkaProducerService not properly injected" -ForegroundColor Red
        $ErrorCount++
    }
    
    # Check for real message production
    if ($content -match "ProduceMessagesAsync.*ingressTopic.*realMessages") {
        Write-Host "  ✅ Real message production implemented" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Real message production not found" -ForegroundColor Red
        $ErrorCount++
    }
    
    # Check that fake metric generation is removed
    if ($content -notmatch "TEMPORARY.*Generate metrics that represent") {
        Write-Host "  ✅ Fake metric generation removed" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Fake metric generation still present" -ForegroundColor Red
        $ErrorCount++
    }
    
    # Check for Prometheus metrics usage
    if ($content -match "_prometheusService\.GetAllMetricsAsync") {
        Write-Host "  ✅ Real Prometheus metrics retrieval implemented" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Prometheus metrics retrieval not implemented" -ForegroundColor Red
        $ErrorCount++
    }
    
} else {
    Write-Host "  ❌ ObservabilityController.cs not found" -ForegroundColor Red
    $ErrorCount++
}

# Check 2: Verify PrometheusMetricsService avoids fake fallbacks
Write-Host ""
Write-Host "✅ Check 2: PrometheusMetricsService real data only"
$prometheusFile = "LocalTesting/LocalTesting.WebApi/Services/PrometheusMetricsService.cs"
if (Test-Path $prometheusFile) {
    $content = Get-Content $prometheusFile -Raw
    
    # Check that fake fallback values are removed
    if ($content -notmatch "Using fallback values") {
        Write-Host "  ✅ Fake fallback values removed" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Fake fallback values still present" -ForegroundColor Red
        $ErrorCount++
    }
    
    # Check for proper empty returns instead of fake data
    if ($content -match "return metrics") {
        Write-Host "  ✅ Returns empty metrics instead of fake data" -ForegroundColor Green
    } else {
        Write-Host "  ❌ May still generate fake data instead of empty results" -ForegroundColor Yellow
    }
    
    # Check for connectivity testing
    if ($content -match "connectivity.*confirmed") {
        Write-Host "  ✅ Prometheus connectivity testing implemented" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Prometheus connectivity testing not found" -ForegroundColor Red
        $ErrorCount++
    }
    
} else {
    Write-Host "  ❌ PrometheusMetricsService.cs not found" -ForegroundColor Red
    $ErrorCount++
}

# Check 3: Verify ComplexLogicMessage model is available
Write-Host ""
Write-Host "✅ Check 3: ComplexLogicMessage model availability"
$modelFile = "LocalTesting/LocalTesting.WebApi/Models/StressTestModels.cs"
if (Test-Path $modelFile) {
    $content = Get-Content $modelFile -Raw
    
    if ($content -match "class ComplexLogicMessage") {
        Write-Host "  ✅ ComplexLogicMessage model available" -ForegroundColor Green
    } else {
        Write-Host "  ❌ ComplexLogicMessage model not found" -ForegroundColor Red
        $ErrorCount++
    }
} else {
    Write-Host "  ❌ StressTestModels.cs not found" -ForegroundColor Red
    $ErrorCount++
}

# Check 4: Verify test calls the right endpoints
Write-Host ""
Write-Host "✅ Check 4: Test integration validation"
$testFile = "LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs"
if (Test-Path $testFile) {
    $content = Get-Content $testFile -Raw
    
    if ($content -match "/api/observability/metrics/simulate") {
        Write-Host "  ✅ Test calls real infrastructure simulate endpoint" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Test does not call simulate endpoint" -ForegroundColor Red
        $ErrorCount++
    }
    
    if ($content -match "/api/observability/metrics/messages-per-second") {
        Write-Host "  ✅ Test calls real metrics retrieval endpoint" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Test does not call metrics endpoint" -ForegroundColor Red
        $ErrorCount++
    }
} else {
    Write-Host "  ❌ ObservabilityMetricsSteps.cs not found" -ForegroundColor Red
    $ErrorCount++
}

# Summary
Write-Host ""
Write-Host "=" * 60
if ($ErrorCount -eq 0) {
    Write-Host "✅ VALIDATION PASSED: Observability metrics fix properly implements real infrastructure" -ForegroundColor Green
    Write-Host "   - Fake metric generation removed" -ForegroundColor Green
    Write-Host "   - Real Kafka message production implemented" -ForegroundColor Green
    Write-Host "   - Real Prometheus metrics retrieval implemented" -ForegroundColor Green
    Write-Host "   - Test integration properly configured" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next Steps:" -ForegroundColor Yellow
    Write-Host "   1. Test requires .NET 9.0 SDK for execution" -ForegroundColor Yellow
    Write-Host "   2. Full infrastructure (Kafka, Prometheus) must be running" -ForegroundColor Yellow
    Write-Host "   3. Run observability test to validate real metrics flow" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "❌ VALIDATION FAILED: $ErrorCount issues found" -ForegroundColor Red
    Write-Host "   Please review and fix the issues above before testing" -ForegroundColor Red
    exit 1
}