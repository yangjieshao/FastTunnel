// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Collections.Generic;

namespace FastTunnel.Core.Models;

public class ResponseTempListInfo
{
    public int Count { set; get; }
    public IEnumerable<string> Rows { set; get; }
}

public class WebListInfo4Result
{
    public int Count { set; get; }
    public IEnumerable<WebInfo4Result> Rows { set; get; }

    public class WebInfo4Result
    {
        public string Key { set; get; }
        public string LocalIp { set; get; }
        public int LocalPort { set; get; }
    }
}

public class ForwardListInfo4Result
{
    public int Count { set; get; }
    public IEnumerable<ForwardInfo4Result> Rows { set; get; }

    public class ForwardInfo4Result
    {
        public int Key { set; get; }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        public string LocalIp { set; get; }
        public int LocalPort { set; get; }
        public int RemotePort { set; get; }
    }
}

public class ClientInfo4Result
{
    public IList<WebInfo> WebInfos { set; get; }
    public IList<ForwardInfo<ForwardHandlerArg>> ForwardInfos { set; get; }
    public string RemoteIpAddress { set; get; }
    public string StartTime { set; get; }
}
