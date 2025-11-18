namespace AzaanService.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class AzaanTimes
    {
        public DateTime Isha { get; set; } = DateTime.MinValue;

        public DateTime Magrib { get; set; } = DateTime.MinValue;

        public DateTime Asr { get; set; } = DateTime.MinValue;

        public DateTime Dhuhr { get; set; } = DateTime.MinValue;

        public DateTime Fajr { get; set; } = DateTime.MinValue;

        public DateTime Shurooq { get; set; } = DateTime.MinValue;

        public Queue<DateTime> AsQueue()
        {
            Queue<DateTime> q = new();
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
            StringBuilder b=  new();
            b.AppendLine($"Fajr: {this.Fajr}");
            b.AppendLine($"Dhuhr: {this.Dhuhr}");
            b.AppendLine($"Asr: {this.Asr}");
            b.AppendLine($"Maghrib: {this.Magrib}");
            b.AppendLine($"Isha: {this.Isha}");
            b.AppendLine($"Shurooq: {this.Shurooq}");
            return b.ToString();
        }
    }
}