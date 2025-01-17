﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPLinkSmartDevices.Devices
{
    public class TPLinkSmartMultiPlug : TPLinkSmartDevice
    {
        /// <summary>
        /// If the all outlet relais are powered on
        /// </summary>
        public bool AllOutletsPowered { get; private set; }

        /// <summary>
        /// If the LED on the smart plug is on
        /// </summary>
        public bool LedOn { get; private set; }

        public string[] Features { get; private set; }

        public int OutletCount { get; private set; }

        public Outlet[] Outlets { get; private set; }

        public TPLinkSmartMultiPlug(string hostname, int port = 9999) : base(hostname, port)
        {
            Task.Run(() => Refresh()).GetAwaiter().GetResult();
        }

        private TPLinkSmartMultiPlug() { }

        public static async Task<TPLinkSmartMultiPlug> Create(string hostname, int port = 9999)
        {
            TPLinkSmartMultiPlug p = new TPLinkSmartMultiPlug() { Hostname = hostname, Port = port };
            await p.Refresh().ConfigureAwait(false);
            return p;
        }

        /// <summary>
        /// Refresh device information
        /// </summary>
        public async Task Refresh()
        {
            dynamic sysInfo = await Execute("system", "get_sysinfo").ConfigureAwait(false);

            JObject info = JObject.Parse(Convert.ToString(sysInfo));
            bool hasChildren = info["children"] != null;

            if (!hasChildren) throw new("This plug does not have multiple outlets. use TPLinkSmartPlug instead!");
            
            OutletCount = (int)sysInfo.child_num;
            Outlets = JsonConvert.DeserializeObject<List<Outlet>>(Convert.ToString(sysInfo.children)).ToArray();
            this.AllOutletsPowered = !this.Outlets.Any(o => o.OutletPowered == false);

            Features = ((string)sysInfo.feature).Split(':');
            LedOn = !(bool)sysInfo.led_off;

            await Refresh((object)sysInfo).ConfigureAwait(false);
        }

        public override async Task SetPoweredOn(bool value)
        {
            await Execute("system", "set_relay_state", "state", value ? 1 : 0).ConfigureAwait(false);
            this.AllOutletsPowered = value;
        }

        /// <summary>
        /// Send command which changes power state to plug
        /// </summary>
        public async Task SetPoweredOn(bool value, int outletId = -1)
        {
            if (outletId > OutletCount - 1 || outletId < -1) throw new ArgumentException("Plug does not have a outlet with specified id");

            //toggle all outlets of plug
            if (OutletCount == 1 || outletId == -1)
            {
                await Execute("system", "set_relay_state", "state", value ? 1 : 0).ConfigureAwait(false);
                this.AllOutletsPowered = value;
            }
            //toggle specific outlet
            else
            {
                JObject root = new()
                { 
                    new JProperty("context", new JObject { new JProperty("child_ids", GetPlugID(outletId)) }),
                    new JProperty("system", new JObject { 
                        new JProperty("set_relay_state", 
                        new JObject { new JProperty("state", value ? 1 : 0) }) 
                    }) 
                };

                string message = root.ToString(Formatting.None);
                await Execute("system", "set_relay_state", message).ConfigureAwait(false);
                this.Outlets[outletId].OutletPowered = value;
                this.AllOutletsPowered = !this.Outlets.Any(o => o.OutletPowered == false);
            }
        }

        private object GetPlugID(int outletId)
        {
            return JArray.FromObject(new[] { $"{DeviceId}0{outletId}" });
        }

        /// <summary>
        /// Send command which enables or disables night mode (LED state)
        /// </summary>
        public async Task SetLedOn(bool value)
        {
            await Execute("system", "set_led_off", "off", value ? 0 : 1).ConfigureAwait(false);
            this.LedOn = value;
        }

        public class Outlet
        {
            [JsonProperty("id")]
            public string Id { get; private set; }

            [JsonProperty("state")]
            public bool OutletPowered { get; set; }

            [JsonProperty("alias")]
            public string Alias { get; private set; }

            [JsonProperty("on_time")]
            public int OnTime { get; private set; }
        }
    }
}
