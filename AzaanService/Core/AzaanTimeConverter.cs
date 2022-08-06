namespace AzaanService.Core
{
    using System;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class AzaanTimeConverter : JsonConverter<AzaanTimes>
    {
        public override AzaanTimes Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Didn't start at start!");

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;

                if (reader.GetString() == "items") break;

                reader.Read();
            }

            reader.Read();
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
            }
            else
            {
                throw new JsonException("Something's wrong...");
            }

            AzaanTimes r = new AzaanTimes();
            while (reader.Read() && reader.CurrentDepth == 3)
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    throw new JsonException("The end has come too soon.");
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString() ?? "";
                    reader.Read();
                    string dateFor = $"{DateTime.Today.Year}-{DateTime.Today.Month}-{DateTime.Today.Day}";
                    switch (propertyName)
                    {
                        case "date_for":
                            break;
                        case "fajr":
                            r.Fajr = AzaanTimeConverter.CustomParse(dateFor, reader.GetString());
                            break;
                        case "dhuhr":
                            r.Dhuhr = AzaanTimeConverter.CustomParse(dateFor, reader.GetString());
                            break;
                        case "asr":
                            r.Asr = AzaanTimeConverter.CustomParse(dateFor, reader.GetString());
                            break;
                        case "maghrib":
                            r.Magrib = AzaanTimeConverter.CustomParse(dateFor, reader.GetString());
                            break;
                        case "isha":
                            r.Isha = AzaanTimeConverter.CustomParse(dateFor, reader.GetString());
                            break;
                    }
                }

                if (r.IsFilled())
                {
                    return r;
                }
            }

            return r;
        }

        public static DateTime CustomParse3(string dateString, string? timeString)
        {
            return DateTime.ParseExact($"{dateString} {timeString}", "yyyy-M-d h:mm tt", CultureInfo.InvariantCulture);
        }

        public static DateTime CustomParse(string dateString, string? timeString)
        {
            return DateTime.ParseExact($"{dateString} {timeString}", "yyyy-M-d HH:mm", CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, AzaanTimes value, JsonSerializerOptions options)
        {
            throw new JsonException("Cannot write this bad boy.");
        }
    }
}