﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace TPLinkSmartDevices.Data.Schedule
{
    [Flags]
    public enum Weekdays
    {
        None = 0,
        Sunday = 1 << 0,
        Monday = 1 << 1,
        Tuesday = 1 << 2,
        Wednesday = 1 << 3,
        Thursday = 1 << 4,
        Friday = 1 << 5,
        Saturday = 1 << 6,
        WeekendDays = Sunday | Saturday,
        WorkDays = Monday | Tuesday | Wednesday | Thursday | Friday,
        EveryDay = WeekendDays | WorkDays
    }

    /// <summary>
    /// converts Weekdays flags to array of weekday bits. example: (Weekdays.Saturday | Weekdays.Monday) ==> [0, 1, 0, 0, 0, 0, 1]
    /// </summary>
    public class WeekdayConverter : JsonConverter
    {

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Weekdays weekdays = (Weekdays)value;

            writer.WriteStartArray();
            for (int i = 0; i < 7; i++)
            {
                if (weekdays.HasFlag((Weekdays)(1 << i)))
                {
                    writer.WriteValue(1);
                }
                else
                {
                    writer.WriteValue(0);
                }
            }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Weekdays weekdays = Weekdays.None;
            if (reader.TokenType != JsonToken.StartArray)
            {
                return weekdays;
            }
            JArray daySource = JArray.Load(reader);
            for (int i = 0; i < daySource.Count; i++)
            {
                if ((int)daySource[i] == 0) continue;

                weekdays |= (Weekdays)(1 << i);
            }

            return weekdays;
        }

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return false;
        }
    }
}
