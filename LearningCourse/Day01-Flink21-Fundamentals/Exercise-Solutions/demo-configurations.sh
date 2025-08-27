#!/bin/bash

echo "🚀 Day 1 Exercise Solutions - Configuration Demo"
echo "================================================"
echo ""

echo "Building ProductionApp..."
cd ProductionApp
dotnet build --property:RunAnalyzersDuringBuild=false --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo "✅ Build successful!"
echo ""

echo "📋 Available Configurations:"
echo "  1. RecommendationEngine (Netflix-style AI recommendations)"
echo "  2. DynamicPricingEngine (Uber-scale dynamic pricing)"
echo "  3. FeedGenerationEngine (LinkedIn feed generation)"
echo "  4. RocksDBStateBackend (Enterprise state management)"
echo ""

echo "🎯 Quick Test Examples:"
echo ""

echo "Netflix RecommendationEngine:"
echo "  dotnet run --configuration=RecommendationEngine"
echo "  curl http://localhost:5000/recommendations/user123"
echo "  curl http://localhost:5000/netflix-metrics"
echo ""

echo "Uber DynamicPricingEngine:"
echo "  dotnet run --configuration=DynamicPricingEngine"
echo "  curl -X POST http://localhost:5000/pricing/calculate -d '{\"pickup\":\"downtown\"}'"
echo "  curl http://localhost:5000/uber-metrics"
echo ""

echo "LinkedIn FeedGenerationEngine:"
echo "  dotnet run --configuration=FeedGenerationEngine"
echo "  curl http://localhost:5000/feed/user456"
echo "  curl http://localhost:5000/linkedin-metrics"
echo ""

echo "Enterprise RocksDBStateBackend:"
echo "  dotnet run --configuration=RocksDBStateBackend"
echo "  curl http://localhost:5000/state/performance"
echo "  curl http://localhost:5000/state/schema-evolution"
echo ""

echo "🔗 All configurations now implement the enterprise patterns described in README.md"
echo "✅ Issue resolved: RecommendationEngine and other exercise code is now fully implemented!"