using System.Timers;

namespace WitchCityRope.Web.Services;

public class ToastService : IToastService, IDisposable
{
    private readonly List<ToastMessage> _messages = new();
    private readonly System.Timers.Timer _timer;
    
    public event Action? OnChange;
    
    public IReadOnlyList<ToastMessage> Messages => _messages.AsReadOnly();
    
    public ToastService()
    {
        _timer = new System.Timers.Timer(1000); // Check every second
        _timer.Elapsed += RemoveExpiredMessages;
        _timer.AutoReset = true;
        _timer.Start();
    }
    
    public void ShowSuccess(string message)
    {
        Show(message, ToastLevel.Success);
    }
    
    public void ShowError(string message)
    {
        Show(message, ToastLevel.Error);
    }
    
    public void ShowWarning(string message)
    {
        Show(message, ToastLevel.Warning);
    }
    
    public void ShowInfo(string message)
    {
        Show(message, ToastLevel.Info);
    }
    
    private void Show(string message, ToastLevel level)
    {
        var toast = new ToastMessage
        {
            Id = Guid.NewGuid(),
            Message = message,
            Level = level,
            Created = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(5) // 5 second duration
        };
        
        _messages.Add(toast);
        OnChange?.Invoke();
    }
    
    private void RemoveExpiredMessages(object? sender, ElapsedEventArgs e)
    {
        var now = DateTime.UtcNow;
        var expired = _messages.Where(m => m.ExpiresAt <= now).ToList();
        
        if (expired.Any())
        {
            foreach (var message in expired)
            {
                _messages.Remove(message);
            }
            OnChange?.Invoke();
        }
    }
    
    public void Remove(Guid id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message != null)
        {
            _messages.Remove(message);
            OnChange?.Invoke();
        }
    }
    
    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }
}

public class ToastMessage
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public ToastLevel Level { get; set; }
    public DateTime Created { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public enum ToastLevel
{
    Success,
    Error,
    Warning,
    Info
}