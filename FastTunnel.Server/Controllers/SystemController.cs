// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using FastTunnel.Api.Helper;
using FastTunnel.Core.Client;
using FastTunnel.Core.Config;
using FastTunnel.Core.Models;
using FastTunnel.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FastTunnel.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[ServiceFilter(typeof(CustomExceptionFilterAttribute))]
public class SystemController : ControllerBase
{
    private readonly FastTunnelServer fastTunnelServer;

    private readonly ILogger<SystemController> _logger;

    private CacheHelper CacheHelper { get; }

    public SystemController(FastTunnelServer fastTunnelServer, CacheHelper cacheHelper
        , ILogger<SystemController> logger)
    {
        this.fastTunnelServer = fastTunnelServer;
        _logger = logger;
        CacheHelper = cacheHelper;
    }

    /// <summary>
    /// 获取当前等待响应的请求
    /// </summary>
    [HttpGet]
    public ApiResponse<ResponseTempListInfo> GetResponseTempList()
    {
        return new()
        {
            data = fastTunnelServer.GetResponseTempList()
        };
    }

    /// <summary>
    /// 获取当前映射的所有站点信息
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ApiResponse<WebListInfo4Result> GetAllWebList()
    {
        return new()
        {
            data = fastTunnelServer.GetAllWebList()
        };
    }

    ///// <summary>
    ///// 获取服务端配置信息
    ///// </summary>
    ///// <returns></returns>
    //[HttpGet]
    //public ApiResponse GetServerOption()
    //{
    //    ApiResponse.data = fastTunnelServer.ServerOption;
    //    return ApiResponse;
    //}

    /// <summary>
    /// 获取所有端口转发映射列表
    /// </summary>
    [HttpGet]
    public ApiResponse<ForwardListInfo4Result> GetAllForwardList()
    {
        return new ()
        {
             data = fastTunnelServer.GetAllForwardList()
        };
    }

    /// <summary>
    /// 获取所有端口已占用端口
    /// </summary>
    [HttpGet]
    public ApiResponse<IEnumerable<int>> GetAllUsedPort()
    {
        return new()
        {
            data = fastTunnelServer.GetAllUsedPort()
        };
    }

    /// <summary>
    /// 获取当前客户端在线数量
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ApiResponse<int> GetOnlineClientCount()
    {
        return new()
        {
            data = fastTunnelServer.ConnectedClientCount
        };
    }

    [HttpGet]
    public ApiResponse<IEnumerable<ClientInfo4Result>> Clients()
    {
        return new()
        {
            data = fastTunnelServer.GetClients()
        };
    }

    [HttpGet]
    public async Task<ApiResponse<int>> GetPort(string token)
    {
        if(string.IsNullOrWhiteSpace(token))
        {
            return new ApiResponse<int> { data = -1, code = ErrorCodeEnum.Exception,message= "token 不能未空" };
        }
        var port = await CacheHelper.GetPort(token);
        if (port <= 0)
        {
            port = await CacheHelper.CreatePort();
            if(port>=0)
            {
                await CacheHelper.SetTempPort(token, port);
            }
        }

        return new ApiResponse<int>()
        {
            data = port
        };
    }

    /// <summary>
    /// 清理g过期缓存
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse> CleanCache()
    {
        await CacheHelper.CleanInvalidateCache();
        return new();
    }
}
