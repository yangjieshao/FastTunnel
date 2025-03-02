// Licensed under the Apache License, Version 2.0 (the "License")

using FastTunnel.Core.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Filters
{
    public class FastTunnelExceptionFilter : IExceptionFilter
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<FastTunnelExceptionFilter> logger;

        public FastTunnelExceptionFilter(
            ILogger<FastTunnelExceptionFilter> logger,
            IWebHostEnvironment hostingEnvironment)
        {
            this.logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        public void OnException(ExceptionContext context)
        {
            if (!_hostingEnvironment.IsDevelopment())
            {
                return;
            }

            logger.LogError(context.Exception, "[全局异常]");
        }
    }
}
