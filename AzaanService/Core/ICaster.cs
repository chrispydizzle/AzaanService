namespace AzaanService.Core
{
    using System;
    using System.Threading.Tasks;
    using GoogleCast;

    public interface ICaster : IObserver<IReceiver>
    {
        public void Subscribe();
        public Task<bool> Connect(string selection);
        public bool Disconnect(string selection);
        public bool Knows(string friendlyName);
        public Task<bool> Play(string contentLink);
        public bool Connected { get; set; }
    }
}