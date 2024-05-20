// Copyright (c) 2019-2022 Gui.H. https://github.com/FastTunnel/FastTunnel
// The FastTunnel licenses this file to you under the Apache License Version 2.0.
// For more details,You may obtain License file at: https://github.com/FastTunnel/FastTunnel/blob/v2/LICENSE

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Client.Sockets;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Handlers.Client
{
    public class SwapHandler : IClientHandler
    {
        private readonly ILogger<SwapHandler> _logger;
        private static int connectionCount;

        public SwapHandler(ILogger<SwapHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandlerMsgAsync(FastTunnelClient cleint, string msg, CancellationToken cancellationToken)
        {
            var msgs = msg.Split('|');
            var requestId = msgs[0];
            var address = msgs[1];

            await swap(cleint, requestId, address, cancellationToken);
        }

        private async Task swap(FastTunnelClient cleint, string requestId, string address, CancellationToken cancellationToken)
        {
            try
            {
                Interlocked.Increment(ref connectionCount);
                _logger.LogDebug("======Swap {requestId} Start======", requestId);
                using var serverStream = await createRemote(requestId, cleint, cancellationToken);
                using var localStream = await createLocal(requestId, address, cancellationToken);
                var taskX = serverStream.CopyToAsync(localStream, cancellationToken);
                var taskY = localStream.CopyToAsync(serverStream, cancellationToken);

                await Task.WhenAny(taskX, taskY);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Swap error {requestId}", requestId);
            }
            finally
            {
                Interlocked.Decrement(ref connectionCount);
                _logger.LogDebug("======Swap {requestId} End======", requestId);
                _logger.LogDebug("统计SwapHandler连接数：{connectionCount}", connectionCount);
            }
        }

#pragma warning disable IDE0060 // 删除未使用的参数
        private async Task<Stream> createLocal(string requestId, string localhost, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // 删除未使用的参数
        {
            var socket = await DnsSocketFactory.ConnectAsync(localhost.Split(":")[0], int.Parse(localhost.Split(":")[1]), cancellationToken);
            return new NetworkStream(socket, true) { ReadTimeout = 1000 * 60 * 10 };
        }

        private async Task<Stream> createRemote(string requestId, FastTunnelClient cleint, CancellationToken cancellationToken)
        {
            var socket = await DnsSocketFactory.ConnectAsync(cleint.Server.ServerAddr, cleint.Server.ServerPort, cancellationToken);
            Stream serverStream = new NetworkStream(socket, true) { ReadTimeout = 1000 * 60 * 10 };

            if (cleint.Server.Protocol == "wss")
            {
                var sslStream = new SslStream(serverStream, false, delegate { return true; });
                await sslStream.AuthenticateAsClientAsync(cleint.Server.ServerAddr);
                serverStream = sslStream;
            }

            var reverse = $"PROXY /{requestId} HTTP/1.1\r\nHost: {cleint.Server.ServerAddr}:{cleint.Server.ServerPort}\r\n\r\n";
            var requestMsg = Encoding.UTF8.GetBytes(reverse);
            await serverStream.WriteAsync(requestMsg, cancellationToken);
            return serverStream;
        }
    }
}
