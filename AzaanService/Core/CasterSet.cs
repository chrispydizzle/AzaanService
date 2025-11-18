namespace AzaanService.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using GoogleCast;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class CasterSet(IConfiguration config, ILogger<Worker> logger) : ICaster
    {
        private readonly Dictionary<string, ICaster> casters = new();

        public void OnCompleted()
        {
            logger.LogError($"Set alerted about a completion. Something is wrong.");
        }

        public void OnError(Exception error)
        {
            logger.LogError("Set exception {Exception}. Something is wrong.", error);
        }

        public void OnNext(IReceiver value)
        {
            logger.LogError("Set alerted about {ValueFriendlyName}. Something is wrong.", value.FriendlyName);
        }

        public void Subscribe()
        {
            IConfigurationSection? targetSection = config.GetSection("azaan:target");
            var targets = targetSection?.GetChildren();
            
            if (targets == null)
            {
                logger.LogWarning("No target devices found in configuration. No casting will be done.");
                return;
            }

            foreach (IConfigurationSection target in targets)
            {
                if (string.IsNullOrEmpty(target.Value))
                {
                    logger.LogWarning("Skipping target with empty value.");
                    continue;
                }

                if (target.Value.StartsWith("DIRECT:"))
                {
                    var directTarget = target.Value.Substring(7);
                    DirectCaster dc = new(config, logger, directTarget);
                    dc.Subscribe();

                    this.casters.Add(directTarget, dc);
                    logger.LogInformation(@"Added direct castable device... ({DirectTarget})", directTarget);
                    continue;
                }
                
                Caster c = new(logger, target.Value);
                c.Subscribe();

                this.casters.Add(target.Value, c);
                logger.LogInformation(@"Listening for castable devices... ({TargetValue})", target.Value);
            }

            if (!this.casters.Any())
            {
                logger.LogWarning("No target devices found in configuration. No casting will be done.");
            }
        }

        public async Task<bool> Connect(string selection)
        {
            logger.LogError("Set received connect signal for {Selection}. Something is wrong.", selection);
            return await this.casters[selection].Connect(selection);
        }

        public bool Disconnect(string selection)
        {
            logger.LogError("Set received disconnect signal for {Selection}. Something is wrong.", selection);
            return this.casters[selection].Disconnect(selection);
        }

        public bool Knows(string friendlyName)
        {
            return this.casters[friendlyName].Knows(friendlyName);
        }

        public Task<bool> Play(string contentLink)
        {
            logger.LogError("Set received play signal for {ContentLink}. Something is wrong.", contentLink);
            throw new InvalidOperationException("Play sent to cast set. (Try broadcast)");
        }

        public bool Connected { get; set; }

        public async Task Broadcast(string path)
        {
            Task[] casting = new Task[this.casters.Count];
            var counter = 0;
            foreach (KeyValuePair<string, ICaster> kvp in this.casters)
            {
                if(config["nobroadcast"] != "true")
                { 
                    casting[counter] = kvp.Value.Broadcast(path);
                }
                else
                {
                    logger.LogInformation("Skipping broadcast to {KvpKey} due to nobroadcast setting.", kvp.Key);
                    casting[counter] = Task.CompletedTask;
                }

                counter++;
            }

            await Task.WhenAll(casting);
        }
    }
}