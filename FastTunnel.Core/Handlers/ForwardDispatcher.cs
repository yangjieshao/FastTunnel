// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Exceptions;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Listener;
using FastTunnel.Core.Models;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Handlers
{
    public class ForwardDispatcher
    {
        private readonly FastTunnelServer _server;
        private readonly ForwardConfig _config;
        private readonly ILogger logger;

        public ForwardDispatcher(ILogger logger, FastTunnelServer server, ForwardConfig config)
        {
            this.logger = logger;
            _server = server;
            _config = config;
        }

        /// <summary>
        /// </summary>
        /// <param name="_socket">用户请求</param>
        /// <param name="client">FastTunnel客户端</param>
        /// <param name="listener"></param>
        /// <returns></returns>
        public async Task DispatchAsync(Socket _socket, WebSocket client, PortProxyListener listener)
        {
            var msgId = Guid.NewGuid().ToString().Replace("-", "");

            try
            {
                await Task.Yield();
                logger.LogDebug("[Forward]Swap开始 {msgId}|{RemotePort}=>{LocalIp}:{LocalPort}"
                    , msgId, _config.RemotePort, _config.LocalIp, _config.LocalPort);

                var tcs = new TaskCompletionSource<Stream>();

                _server.ResponseTasks.TryAdd(msgId, (tcs, CancellationToken.None));

                try
                {
                    await client.SendCmdAsync(MessageType.Forward, $"{msgId}|{_config.LocalIp}:{_config.LocalPort}", CancellationToken.None);
                }
                catch (SocketClosedException sex)
                {
                    // TODO:客户端已掉线，但是没有移除对端口的监听
                    logger.LogError("[Forward]Swap 客户端已离线 {msg}", sex.Message);
                    tcs.TrySetCanceled();
                    Close(_socket);
                    return;
                }
                catch (Exception ex)
                {
                    // 网络不稳定
                    logger.LogError(ex, $"[Forward]Swap Exception");
                    tcs.TrySetCanceled();
                    Close(_socket);
                    return;
                }

                using var stream1 = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
                using var stream2 = new NetworkStream(_socket, true) { ReadTimeout = 1000 * 60 * 10 };
                await Task.WhenAny(stream1.CopyToAsync(stream2), stream2.CopyToAsync(stream1));
            }
            catch (Exception ex)
            {
                logger.LogDebug("[Forward]Swap Error {msgId}：{msg}", msgId, ex.Message);
            }
            finally
            {
                logger.LogDebug("[Forward]Swap结束 {msgId}", msgId);
                _server.ResponseTasks.TryRemove(msgId, out _);
                listener.DecrementClients();
            }
        }

        private void Close(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }
            finally
            {
                socket.Close();
            }
        }
    }
}
