﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using TPLinkSmartDevices.Devices;

namespace TPLinkSmartDevices.Data.Schedule
{
    public class Schedule
    {
        /// <summary>
        /// identifier of schedule
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// custom name of CountDown rule
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// start time option for schedule
        /// </summary>
        [JsonProperty("stime_opt")]
        public TimeOption StartTimeOption { get; set; }

        /// <summary>
        /// start time after midnight
        /// </summary>
        [JsonProperty("smin")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// whether to turn device on or off at start of rule.
        /// 0 = turn off,
        /// 1 = turn on,
        /// -1 and 2: purpose still unknown
        /// </summary>
        [JsonProperty("sact")]
        public int StartAction { get; set; }

        /// <summary>
        /// end time option for schedule
        /// </summary>
        [JsonProperty("etime_opt")]
        public TimeOption EndTimeOption { get; set; }

        /// <summary>
        /// end time in minutes after midnight
        /// </summary>
        [JsonProperty("emin")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// whether to turn device on or off at end of rule.
        /// 0 = turn off,
        /// 1 = turn on,
        /// -1 and 2: purpose still unknown
        /// </summary>
        [JsonProperty("eact")]
        public int EndAction { get; set; }

        /// <summary>
        /// days that rule should be active,
        /// </summary>
        [JsonProperty("wday")]
        [JsonConverter(typeof(WeekdayConverter))]
        public Weekdays Weekdays { get; set; }

        /// <summary>
        /// if the schdeule is currently active or not
        /// </summary>
        [JsonProperty("enable")]
        [JsonConverter(typeof(BoolConverter))]
        public bool Enabled { get; set; }

        /// <summary>
        /// whether the rule will be used more than one time.
        /// </summary>
        [JsonProperty("repeat")]
        [JsonConverter(typeof(BoolConverter))]
        //seems to be required to set to true, otherwise throws -3 protocol error
        public bool Repeat => true;  

        [JsonProperty("s_light")]
        public object LightState => new { saturation = Saturation, hue = Hue, brightness = Brightness, color_temp = ColorTemp };

        /// <summary>
        /// sets saturation of bulb’s light output. values from 0 to 100 are accepted.
        /// </summary>
        [JsonIgnore]
        public int Saturation { get; set; }

        /// <summary>
        /// sets hue of bulb’s light output. values from 0 to 360 accepted.
        /// </summary>
        [JsonIgnore]
        public int Hue { get; set; }

        /// <summary>
        /// sets brightness of bulb’s light output. values from 0 to 100 are accepted.
        /// </summary>
        [JsonIgnore]
        public int Brightness { get; set; }

        /// <summary>
        /// sets brightness of bulb’s light output. values from 0 to 100 are accepted.
        /// </summary>
        [JsonIgnore]
        public int ColorTemp { get; set; }

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData = new Dictionary<string, JToken>();

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_additionalData.ContainsKey("s_light"))
            { 
                Hue = (int)_additionalData["s_light"]["hue"];
                Saturation = (int)_additionalData["s_light"]["saturation"];
                Brightness = (int)_additionalData["s_light"]["brightness"];
                ColorTemp = (int)_additionalData["s_light"]["color_temp"];
            }
        }

        public bool ShouldSerializeId()
        {
            return Id != null;
        }

        //TODO: additional unknown parameters: latitude, longitude, year, month, day, force, frequency, on_off, eoffset/soffset !!
    }

    internal class TimeSpanConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            int timeSinceMidnight = Convert.ToInt32(((TimeSpan)value).TotalMinutes);
            if (timeSinceMidnight > 1440) throw new ArgumentException("invalid time span");
            writer.WriteValue(timeSinceMidnight);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return TimeSpan.FromMinutes(Convert.ToDouble(reader.Value));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan);
        }
    }

    internal static class SchedulesExtensions
    {
        internal static async Task<List<Schedule>> RetrieveSchedules(this ISchedule device, string ns)
        {
            dynamic result = await ((TPLinkSmartDevice)device).Execute(ns, "get_rules").ConfigureAwait(false);
            string rule_list = Convert.ToString(result.rule_list);
            return JsonConvert.DeserializeObject<List<Schedule>>(rule_list);
        }

        internal static async Task AddSchedule(this ISchedule device, string ns, Schedule schedule)
        {
            JObject payload = JObject.FromObject(schedule);
            if (ns == "schedule")
            {
                payload.Property("s_light")?.Remove();
            }
            dynamic result = await ((TPLinkSmartDevice)device).Execute(ns, "add_rule", payload).ConfigureAwait(false);
            schedule.Id = (string)result.id;
            device.Schedules.Add(schedule);
        }

        internal static async Task EditSchedule(this ISchedule device, string ns, Schedule schedule)
        {
            JObject payload = JObject.FromObject(schedule);
            dynamic result = await ((TPLinkSmartDevice)device).Execute(ns, "edit_rule", payload).ConfigureAwait(false);
        }
    }
}
