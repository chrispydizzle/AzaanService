namespace AzaanService.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using GoogleCast;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class CasterSet : ICaster
    {
        private readonly Dictionary<string, ICaster> casters;
        private readonly ILogger<Worker> logger;
        private readonly IConfiguration config;

        public CasterSet(IConfiguration config, ILogger<Worker> logger)
        {
            this.config = config;
            this.logger = logger;
            this.casters = new Dictionary<string, ICaster>();
        }

        public void OnCompleted()
        {
            this.logger.LogError($"Set alerted about a completion. Something is wrong.");
        }

        public void OnError(Exception error)
        {
            this.logger.LogError($"Set exception {error}. Something is wrong.");
        }

        public void OnNext(IReceiver value)
        {
            this.logger.LogError($"Set alerted about {value.FriendlyName}. Something is wrong.");
        }

        public void Subscribe()
        {
            foreach (IConfigurationSection target in this.config.GetSection("azaan:target").GetChildren())
            {
                var c = new Caster(this.config, this.logger, target.Value);
                c.Subscribe();

                this.casters.Add(target.Value, c);
                this.logger.LogInformation(@$"Listening for castable devices... ({target.Value})");
            }

            if (!this.casters.Any())
            {
                this.logger.LogWarning("No target devices found in configuration. No casting will be done.");
            }
        }

        public async Task<bool> Connect(string selection)
        {
            this.logger.LogError($"Set received connect signal for {selection}. Something is wrong.");
            return await this.casters[selection].Connect(selection);
        }

        public bool Disconnect(string selection)
        {
            this.logger.LogError($"Set received disconnect signal for {selection}. Something is wrong.");
            return this.casters[selection].Disconnect(selection);
        }

        public bool Knows(string friendlyName)
        {
            return this.casters[friendlyName].Knows(friendlyName);
        }

        public Task<bool> Play(string contentLink)
        {
            this.logger.LogError($"Set received play signal for {contentLink}. Something is wrong.");
            throw new InvalidOperationException("Play sent to cast set. (Try broadcast)");
        }

        public bool Connected { get; set; }

        public async Task Broadcast()
        {
            Task[] casting = new Task[this.casters.Count];
            int counter = 0;
            foreach (KeyValuePair<string, ICaster> kvp in this.casters)
            {
                casting[counter] = kvp.Value.Broadcast();
                counter++;
            }

            await Task.WhenAll(casting);
        }
    }
}