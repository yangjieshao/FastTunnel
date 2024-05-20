// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Handlers.Client;

public class LogHandler : IClientHandler
{
    private readonly ILogger<LogHandler> _logger;

    public LogHandler(ILogger<LogHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandlerMsgAsync(FastTunnelClient cleint, string msg, CancellationToken cancellationToken)
    {
#pragma warning disable CA2254 // 模板应为静态表达式
        _logger.LogInformation(msg);
#pragma warning restore CA2254 // 模板应为静态表达式
        await Task.CompletedTask;
    }
}
