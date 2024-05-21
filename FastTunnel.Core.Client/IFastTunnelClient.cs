// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Config;

namespace FastTunnel.Core.Client
{
    public interface IFastTunnelClient
    {
        public SuiDaoServer Server { get; set; }

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
