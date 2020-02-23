namespace AzaanService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly ICaster caster;
        private readonly IConfiguration configuration;

        public Worker(ILogger<Worker> logger, ICaster caster, IConfiguration configuration)
        {
            this.logger = logger;
            this.caster = caster;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Starting the caster: {time}", DateTimeOffset.Now);
            caster.Subscribe();
            string targetDevice = configuration["azaan:target"];
            logger.LogInformation(@$"Listening for castable devices... ({targetDevice})");
            while (!stoppingToken.IsCancellationRequested)
            {
                if (caster.Knows(targetDevice))
                {
                    bool connected = await caster.Connect(targetDevice);
                    if (connected)
                    {
                        logger.LogInformation("Casting..", DateTimeOffset.Now);
                        await caster.Play(configuration["azaan:source"]);
                        caster.Disconnect(targetDevice);
                    }

                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                await Task.Delay(int.Parse(configuration["azaan:delay"]), stoppingToken);
            }
        }
    }
}