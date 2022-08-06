namespace AzaanService
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.Extensions.Logging;

    public class BroadcastTimeService
    {
        private readonly ILogger<Worker> logger;
        private readonly string url;

        public BroadcastTimeService(ILogger<Worker> logger, string url)
        {
            this.logger = logger;
            this.url = url;
        }

        public async Task<AzaanTimes> GetBroadcastTimes()
        {
            HttpClient c = new HttpClient();
            c.DefaultRequestHeaders.Accept.Clear();
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.DefaultRequestHeaders.Add("User-Agent", "AzaanService 2.0");
            string url = this.url;
            Stream result = await c.GetStreamAsync(url);
            JsonDocument jd = JsonDocument.Parse(result);
            AzaanTimes r = new AzaanTimes();

            //foreach (JsonProperty jsonElement in jd.RootElement.GetProperty("data").EnumerateObject().First().Value)
            JsonElement jsonElement = jd.RootElement.GetProperty("data").EnumerateObject().FirstOrDefault().Value;
            string dateFor = $"{DateTime.Today.Year}-{DateTime.Today.Month}-{DateTime.Today.Day}";
            foreach (JsonProperty jsonProperty in jsonElement.EnumerateObject())
            {
                this.logger.LogInformation($"{jsonProperty.Name}: {jsonProperty.Value}");
                string propertyName = jsonProperty.Name.ToLower();
                string val = jsonProperty.Value.GetString() ?? "";

                switch (propertyName)
                {
                    case "date_for":
                        dateFor = val;
                        break;
                    case "sunrise": //fajr
                        r.Fajr = AzaanTimeConverter.CustomParse(dateFor, val);
                        break;
                    case "dhuhr":
                        r.Dhuhr = AzaanTimeConverter.CustomParse(dateFor, val);
                        break;
                    case "asr":
                        r.Asr = AzaanTimeConverter.CustomParse(dateFor, val);
                        break;
                    case "maghrib":
                        r.Magrib = AzaanTimeConverter.CustomParse(dateFor, val);
                        break;
                    case "isha":
                        r.Isha = AzaanTimeConverter.CustomParse(dateFor, val);
                        break;
                    case "shurooq":
                        //r.Shurooq = AzaanTimeConverter.CustomParse(dateFor, val);
                        break;
                }
            }

            if (r.IsFilled()) return r;

            this.logger.LogWarning($"Something's wrong: {jd.RootElement.ToString()}");
            this.logger.LogTrace(r.ToString());

            return r;
        }
    }
}