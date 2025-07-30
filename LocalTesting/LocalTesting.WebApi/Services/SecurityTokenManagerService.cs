using LocalTesting.WebApi.Models;

namespace LocalTesting.WebApi.Services;

public class SecurityTokenManagerService
{
    private string _currentToken = string.Empty;
    private int _renewalInterval = 10000;
    private int _renewalCount = 0;
    private int _messagesSinceRenewal = 0;
    private DateTime _lastRenewal = DateTime.UtcNow;
    private readonly SemaphoreSlim _renewalLock = new(1, 1);
    private bool _isRenewing = false;
    private readonly ILogger<SecurityTokenManagerService> _logger;

    public SecurityTokenManagerService(ILogger<SecurityTokenManagerService> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(int renewalInterval)
    {
        _renewalInterval = renewalInterval;
        _currentToken = $"initial-token-{DateTime.UtcNow:yyyyMMddHHmmss}";
        _renewalCount = 0;
        _messagesSinceRenewal = 0;
        _lastRenewal = DateTime.UtcNow;
        _isRenewing = false;

        _logger.LogInformation("SecurityTokenManager initialized with renewal interval: {RenewalInterval}", renewalInterval);
        return Task.CompletedTask;
    }

    public async Task<string> GetTokenAsync()
    {
        // Simulate token renewal logic
        Interlocked.Increment(ref _messagesSinceRenewal);
        
        if (_messagesSinceRenewal >= _renewalInterval)
        {
            await _renewalLock.WaitAsync();
            try
            {
                if (_messagesSinceRenewal >= _renewalInterval)
                {
                    _isRenewing = true;
                    _logger.LogInformation("Renewing security token (renewal #{RenewalCount})", _renewalCount + 1);
                    
                    // Simulate renewal time
                    await Task.Delay(100);
                    
                    _currentToken = $"renewed-token-{DateTime.UtcNow:yyyyMMddHHmmss}";
                    Interlocked.Increment(ref _renewalCount);
                    Interlocked.Exchange(ref _messagesSinceRenewal, 0);
                    _lastRenewal = DateTime.UtcNow;
                    _isRenewing = false;
                    
                    _logger.LogInformation("Security token renewed successfully. New token: {Token}", _currentToken[..20] + "...");
                }
            }
            finally
            {
                _renewalLock.Release();
            }
        }
        
        return _currentToken;
    }

    public SecurityTokenInfo GetTokenInfo()
    {
        return new SecurityTokenInfo
        {
            CurrentToken = _currentToken.Length > 20 ? _currentToken[..20] + "..." : _currentToken,
            RenewalCount = _renewalCount,
            MessagesSinceRenewal = _messagesSinceRenewal,
            RenewalInterval = _renewalInterval,
            LastRenewal = _lastRenewal,
            IsRenewing = _isRenewing
        };
    }

    public int GetRenewalCount() => _renewalCount;

    public void Reset()
    {
        _renewalCount = 0;
        _messagesSinceRenewal = 0;
        _lastRenewal = DateTime.UtcNow;
        _currentToken = $"reset-token-{DateTime.UtcNow:yyyyMMddHHmmss}";
        _isRenewing = false;
        
        _logger.LogInformation("SecurityTokenManager reset");
    }
}