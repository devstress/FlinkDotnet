#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Kafka Container Configuration Validation Script for WI27
.DESCRIPTION
    Validates the custom Kafka container configuration changes made in WI27
    without requiring a full infrastructure deployment.
    
    This script validates:
    1. Custom Kafka container configuration in AppHost
    2. Manual producer configuration in WebAPI
    3. Service discovery integration
    4. Configuration compatibility with Aspire
.PARAMETER Verbose
    Enable verbose output for detailed analysis
#>

param(
    [switch]$Verbose = $false
)

# Set error action preference for proper error handling
$ErrorActionPreference = "Stop"

Write-Host "🔍 WI27 Configuration Validation: Custom Kafka Container Implementation" -ForegroundColor Green
Write-Host ""

$validationResults = @()

try {
    # Step 1: Validate AppHost Kafka container configuration
    Write-Host "📋 Step 1: Validating AppHost Custom Kafka Container Configuration" -ForegroundColor Yellow
    
    $appHostPath = "LocalTesting/LocalTesting.AppHost/Program.cs"
    if (Test-Path $appHostPath) {
        $appHostContent = Get-Content $appHostPath -Raw
        
        # Check for custom Kafka container configuration
        $hasCustomKafkaContainer = $appHostContent -match 'builder\.AddContainer\("kafka", "apache/kafka:3\.8\.0"\)'
        $hasTargetPort = $appHostContent -match 'targetPort: 9092'
        $hasKafkaListeners = $appHostContent -match 'KAFKA_LISTENERS.*0\.0\.0\.0:9092'
        $hasAdvertisedListeners = $appHostContent -match 'KAFKA_ADVERTISED_LISTENERS.*localhost:9092'
        $hasClusterId = $appHostContent -match 'CLUSTER_ID.*LocalTestingCluster2024'
        $hasAutoCreateTopics = $appHostContent -match 'KAFKA_AUTO_CREATE_TOPICS_ENABLE.*true'
        
        Write-Host "   ✅ Custom Kafka Container: $(if ($hasCustomKafkaContainer) { 'FOUND' } else { 'MISSING' })" -ForegroundColor $(if ($hasCustomKafkaContainer) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Target Port Configuration: $(if ($hasTargetPort) { 'CONFIGURED' } else { 'MISSING' })" -ForegroundColor $(if ($hasTargetPort) { 'Green' } else { 'Red' })
        Write-Host "   ✅ External Listeners: $(if ($hasKafkaListeners) { 'CONFIGURED' } else { 'MISSING' })" -ForegroundColor $(if ($hasKafkaListeners) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Advertised Listeners: $(if ($hasAdvertisedListeners) { 'CONFIGURED' } else { 'MISSING' })" -ForegroundColor $(if ($hasAdvertisedListeners) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Cluster Configuration: $(if ($hasClusterId) { 'SET' } else { 'MISSING' })" -ForegroundColor $(if ($hasClusterId) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Auto-create Topics: $(if ($hasAutoCreateTopics) { 'ENABLED' } else { 'DISABLED' })" -ForegroundColor $(if ($hasAutoCreateTopics) { 'Green' } else { 'Red' })
        
        # Check for old AddKafka() usage (should be removed)
        $hasOldAddKafka = $appHostContent -match '\.AddKafka\('
        Write-Host "   ✅ Old AddKafka() Removed: $(if (-not $hasOldAddKafka) { 'YES' } else { 'NO - NEEDS CLEANUP' })" -ForegroundColor $(if (-not $hasOldAddKafka) { 'Green' } else { 'Red' })
        
        $validationResults += @{
            Component = "AppHost Kafka Configuration"
            Status = if ($hasCustomKafkaContainer -and $hasTargetPort -and $hasKafkaListeners -and $hasAdvertisedListeners -and -not $hasOldAddKafka) { "PASS" } else { "FAIL" }
            Details = "Custom container: $hasCustomKafkaContainer, TargetPort: $hasTargetPort, External access: $hasKafkaListeners, Old config removed: $(-not $hasOldAddKafka)"
        }
        
        if ($Verbose) {
            Write-Host "   🔍 Configuration Details:" -ForegroundColor Gray
            if ($hasCustomKafkaContainer) {
                $kafkaConfig = ($appHostContent | Select-String -Pattern 'var kafka = builder\.AddContainer.*?\.WithArgs.*?;' -AllMatches).Matches[0].Value
                Write-Host "      $kafkaConfig" -ForegroundColor White
            }
        }
    }
    else {
        Write-Host "   ❌ AppHost file not found: $appHostPath" -ForegroundColor Red
        $validationResults += @{
            Component = "AppHost File"
            Status = "FAIL"
            Details = "File not found"
        }
    }
    
    # Step 2: Validate WebAPI manual producer configuration
    Write-Host ""
    Write-Host "📋 Step 2: Validating WebAPI Manual Producer Configuration" -ForegroundColor Yellow
    
    $webApiPath = "LocalTesting/LocalTesting.WebApi/Program.cs"
    if (Test-Path $webApiPath) {
        $webApiContent = Get-Content $webApiPath -Raw
        
        # Check for manual producer configuration
        $hasManualProducer = $webApiContent -match 'AddSingleton<IProducer<string, string>>'
        $hasProducerConfig = $webApiContent -match 'ProducerConfig'
        $hasBootstrapServersConfig = $webApiContent -match 'BootstrapServers.*bootstrapServers'
        $hasKafkaConnectionString = $webApiContent -match 'GetConnectionString\("kafka"\)'
        
        Write-Host "   ✅ Manual Producer Registration: $(if ($hasManualProducer) { 'CONFIGURED' } else { 'MISSING' })" -ForegroundColor $(if ($hasManualProducer) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Producer Configuration: $(if ($hasProducerConfig) { 'FOUND' } else { 'MISSING' })" -ForegroundColor $(if ($hasProducerConfig) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Bootstrap Servers Setup: $(if ($hasBootstrapServersConfig) { 'CONFIGURED' } else { 'MISSING' })" -ForegroundColor $(if ($hasBootstrapServersConfig) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Kafka Connection String: $(if ($hasKafkaConnectionString) { 'INTEGRATED' } else { 'MISSING' })" -ForegroundColor $(if ($hasKafkaConnectionString) { 'Green' } else { 'Red' })
        
        # Check for old Aspire producer integration (should be removed)
        $hasOldAddKafkaProducer = $webApiContent -match '\.AddKafkaProducer'
        Write-Host "   ✅ Old AddKafkaProducer Removed: $(if (-not $hasOldAddKafkaProducer) { 'YES' } else { 'NO - NEEDS CLEANUP' })" -ForegroundColor $(if (-not $hasOldAddKafkaProducer) { 'Green' } else { 'Red' })
        
        $validationResults += @{
            Component = "WebAPI Producer Configuration"
            Status = if ($hasManualProducer -and $hasProducerConfig -and $hasBootstrapServersConfig -and $hasKafkaConnectionString -and -not $hasOldAddKafkaProducer) { "PASS" } else { "FAIL" }
            Details = "Manual producer: $hasManualProducer, Config: $hasProducerConfig, Connection string: $hasKafkaConnectionString, Old config removed: $(-not $hasOldAddKafkaProducer)"
        }
        
        if ($Verbose) {
            Write-Host "   🔍 Producer Configuration Details:" -ForegroundColor Gray
            if ($hasManualProducer) {
                $producerConfig = ($webApiContent | Select-String -Pattern 'builder\.Services\.AddSingleton<IProducer<string, string>>.*?\}\);' -AllMatches).Matches[0].Value
                Write-Host "      Manual producer configuration found" -ForegroundColor White
            }
        }
    }
    else {
        Write-Host "   ❌ WebAPI file not found: $webApiPath" -ForegroundColor Red
        $validationResults += @{
            Component = "WebAPI File"
            Status = "FAIL"
            Details = "File not found"
        }
    }
    
    # Step 3: Validate service discovery integration
    Write-Host ""
    Write-Host "📋 Step 3: Validating Aspire Service Discovery Integration" -ForegroundColor Yellow
    
    if (Test-Path $appHostPath) {
        $appHostContent = Get-Content $appHostPath -Raw
        
        # Check for service discovery endpoint reference
        $hasEndpointReference = $appHostContent -match '\.WithReference\(kafka\.GetEndpoint\("kafka"\)\)'
        $hasCorrectEndpointName = $appHostContent -match 'name: "kafka"'
        
        Write-Host "   ✅ Endpoint Reference: $(if ($hasEndpointReference) { 'CONFIGURED' } else { 'MISSING' })" -ForegroundColor $(if ($hasEndpointReference) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Named Endpoint: $(if ($hasCorrectEndpointName) { 'SET' } else { 'MISSING' })" -ForegroundColor $(if ($hasCorrectEndpointName) { 'Green' } else { 'Red' })
        
        $validationResults += @{
            Component = "Service Discovery Integration"
            Status = if ($hasEndpointReference -and $hasCorrectEndpointName) { "PASS" } else { "FAIL" }
            Details = "Endpoint reference: $hasEndpointReference, Named endpoint: $hasCorrectEndpointName"
        }
    }
    
    # Step 4: Validate API endpoints for testing
    Write-Host ""
    Write-Host "📋 Step 4: Validating Test API Endpoints" -ForegroundColor Yellow
    
    $controllerPath = "LocalTesting/LocalTesting.WebApi/Controllers/ObservabilityController.cs"
    if (Test-Path $controllerPath) {
        $controllerContent = Get-Content $controllerPath -Raw
        
        # Check for test endpoints
        $hasKafkaHealthEndpoint = $controllerContent -match '\[HttpGet\("kafka-health"\)\]'
        $hasKafkaProducerTestEndpoint = $controllerContent -match '\[HttpPost\("test-kafka-producer"\)\]'
        $hasTestMessageMethod = $controllerContent -match 'ProduceTestMessageAsync'
        
        Write-Host "   ✅ Kafka Health Endpoint: $(if ($hasKafkaHealthEndpoint) { 'AVAILABLE' } else { 'MISSING' })" -ForegroundColor $(if ($hasKafkaHealthEndpoint) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Producer Test Endpoint: $(if ($hasKafkaProducerTestEndpoint) { 'AVAILABLE' } else { 'MISSING' })" -ForegroundColor $(if ($hasKafkaProducerTestEndpoint) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Test Message Method: $(if ($hasTestMessageMethod) { 'IMPLEMENTED' } else { 'MISSING' })" -ForegroundColor $(if ($hasTestMessageMethod) { 'Green' } else { 'Red' })
        
        $validationResults += @{
            Component = "Test API Endpoints"
            Status = if ($hasKafkaHealthEndpoint -and $hasKafkaProducerTestEndpoint -and $hasTestMessageMethod) { "PASS" } else { "FAIL" }
            Details = "Health endpoint: $hasKafkaHealthEndpoint, Test endpoint: $hasKafkaProducerTestEndpoint, Test method: $hasTestMessageMethod"
        }
    }
    else {
        Write-Host "   ❌ ObservabilityController file not found: $controllerPath" -ForegroundColor Red
        $validationResults += @{
            Component = "Test API Endpoints"
            Status = "FAIL"
            Details = "Controller file not found"
        }
    }
    
    # Step 5: Validate KafkaProducerService updates
    Write-Host ""
    Write-Host "📋 Step 5: Validating KafkaProducerService Updates" -ForegroundColor Yellow
    
    $servicePath = "LocalTesting/LocalTesting.WebApi/Services/KafkaProducerService.cs"
    if (Test-Path $servicePath) {
        $serviceContent = Get-Content $servicePath -Raw
        
        # Check for new test methods and health check
        $hasProduceTestMethod = $serviceContent -match 'ProduceTestMessageAsync'
        $hasHealthCheckMethod = $serviceContent -match 'ValidateKafkaConnectivityAsync'
        $hasHealthCheckResult = $serviceContent -match 'KafkaHealthCheckResult'
        $hasInjectedProducer = $serviceContent -match 'IProducer<string, string> producer.*Injected Aspire-managed producer'
        
        Write-Host "   ✅ Test Message Production: $(if ($hasProduceTestMethod) { 'IMPLEMENTED' } else { 'MISSING' })" -ForegroundColor $(if ($hasProduceTestMethod) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Health Check Method: $(if ($hasHealthCheckMethod) { 'IMPLEMENTED' } else { 'MISSING' })" -ForegroundColor $(if ($hasHealthCheckMethod) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Health Check Result Type: $(if ($hasHealthCheckResult) { 'DEFINED' } else { 'MISSING' })" -ForegroundColor $(if ($hasHealthCheckResult) { 'Green' } else { 'Red' })
        Write-Host "   ✅ Injected Producer Usage: $(if ($hasInjectedProducer) { 'CONFIGURED' } else { 'MISSING' })" -ForegroundColor $(if ($hasInjectedProducer) { 'Green' } else { 'Red' })
        
        $validationResults += @{
            Component = "KafkaProducerService"
            Status = if ($hasProduceTestMethod -and $hasHealthCheckMethod -and $hasHealthCheckResult) { "PASS" } else { "FAIL" }
            Details = "Test method: $hasProduceTestMethod, Health check: $hasHealthCheckMethod, Result type: $hasHealthCheckResult"
        }
    }
    else {
        Write-Host "   ❌ KafkaProducerService file not found: $servicePath" -ForegroundColor Red
        $validationResults += @{
            Component = "KafkaProducerService"
            Status = "FAIL"
            Details = "Service file not found"
        }
    }
    
    # Summary
    Write-Host ""
    Write-Host "📊 WI27 Configuration Validation Summary" -ForegroundColor Cyan
    Write-Host "=" * 50 -ForegroundColor Cyan
    
    $passCount = ($validationResults | Where-Object { $_.Status -eq "PASS" }).Count
    $failCount = ($validationResults | Where-Object { $_.Status -eq "FAIL" }).Count
    $totalCount = $validationResults.Count
    
    foreach ($result in $validationResults) {
        $statusColor = if ($result.Status -eq "PASS") { "Green" } else { "Red" }
        $statusSymbol = if ($result.Status -eq "PASS") { "✅" } else { "❌" }
        Write-Host "   $statusSymbol $($result.Component): $($result.Status)" -ForegroundColor $statusColor
        if ($Verbose -and $result.Status -eq "FAIL") {
            Write-Host "      Details: $($result.Details)" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "📈 Overall Results: $passCount/$totalCount components validated successfully" -ForegroundColor $(if ($passCount -eq $totalCount) { "Green" } else { "Yellow" })
    
    if ($passCount -eq $totalCount) {
        Write-Host ""
        Write-Host "🎉 WI27 VALIDATION SUCCESSFUL" -ForegroundColor Green
        Write-Host "✅ Custom Kafka container configuration is properly implemented" -ForegroundColor Green
        Write-Host "✅ External access issues should be resolved" -ForegroundColor Green
        Write-Host "✅ Manual producer configuration is correctly integrated" -ForegroundColor Green
        Write-Host "✅ Test endpoints are available for connectivity validation" -ForegroundColor Green
        Write-Host ""
        Write-Host "🚀 Next Steps:" -ForegroundColor Cyan
        Write-Host "   1. Deploy infrastructure to test custom container startup" -ForegroundColor White
        Write-Host "   2. Validate Kafka broker external access on dynamic ports" -ForegroundColor White
        Write-Host "   3. Run observability tests to confirm hanging issue is resolved" -ForegroundColor White
        Write-Host "   4. Verify test failure time reduced from 5+ minutes to <60 seconds" -ForegroundColor White
        
        exit 0
    }
    else {
        Write-Host ""
        Write-Host "⚠️ WI27 VALIDATION INCOMPLETE" -ForegroundColor Yellow
        Write-Host "🔧 $failCount/$totalCount components need attention" -ForegroundColor Yellow
        Write-Host "🔧 Review failed components above for required fixes" -ForegroundColor Yellow
        
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "❌ Configuration Validation FAILED" -ForegroundColor Red
    Write-Host "💥 Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔧 Check file paths and configuration syntax" -ForegroundColor Yellow
    
    exit 1
}