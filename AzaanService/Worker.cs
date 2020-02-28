namespace AzaanService
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
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
        private readonly ICaster caster;
        private readonly IConfiguration configuration;
        private readonly JsonSerializerOptions serializeOptions = new JsonSerializerOptions();

        public Worker(ILogger<Worker> logger, ICaster caster, IConfiguration configuration)
        {
            this.serializeOptions.Converters.Add(new AzaanTimeConverter());
            this.logger = logger;
            this.caster = caster;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            AppDomain.CurrentDomain.UnhandledException+=CurrentDomainOnUnhandledException;
            this.logger.LogInformation("Starting the caster: {time}", DateTimeOffset.Now);
            this.caster.Subscribe();
            string targetDevice = this.configuration["azaan:target"];
            this.logger.LogInformation(@$"Listening for castable devices... ({targetDevice})");
            while (!stoppingToken.IsCancellationRequested)
            {
                this.logger.LogInformation("New daily cycle.");
                AzaanTimes times = await GetBroadcastTimes();
                this.logger.LogInformation(times.ToString());
                Queue<DateTime> q = times.AsQueue();
                while (q.Peek() < DateTime.Now)
                {
                    DateTime item = q.Dequeue();
                    this.logger.LogInformation($"service spin up at {DateTime.Now}, discarding {item}");
                }

                this.logger.LogInformation($"Entering actionable loop with {q.Count}");
                while (q.Any())
                {
                    if (q.Peek() < DateTime.Now)
                    {
                        DateTime actionable = q.Dequeue();
                        this.logger.LogInformation($"Broadcasting {actionable}");

                        // await Broadcast();
                    }

                    await Task.Delay(int.Parse(this.configuration["azaan:delay"]), stoppingToken);
                }
            }
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.logger.LogError($"{sender}: {e}");
        }

        private async Task<AzaanTimes> GetBroadcastTimes()
        {
            HttpClient c = new HttpClient();
            c.DefaultRequestHeaders.Accept.Clear();
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.DefaultRequestHeaders.Add("User-Agent", "AzaanService 2.0");
            string url = $"{this.configuration["azaan:apitarget"]}{this.configuration["azaan:apikey"]}";
            Task<Stream> result = c.GetStreamAsync(url);
            AzaanTimes r = await JsonSerializer.DeserializeAsync<AzaanTimes>(await result, this.serializeOptions);
            return r;
        }

        private async Task Broadcast()
        {
            string targetDevice = this.configuration["azaan:target"];
            if (this.caster.Knows(targetDevice))
            {
                bool connected = await this.caster.Connect(targetDevice);
                if (connected)
                {
                    this.logger.LogInformation("Casting..", DateTimeOffset.Now);
                    bool played = await this.caster.Play(this.configuration["azaan:source"]);
                    if (!played)
                    {
                        this.logger.LogError($"Could not play to {targetDevice}. Not playing.");
                    }

                    this.caster.Disconnect(targetDevice);
                }
                else
                {
                    this.logger.LogError($"Could not connect to device {targetDevice}. Not playing.");
                }

                this.logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            else
            {
                this.logger.LogError($"Could not locate device {targetDevice}. Not playing.");
            }
        }
    }
}