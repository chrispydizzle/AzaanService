namespace AzaanService.Core
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using GoogleCast;
    using GoogleCast.Channels;
    using GoogleCast.Models.Media;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class DirectCaster(IConfiguration config, ILogger<Worker> logger, string target)
        : ICaster
    {
        private readonly IConfiguration config = config;
        private readonly Dictionary<string, IReceiver> list = new();
        private IMediaChannel? mediaChannel;

        public void Subscribe()
        {
            IPEndPoint ipEndPoint = new(IPAddress.Parse(target), 8009);
            IReceiver receiver = new Receiver() { IPEndPoint = ipEndPoint };
            this.Add(receiver);
        }

        public bool Connected { get; set; }

        public async Task Broadcast(string path)
        {
            if (this.Knows(target))
            {
                var connected = await this.Connect(target);
                if (connected)
                {
                    logger.LogInformation("Casting to {Target}..{TimeStamp}", target, DateTimeOffset.Now);
                    var played = await this.Play(path);
                    if (!played)
                    {
                        logger.LogWarning("Could not play to {Target}. Not playing.", target);
                    }

                    this.Disconnect(target);
                }
                else
                {
                    logger.LogWarning("Could not connect to device {Target}. Not playing.", target);
                }

                logger.LogInformation("Caster running at: {time}", DateTimeOffset.Now);
            }
            else
            {
                logger.LogWarning("Could not locate device {Target}. Not playing.", target);
            }
        }

        public void Unsubscribe()
        {
            this.Disconnect(target);
        }

        public void OnCompleted()
        {
            logger.LogInformation("Fin");
        }

        public void OnError(Exception error)
        {
            logger.LogError(error, "Oops");
        }

        public void OnNext(IReceiver value)
        {
            IReceiver receiver = value;
            logger.LogInformation("Found {ValueFriendlyName} @ ${ValueIpEndPoint}", value.FriendlyName, value.IPEndPoint);
            if (value.FriendlyName == target)
            {
                this.Add(receiver);
            }
        }

        public async Task<bool> Connect(string ipAddress)
        {
            if (!this.Knows(ipAddress)) {
                IPEndPoint ipEndPoint = new(IPAddress.Parse(ipAddress), 8009);
                IReceiver receiver = new Receiver() { IPEndPoint = ipEndPoint };
                this.Add(receiver);
            }
            Sender sender = new();
            await sender.ConnectAsync(this.Get(ipAddress));

            logger.LogInformation("Connecting...");

            IMediaChannel mChannel = sender.GetChannel<IMediaChannel>();
            await sender.LaunchAsync(mChannel);

            this.mediaChannel = mChannel;
            this.Connected = true;
            return true;
        }

        public bool Disconnect(string byName)
        {
            IReceiver receiver = this.Get(byName);
            logger.LogInformation("dropping connection...");
            this.mediaChannel?.Sender?.Disconnect();
            this.Connected = false;
            return true;
        }

        public async Task<bool> Play(string contentLink)
        {
            MediaInformation mediaInformation = new();
            mediaInformation.ContentId = contentLink;
            mediaInformation.ContentType = "audio/x-wav";
            if(mediaChannel != null) { 
                await mediaChannel.LoadAsync(mediaInformation);
                await mediaChannel.PlayAsync();
                return true;
            }

            return false;
        }

        public bool Knows(string friendlyName)
        {
            return this.list.ContainsKey(friendlyName);
        }

        private void Add(IReceiver value)
        {
            lock (this.list)
            {
                this.list.Add(value.IPEndPoint.Address.ToString(), value);
            }
        }

        private IReceiver Get(string id)
        {
            lock (this.list)
            {
                return this.list[id];
            }
        }
    }
}