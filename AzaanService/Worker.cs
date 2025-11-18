namespace AzaanService
{
    using Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly ICaster casterSet;
        private readonly IConfiguration configuration;
        private readonly IFileManager fileManager;
        private readonly JsonSerializerOptions serializeOptions = new();
        private readonly List<string> targetDevices = new();
        private readonly BroadcastTimeService timeService;

        public Worker(ILogger<Worker> logger, ICaster casterSet, IConfiguration configuration, IFileManager fileManager)
        {
            this.serializeOptions.Converters.Add(new AzaanTimeConverter());
            this.logger = logger;
            this.casterSet = casterSet;
            this.configuration = configuration;
            this.timeService = new BroadcastTimeService(this.logger, $"{this.configuration["azaan:apitarget"]}");
            this.fileManager = fileManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            this.logger.LogInformation("Starting the caster: {time}", DateTimeOffset.Now);
            this.casterSet.Subscribe();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.logger.LogInformation("New daily cycle.");
                    AzaanTimes times = await this.timeService.GetBroadcastTimes();
                    this.logger.LogInformation(times.ToString());
                    Queue<DateTime> q = times.AsQueue();
                    while (q.Any() && q.Peek() < DateTime.Now - TimeSpan.FromMinutes(20))
                    {
                        DateTime item = q.Dequeue();
                        this.logger.LogInformation("service spin up at {DateTime}, discarding {Item}", DateTime.Now, item);
                    }

                    if (q.Any()) this.logger.LogInformation("Entering actionable loop with {QCount}, next broadcast at {DateTime}", q.Count, q.Peek());
                    while (q.Any())
                    {
                        if (q.Peek() < DateTime.Now)
                        {
                            DateTime actionable = q.Dequeue();
                            var file = fileManager.Pick();
                            this.logger.LogInformation("Broadcasting {DateTime}: {File}", actionable, file);
                            await this.Broadcast(file);
                            if (q.Any())
                                this.logger.LogInformation("Next broadcast: {DateTime}", q.Peek());
                        }

                        await Task.Delay(int.Parse(this.configuration["azaan:delay"] ?? "10"), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex.ToString());
                }

                TimeSpan sleepTime = DateTime.Today.AddDays(1) - DateTime.Now;
                this.logger.LogInformation("Finished daily routine. Sleeping till midnight {SleepTime}. Goodbye.", sleepTime);
                await Task.Delay(sleepTime, stoppingToken);
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }

        private async Task Broadcast(string path)
        {
            await this.casterSet.Broadcast(path);
        }
    }
}