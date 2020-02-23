namespace AzaanService.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using GoogleCast;
    using GoogleCast.Channels;
    using GoogleCast.Models.Media;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public class Caster : ICaster
    {
        private readonly IConfiguration config;
        private readonly ILogger<Worker> logger;
        private readonly Dictionary<string, IReceiver> list = new Dictionary<string, IReceiver>();
        private IDisposable subscription;
        private DeviceLocator deviceLocator;
        private IMediaChannel mediaChannel;

        public Caster(IConfiguration config, ILogger<Worker> logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public void Subscribe()
        {
            this.deviceLocator = new DeviceLocator();
            IObservable<IReceiver> observable = deviceLocator.FindReceiversContinuous();
            this.subscription = observable.Subscribe(this.OnNext);
        }

        public bool Connected { get; set; }

        public void Unsubscribe()
        {
            subscription.Dispose();
        }

        public void OnCompleted()
        {
            this.logger.LogInformation("Fin");
        }

        public void OnError(Exception error)
        {
            this.logger.LogError(error, "Oops");
        }

        public void OnNext(IReceiver value)
        {
            IReceiver receiver = value;

            this.logger.LogInformation($"Found {value.FriendlyName} @ ${value.IPEndPoint}");

            this.Add(receiver);
        }

        public async Task<bool> Connect(string byName)
        {
            IReceiver receiver = this.Get(byName);
            var sender = new Sender();

            this.logger.LogInformation("Connecting...");
            await sender.ConnectAsync(receiver);


            IMediaChannel mChannel = sender.GetChannel<IMediaChannel>();
            await sender.LaunchAsync(mChannel);
            this.mediaChannel = mChannel;
            this.Connected = true;
            return true;
        }

        public bool Disconnect(string byName)
        {
            IReceiver receiver = this.Get(byName);
            this.logger.LogInformation("dropping connection...");
            this.mediaChannel.Sender.Disconnect();
            this.Connected = false;
            return true;
        }

        public async Task<bool> Play(string contentLink)
        {
            FileStyleUriParser uriParser = new FileStyleUriParser();

            MediaInformation mediaInformation = new MediaInformation();
            mediaInformation.ContentId = contentLink;
            mediaInformation.ContentType = "audio/x-wav";
            await mediaChannel.LoadAsync(mediaInformation);
            await mediaChannel.PlayAsync();
            return true;
        }

        public bool Knows(string friendlyName)
        {
            lock (this.list)
            {
                return this.list.ContainsKey(friendlyName);
            }
        }

        private void Add(IReceiver value)
        {
            lock (this.list)
            {
                this.list.Add(value.FriendlyName, value);
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