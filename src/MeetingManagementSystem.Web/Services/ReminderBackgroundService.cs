using MeetingManagementSystem.Core.Interfaces;

namespace MeetingManagementSystem.Web.Services;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public ReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder Background Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing reminders");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Reminder Background Service is stopping");
    }

    private async Task ProcessRemindersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var reminderScheduler = scope.ServiceProvider.GetRequiredService<IReminderSchedulerService>();

        _logger.LogDebug("Processing pending reminders");
        await reminderScheduler.ProcessPendingRemindersAsync();
    }
}
