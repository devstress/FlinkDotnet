# BackPressure Kafka Testing

```bash
# Aspire infrastructure will auto-start Kafka
dotnet test BackPressure.Tests --configuration Release --logger "trx;LogFileName=TestResults.trx"
```