using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TPLinkSmartDevices.Devices;
using Microsoft.CSharp.RuntimeBinder;
using TPLinkSmartDevices.Events;
using TPLinkSmartDevices.Messaging;

namespace TPLinkSmartDevices
{
    public class TPLinkDiscovery
    {
        public event EventHandler<DeviceFoundEventArgs> DeviceFound;

        private int PORT_NUMBER = 9999;

        public List<TPLinkSmartDevice> DiscoveredDevices { get; private set; }

        private UdpClient udp;

        private bool discoveryComplete = false;

        public TPLinkDiscovery()
        {
            DiscoveredDevices = new();
        }

        public async Task<List<TPLinkSmartDevice>> Discover(int port=9999, int timeout=5000, string target = "255.255.255.255")
        {
            discoveryComplete = false;

            DiscoveredDevices.Clear();
            PORT_NUMBER = port;

            await SendDiscoveryRequestAsync(target).ConfigureAwait(false);

            udp = new(PORT_NUMBER)
            {
                EnableBroadcast = true
            };

            return await Task.WhenAny(Task.Delay(timeout), Receive())
                .ContinueWith(t =>
                {
                    discoveryComplete = true;
                    udp.Close();
                    udp = null;

                    return DiscoveredDevices;
                })
                .ConfigureAwait(false);
        }

        private async Task Receive()
        {
            while (true)
            {
                if (discoveryComplete) //Prevent ObjectDisposedException/NullReferenceException when the Close() function is called
                    return;

                IPEndPoint ip = new(IPAddress.Any, PORT_NUMBER);
                UdpReceiveResult result = await udp.ReceiveAsync().ConfigureAwait(false);
                ip = result.RemoteEndPoint;
                string message = Encoding.ASCII.GetString(Messaging.SmartHomeProtocolEncoder.Decrypt(result.Buffer));

                TPLinkSmartDevice device = null;
                try
                {
                    dynamic sys_info = ((dynamic)JObject.Parse(message)).system.get_sysinfo;
                    string model = (string)sys_info.model;
                    if (model != null)
                    {
                        if (model.StartsWith("HS110"))
                            device = await TPLinkSmartMeterPlug.Create(ip.Address.ToString()).ConfigureAwait(false);
                        else if (model.StartsWith("HS300") || model.StartsWith("KP303") || model.StartsWith("HS107"))
                            device = await TPLinkSmartMultiPlug.Create(ip.Address.ToString()).ConfigureAwait(false);
                        else if (model.StartsWith("HS220"))
                            device = await TPLinkSmartDimmer.Create(ip.Address.ToString()).ConfigureAwait(false);
                        else if (model.StartsWith("HS"))
                            device = await TPLinkSmartPlug.Create(ip.Address.ToString()).ConfigureAwait(false);
                        else if (model.StartsWith("KL") || model.StartsWith("LB"))
                            device = await TPLinkSmartBulb.Create(ip.Address.ToString()).ConfigureAwait(false);
                    }
                }
                catch (RuntimeBinderException ex)
                {
                    //discovered wrong device
                    Console.WriteLine(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                if (device != null)
                {
                    DiscoveredDevices.Add(device);
                    OnDeviceFound(device);
                }
            }
        }

        private async Task SendDiscoveryRequestAsync(string target)
        {
            UdpClient client = new(PORT_NUMBER);
            IPEndPoint ip = new(IPAddress.Parse(target), PORT_NUMBER);

            string discoveryJson = JObject.FromObject(new
            {
                system = new { get_sysinfo = (object)null },
                emeter = new { get_realtime = (object)null }
            }).ToString(Newtonsoft.Json.Formatting.None);
            byte[] discoveryPacket = Messaging.SmartHomeProtocolEncoder.Encrypt(discoveryJson).ToArray();

            byte[] bytes = discoveryPacket.Skip(4).ToArray();
            client.EnableBroadcast = true;
            await client.SendAsync(bytes, bytes.Length, ip).ConfigureAwait(false);
            client.Close();
            client.Dispose();
        }

        private void OnDeviceFound(TPLinkSmartDevice device)
        {
            DeviceFound?.Invoke(this, new(device));
        }

        /// <summary>
        /// Makes device connect to specified network. Host who runs the application needs to be connected to the open configuration network! (TP-Link_Smart Plug_XXXX or similar)
        /// </summary>
        public async Task Associate(string ssid, string password, int type = 3)
        {
            dynamic scan = await new SmartHomeProtocolMessage("netif","get_scaninfo","refresh","1").Execute("192.168.0.1", 9999).ConfigureAwait(false);

            if (scan == null || !scan.ToString().Contains(ssid))
            {
                throw new("Couldn't find network!");
            }

            JArray networks = JArray.Parse(Convert.ToString(scan.ap_list));
            JToken network = networks.First(n => n["ssid"].ToString() == ssid);
            type = (int)network["key_type"];

            dynamic result = await new SmartHomeProtocolMessage("netif", "set_stainfo", new JObject
                {
                    new JProperty("ssid", ssid),
                    new JProperty("password", password),
                    new JProperty("key_type", type)
                }, null).Execute("192.168.0.1", 9999).ConfigureAwait(false);

            if (result == null)
            {
                throw new("Couldn't connect to network. Check password");
            }
            else if (result["err_code"] != null && result.err_code != 0)
                throw new($"Protocol error {result.err_code} ({result.err_msg})");
        }
    }
}
