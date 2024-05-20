// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastTunnel.Core.Models;

public class ResponseTempListInfo
{
    public int Count { set; get; }
    public IEnumerable<string> Rows { set; get; }
}

public class WebListInfo
{
    public int Count { set; get; }
    public IEnumerable<WebInfo> Rows { set; get; }

    public class WebInfo
    {
        public string Key { set; get; }
        public string LocalIp { set; get; }
        public int LocalPort { set; get; }
    }
}

public class ForwardListInfo
{
    public int Count { set; get; }
    public IEnumerable<ForwardInfo> Rows { set; get; }

    public class ForwardInfo
    {
        public int Key { set; get; }
        public string LocalIp { set; get; }
        public int LocalPort { set; get; }
        public int RemotePort { set; get; }
    }
}

public class ClientInfo
{
    public IList<WebInfo> WebInfos { set; get; }
    public IList<ForwardInfo<ForwardHandlerArg>> ForwardInfos { set; get; }
    public string RemoteIpAddress { set; get; }
    public string StartTime { set; get; }
}
