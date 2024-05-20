// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Forwarder;

namespace FastTunnel.Core.Forwarder;

public class FastTunnelForwarderHttpClientFactory : ForwarderHttpClientFactory
{
    private readonly ILogger<FastTunnelForwarderHttpClientFactory> logger;
    private readonly FastTunnelServer fastTunnelServer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static int connectionCount;

    public FastTunnelForwarderHttpClientFactory(
        ILogger<FastTunnelForwarderHttpClientFactory> logger,
        IHttpContextAccessor httpContextAccessor, FastTunnelServer fastTunnelServer)
    {
        this.fastTunnelServer = fastTunnelServer;
        this.logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override void ConfigureHandler(ForwarderHttpClientContext context, SocketsHttpHandler handler)
    {
        base.ConfigureHandler(context, handler);
        handler.ConnectCallback = ConnectCallback;
    }

    private async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.InitialRequestMessage.RequestUri.Host;

        var contextRequest = _httpContextAccessor.HttpContext;
        //var lifetime = contextRequest.Features.Get<IConnectionLifetimeFeature>()!;

        try
        {
            Interlocked.Increment(ref connectionCount);
            var res = await proxyAsync(host, context, contextRequest.RequestAborted);
            return res;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            Interlocked.Decrement(ref connectionCount);
            logger.LogDebug("统计YARP连接数：{connectionCount}", connectionCount);
        }
    }

    public async ValueTask<Stream> proxyAsync(string host, SocketsHttpConnectionContext context, CancellationToken cancellation)
    {
        if (!fastTunnelServer.WebList.TryGetValue(host, out var web))
        {
            // 客户端已离线
            return await OfflinePage(host, context);
        }

        var msgId = Guid.NewGuid().ToString().Replace("-", "");

        TaskCompletionSource<Stream> tcs = new();
        logger.LogDebug("[Http]Swap开始 {msgId}|{host}=>{LocalIp}:{LocalPort}"
            , msgId, host, web.WebConfig.LocalIp, web.WebConfig.LocalPort);

        cancellation.Register(() =>
        {
            logger.LogDebug("[Proxy TimeOut]:{msgId}", msgId);
            tcs.TrySetCanceled();
        });

        fastTunnelServer.ResponseTasks.TryAdd(msgId, (tcs, cancellation));

        try
        {
            // 发送指令给客户端，等待建立隧道
            await web.Socket.SendCmdAsync(MessageType.SwapMsg, $"{msgId}|{web.WebConfig.LocalIp}:{web.WebConfig.LocalPort}", cancellation);
            var res = await tcs.Task.WaitAsync(cancellation);

            logger.LogDebug("[Http]Swap OK {msgId}", msgId);
            return res;
        }
        catch (WebSocketException)
        {
            // 通讯异常，返回客户端离线
            return await OfflinePage(host, context);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            fastTunnelServer.ResponseTasks.TryRemove(msgId, out _);
        }
    }

    private async ValueTask<Stream> OfflinePage(string host, SocketsHttpConnectionContext context)
    {
        var bytes = Encoding.UTF8.GetBytes(
            $"HTTP/1.1 200 OK\r\nContent-Type:text/html; charset=utf-8\r\n\r\n{TunnelResource.Page_Offline}\r\n");

        return await Task.FromResult(new ResponseStream(bytes));
    }
}
