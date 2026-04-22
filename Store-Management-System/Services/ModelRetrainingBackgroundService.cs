using Microsoft.EntityFrameworkCore;

namespace Store_Management_System.Services
{
    public class ModelRetrainingBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ModelRetrainingBackgroundService> _logger;

        public ModelRetrainingBackgroundService(IServiceProvider services, ILogger<ModelRetrainingBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Run every Sunday at 2 AM
                    var now = DateTime.UtcNow;
                    var nextSunday = now.AddDays(7 - (int)now.DayOfWeek);
                    var nextRun = new DateTime(nextSunday.Year, nextSunday.Month, nextSunday.Day, 2, 0, 0);

                    if (nextRun <= now)
                    {
                        nextRun = nextRun.AddDays(7);
                    }

                    var delay = nextRun - now;
                    _logger.LogInformation($"Next model retraining scheduled at {nextRun.ToLocalTime()}");

                    await Task.Delay(delay, stoppingToken);

                    using var scope = _services.CreateScope();
                    var dataService = scope.ServiceProvider.GetRequiredService<MLDataService>();
                    var trainerService = scope.ServiceProvider.GetRequiredService<ModelTrainerService>();
                    var forecastService = scope.ServiceProvider.GetRequiredService<ForecastService>();

                    _logger.LogInformation("Starting weekly model retraining...");

                    var trainingData = await dataService.GetTrainingData();
                    if (trainingData.Any())
                    {
                        var success = await trainerService.TrainDemandForecastModel(trainingData);
                        if (success)
                        {
                            forecastService.ReloadModel();
                            _logger.LogInformation("Weekly model retraining completed successfully");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background model retraining");
                }
            }
        }
    }
}