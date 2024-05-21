// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Config;
using FastTunnel.Core.Forwarder.MiddleWare;
using FastTunnel.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using static FastTunnel.Core.Models.ForwardListInfo4Result;

namespace FastTunnel.Core.Client
{
    public class FastTunnelServer
    {
        public int ConnectedClientCount;
        public readonly IOptionsMonitor<DefaultServerConfig> ServerOption;
        public IProxyConfigProvider proxyConfig;
        private readonly ILogger<FastTunnelServer> logger;

        public event EventHandler<(string Name,int port)> ForwardRemoved;
        public event EventHandler<(string Name, int port)> ForwardAdded;

        public ConcurrentDictionary<string, (TaskCompletionSource<Stream>, CancellationToken)> ResponseTasks { get; } = new();

        public ConcurrentDictionary<string, WebInfo> WebList { get; private set; } = new();

        private ConcurrentDictionary<int, ForwardInfo<ForwardHandlerArg>> ForwardList { get; set; }= new ();

        /// <summary>
        /// 在线客户端列表
        /// </summary>
        public IList<TunnelClient> Clients = [];

        public FastTunnelServer(ILogger<FastTunnelServer> logger, IProxyConfigProvider proxyConfig, IOptionsMonitor<DefaultServerConfig> serverSettings)
        {
            this.logger = logger;
            ServerOption = serverSettings;
            this.proxyConfig = proxyConfig;
        }

        /// <summary>
        /// 客户端登录
        /// </summary>
        /// <param name="client"></param>
        internal void ClientLogin(TunnelClient client)
        {
            Interlocked.Increment(ref ConnectedClientCount);
            logger.LogInformation("客户端连接 {RemoteIpAddress} 当前在线数：{ConnectedClientCount}，统计Client连接数：{ConnectionCount}"
                , client.RemoteIpAddress, ConnectedClientCount, FastTunnelClientHandler.ConnectionCount);
            Clients.Add(client);
        }

        /// <summary>
        /// 客户端退出
        /// </summary>
        /// <param name="client"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void ClientLogout(TunnelClient client)
        {
            Interlocked.Decrement(ref ConnectedClientCount);
            logger.LogInformation("客户端关闭  {RemoteIpAddress} 当前在线数：{ConnectedClientCount}，统计Client连接数：{ConnectionCount}"
                , client.RemoteIpAddress, ConnectedClientCount, FastTunnelClientHandler.ConnectionCount - 1);
            Clients.Remove(client);
            client.Logout();
        }

        public bool TryGetValueForward(int remotePort,out ForwardInfo<ForwardHandlerArg> forward)
        {
            return ForwardList.TryGetValue(remotePort, out forward);
        }

        public void RemoveForward(int remotePort)
        {
            if(ForwardList.TryRemove(remotePort, out var val))
            {
                ForwardRemoved?.Invoke(this, (val?.SSHConfig?.Name, remotePort));
            }
        }

        public void AddForward(int remotePort, ForwardInfo<ForwardHandlerArg> forward)
        {
            if(ForwardList.TryAdd(remotePort, forward))
            {
                ForwardAdded?.Invoke(this, (forward?.SSHConfig?.Name, remotePort));
            }
        }

        public IEnumerable<int> GetAllUsedPorts()
        {
            return ForwardList.Select(x => x.Key);
        }

        public ResponseTempListInfo GetResponseTempList()
        {
            return new ()
            {
                 Count = ResponseTasks.Count,
                 Rows= ResponseTasks.Select(r => r.Key)
            };
        }

        public IEnumerable<ClientInfo4Result> GetClients()
        {
#pragma warning disable CA1305 // 指定 IFormatProvider
            return Clients.Select(x => new ClientInfo4Result
            {
                 WebInfos = x.WebInfos,
                 ForwardInfos = x.ForwardInfos,
                 RemoteIpAddress = x.RemoteIpAddress.ToString(),
                 StartTime=x.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            });
#pragma warning restore CA1305 // 指定 IFormatProvider
        }

        public WebListInfo4Result GetAllWebList()
        {
            return new()
            {
                 Count= WebList.Count,
                 Rows = WebList.Select(x=>new WebListInfo4Result.WebInfo4Result
                 {
                      Key = x.Key,
                      LocalIp= x.Value.WebConfig.LocalIp,
                      LocalPort = x.Value.WebConfig.LocalPort
                 })
            };
        }

        public ForwardListInfo4Result GetAllForwardList()
        {
            return new()
            {
                 Count = ForwardList.Count,
                  Rows = ForwardList.Select(x=>new ForwardListInfo4Result.ForwardInfo4Result
                  {
                      Key = x.Key,
                      Name = x.Value.SSHConfig.Name,
                      LocalIp = x.Value.SSHConfig.LocalIp,
                      LocalPort = x.Value.SSHConfig.LocalPort,
                      RemotePort = x.Value.SSHConfig.RemotePort,
                  })
            };
        }

        public IEnumerable<int> GetAllUsedPort()
        {
            return ForwardList.Select(x => x.Key);
        }
    }
}
