namespace HelpDeskPro.Infrastructure.Services;

using HelpDeskPro.Application.Features.Sla.Commands.CheckSlaBreaches;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class SlaMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaMonitorService> _logger;
    private readonly int _intervalMinutes;

    public SlaMonitorService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SlaMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _intervalMinutes = configuration.GetValue<int>("SlaMonitor:IntervalMinutes", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SlaMonitorService started with interval of {Interval} minutes.", _intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new CheckSlaBreachesCommand(), stoppingToken);

                _logger.LogInformation(
                    "SLA Monitor executed: {ResponseBreaches} response breach(es), {ResolutionBreaches} resolution breach(es), {AutoClosed} auto-closed.",
                    result.ResponseBreachesCount,
                    result.ResolutionBreachesCount,
                    result.AutoClosedCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during SLA monitoring execution.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SlaMonitorService is stopping.");
    }
}

