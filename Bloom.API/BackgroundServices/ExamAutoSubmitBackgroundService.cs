using Bloom.BLL.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bloom.API.BackgroundServices;

public class ExamAutoSubmitBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExamAutoSubmitBackgroundService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public ExamAutoSubmitBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ExamAutoSubmitBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExamAutoSubmitBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var examService = scope.ServiceProvider.GetRequiredService<IExamService>();

                var submittedCount = await examService.AutoSubmitExpiredAttemptsAsync(stoppingToken);
                if (submittedCount > 0)
                {
                    _logger.LogInformation("ExamAutoSubmitBackgroundService auto-submitted {Count} expired attempts.", submittedCount);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error while checking for expired exam attempts.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ExamAutoSubmitBackgroundService stopping.");
    }
}

