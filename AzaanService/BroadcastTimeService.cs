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

    public class BroadcastTimeService(ILogger<Worker> logger, string url)
    {
        public async Task<AzaanTimes> GetBroadcastTimes()
        {
            HttpClient c = new();
            c.DefaultRequestHeaders.Accept.Clear();
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.DefaultRequestHeaders.Add("User-Agent", "AzaanService 2.0");
            Console.WriteLine(url);
            Stream result = await c.GetStreamAsync(url);
            JsonDocument jd = await JsonDocument.ParseAsync(result);
            AzaanTimes r = new();

            //foreach (JsonProperty jsonElement in jd.RootElement.GetProperty("data").EnumerateObject().First().Value)
            JsonElement jsonElement = jd.RootElement.GetProperty("data").EnumerateObject().FirstOrDefault().Value;
            var dateFor = $"{DateTime.Today.Year}-{DateTime.Today.Month}-{DateTime.Today.Day}";
            foreach (JsonProperty jsonProperty in jsonElement.EnumerateObject())
            {
                logger.LogInformation("{JsonPropertyName}: {JsonPropertyValue}", jsonProperty.Name, jsonProperty.Value);
                var propertyName = jsonProperty.Name.ToLower();
                var val = jsonProperty.Value.GetString() ?? "";

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
                    case "sunset":
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

            logger.LogWarning("Something's wrong: {Root}", jd.RootElement.ToString());
            logger.LogTrace(r.ToString());

            return r;
        }
    }
}