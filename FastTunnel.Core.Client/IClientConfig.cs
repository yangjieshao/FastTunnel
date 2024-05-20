// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Collections.Generic;
using FastTunnel.Core.Config;
using FastTunnel.Core.Models;

namespace FastTunnel.Core.Client
{
    public interface IClientConfig
    {
        public SuiDaoServer Server { get; set; }

        public IEnumerable<WebConfig> Webs { get; set; }

        public IEnumerable<ForwardConfig> Forwards { get; set; }
    }
}
