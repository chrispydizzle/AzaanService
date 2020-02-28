namespace AzaanService.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class AzaanTimes
    {
        public AzaanTimes()
        {
            this.Isha = DateTime.MinValue;
            this.Magrib = DateTime.MinValue;
            this.Asr = DateTime.MinValue;
            this.Dhuhr = DateTime.MinValue;
            this.Fajr = DateTime.MinValue;
        }

        public DateTime Isha { get; set; }

        public DateTime Magrib { get; set; }

        public DateTime Asr { get; set; }

        public DateTime Dhuhr { get; set; }

        public DateTime Fajr { get; set; }

        public Queue<DateTime> AsQueue()
        {
            Queue<DateTime> q = new Queue<DateTime>();
            q.Enqueue(this.Fajr);
            q.Enqueue(this.Dhuhr);
            q.Enqueue(this.Asr);
            q.Enqueue(this.Magrib);
            q.Enqueue(this.Isha);
            return q;
        }

        public bool IsFilled() =>
            this.Isha != DateTime.MinValue
            && this.Magrib != DateTime.MinValue
            && this.Asr != DateTime.MinValue
            && this.Dhuhr != DateTime.MinValue
            && this.Fajr != DateTime.MinValue;

        public override string ToString()
        {
            StringBuilder b=  new StringBuilder();
            b.AppendLine($"Fajr: {this.Fajr}");
            b.AppendLine($"Dhuhr: {this.Dhuhr}");
            b.AppendLine($"Asr: {this.Asr}");
            b.AppendLine($"Maghrib: {this.Magrib}");
            b.AppendLine($"Isha: {this.Isha}");
            return b.ToString();
        }
    }
}