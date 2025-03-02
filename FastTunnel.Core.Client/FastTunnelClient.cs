// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Config;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Handlers.Client;
using FastTunnel.Core.Models;
using FastTunnel.Core.Models.Massage;
using FastTunnel.Core.Utilitys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FastTunnel.Core.Client;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(LogInMassage))]
[JsonSerializable(typeof(Message<TunnelMassage>))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}

public class FastTunnelClient : IFastTunnelClient
{
    private ClientWebSocket socket;

    protected readonly ILogger<FastTunnelClient> _logger;
    protected DefaultClientConfig ClientConfig { get; private set; }
    public SuiDaoServer Server { get ; set ; }

    private readonly SwapHandler swapHandler;
    private readonly LogHandler logHandler;

    public FastTunnelClient(
        ILogger<FastTunnelClient> logger,
        SwapHandler newCustomerHandler,
        LogHandler logHandler,
        IOptionsMonitor<DefaultClientConfig> configuration)
    {
        _logger = logger;
        swapHandler = newCustomerHandler;
        this.logHandler = logHandler;
        ClientConfig = configuration.CurrentValue;
        Server = ClientConfig.Server;
    }

    /// <summary>
    /// 启动客户端
    /// </summary>
    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===== FastTunnel Client Start =====");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await loginAsync(cancellationToken);
                await ReceiveServerAsync(cancellationToken);
            }
            catch (Exception ex)
            {
#pragma warning disable CA2254 // 模板应为静态表达式
                _logger.LogError(ex, ex.Message);
#pragma warning restore CA2254 // 模板应为静态表达式
            }

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }

        _logger.LogInformation("===== FastTunnel Client End =====");
    }

    private async Task loginAsync(CancellationToken cancellationToken)
    {
        var logMsg = GetLoginMsg(cancellationToken);
        socket?.Abort();

        // 连接到的目标IP
        socket = new ClientWebSocket();
        socket.Options.RemoteCertificateValidationCallback = delegate { return true; };
        socket.Options.SetRequestHeader(FastTunnelConst.FASTTUNNEL_VERSION, AssemblyUtility.GetVersion().ToString());
        socket.Options.SetRequestHeader(FastTunnelConst.FASTTUNNEL_TOKEN, ClientConfig.Token);

        _logger.LogInformation("正在连接服务端 {ServerAddr}:{ServerPort}", Server.ServerAddr, Server.ServerPort);
        await socket.ConnectAsync(
            new Uri($"{Server.Protocol}://{Server.ServerAddr}:{Server.ServerPort}"), cancellationToken);

        _logger.LogDebug("连接服务端成功");

        // 登录
        await socket.SendCmdAsync(MessageType.LogIn, logMsg, cancellationToken);
    }

    protected virtual string GetLoginMsg(CancellationToken cancellationToken)
    {
        Server = ClientConfig.Server;

        return new LogInMassage
        {
            Webs = ClientConfig.Webs,
            Forwards = ClientConfig.Forwards,
        }.ToJson(jsonTypeInfo: SourceGenerationContext.Default.LogInMassage);

    }

    protected async Task ReceiveServerAsync(CancellationToken cancellationToken)
    {
        var utility = new WebSocketUtility(socket, ProcessLine);
        await utility.ProcessLinesAsync(cancellationToken);
    }

    private void ProcessLine(ReadOnlySequence<byte> line, CancellationToken cancellationToken)
    {
        HandleServerRequestAsync(line, cancellationToken);
    }

    private void HandleServerRequestAsync(ReadOnlySequence<byte> line, CancellationToken cancellationToken)
    {
        try
        {
            var row = line.ToArray();
            var cmd = row[0];
            IClientHandler handler = (MessageType)cmd switch
            {
                MessageType.SwapMsg or MessageType.Forward => swapHandler,
                MessageType.Log => logHandler,
                _ => throw new Exception($"未处理的消息：cmd={cmd}"),
            };
            var content = Encoding.UTF8.GetString(line.Slice(1));
            handler.HandlerMsgAsync(this, content, cancellationToken);
        }
        catch (Exception ex)
        {
#pragma warning disable CA2254 // 模板应为静态表达式
            _logger.LogError(ex, ex.Message);
#pragma warning restore CA2254 // 模板应为静态表达式
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===== FastTunnel Client Stoping =====");
        socket?.Abort();
        await Task.CompletedTask;
    }
}
