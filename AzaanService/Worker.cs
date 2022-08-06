namespace AzaanService
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly ICaster casterSet;
        private readonly IConfiguration configuration;
        private readonly JsonSerializerOptions serializeOptions = new JsonSerializerOptions();
        private readonly List<string> targetDevices = new List<string>();
        private readonly BroadcastTimeService timeService;
        private string[]? files;

        public Worker(ILogger<Worker> logger, ICaster casterSet, IConfiguration configuration)
        {
            this.serializeOptions.Converters.Add(new AzaanTimeConverter());
            this.logger = logger;
            this.casterSet = casterSet;
            this.configuration = configuration;
            this.timeService = new BroadcastTimeService(this.logger, $"{this.configuration["azaan:apitarget"]}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.files = Directory.GetFiles(this.configuration["azaan:source"], "*.opus");
            for (int i = 0; i < this.files.Length; i++)
            {
                this.files[i] = $"http://192.168.1.10/{Path.GetFileName(this.files[i])}";
            }

            Random r = new Random();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            this.logger.LogInformation("Starting the caster: {time}", DateTimeOffset.Now);
            this.casterSet.Subscribe();

            while (!stoppingToken.IsCancellationRequested)
            {
                try { 
                    this.logger.LogInformation("New daily cycle.");
                    AzaanTimes times = await this.timeService.GetBroadcastTimes();
                    this.logger.LogInformation(times.ToString());
                    Queue<DateTime> q = times.AsQueue();
                    while (q.Any() && q.Peek() < DateTime.Now - TimeSpan.FromMinutes(20))
                    {
                        DateTime item = q.Dequeue();
                        this.logger.LogInformation($"service spin up at {DateTime.Now}, discarding {item}");
                    }

                    if (q.Any()) this.logger.LogInformation($"Entering actionable loop with {q.Count}, next broadcast at {q.Peek()}");
                    while (q.Any())
                    {
                        if (q.Peek() < DateTime.Now)
                        {
                            int chosen = r.Next(files.Length - 1);
                            DateTime actionable = q.Dequeue();
                            this.logger.LogInformation($"Broadcasting {actionable}: {this.files[chosen]}");
                             await this.Broadcast(this.files[chosen]);
                            if(q.Any())
                                this.logger.LogInformation($"Next broadcast: {q.Peek()}");
                        }

                        await Task.Delay(int.Parse(this.configuration["azaan:delay"]), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex.ToString());
                }
                
                TimeSpan sleepTime = DateTime.Today.AddDays(1) - DateTime.Now;
                this.logger.LogInformation($"Finished daily routine. Sleeping till midnight {sleepTime}. Goodbye.");
                await Task.Delay(sleepTime, stoppingToken);
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.logger.LogError($"{sender}: {e}");
        }

        private async Task Broadcast(string path)
        {
            await this.casterSet.Broadcast(path);
        }
    }
}