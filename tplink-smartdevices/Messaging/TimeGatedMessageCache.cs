﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPLinkSmartDevices.Messaging
{
    public class TimeGatedMessageCache : IMessageCache
    {
        public int TimeGateResetSeconds { get; set; } = 10;

        private List<MessageCacheItem> _cache = new();

        public TimeGatedMessageCache(int resetTimeSeconds = 10)
        {
            TimeGateResetSeconds = resetTimeSeconds;
        }

        public override async Task<dynamic> Request(SmartHomeProtocolMessage message, string hostname, int port)
        {
            MessageCacheItem cachedMessage = _cache.FirstOrDefault(c => c.Matches(message, hostname, port));

            if (cachedMessage != null)
            {
                if (cachedMessage.IsExpired(TimeGateResetSeconds))
                    _cache.Remove(cachedMessage);
                else
                    return cachedMessage.MessageResult;
            }

            dynamic result = await message.Execute(hostname, port).ConfigureAwait(false);
            _cache.Add(new MessageCacheItem(result, hostname, port));
            return result;
        }

        protected class MessageCacheItem
        {
            internal int Hash { get; set; }
            internal string Hostname { get; set; }
            internal int Port { get; set; }
            internal dynamic MessageResult { get; set; }
            internal DateTime Cached { get; set; }

            internal MessageCacheItem(dynamic messageResult, string hostname, int port)
            {
                MessageResult = messageResult;
                Hostname = hostname;
                Port = port;
                Cached = DateTime.Now;
            }

            internal bool IsExpired(int expirySeconds)
            {
                return Cached.AddSeconds(expirySeconds) < DateTime.Now;
            }

            internal bool Matches(SmartHomeProtocolMessage message, string hostname, int port)
            {
                if (Hostname != hostname || Port != port)
                    return false;

                return message.MessageHash == Hash;
            }
        }
    }
}
