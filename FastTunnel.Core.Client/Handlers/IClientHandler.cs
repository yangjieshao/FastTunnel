// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;

namespace FastTunnel.Core.Handlers.Client
{
    public interface IClientHandler
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="cleint"></param>
        /// <param name="msg"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task HandlerMsgAsync(FastTunnelClient cleint, string msg, CancellationToken cancellationToken);
    }
}
