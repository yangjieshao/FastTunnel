// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Diagnostics.CodeAnalysis;
using FastTunnel.Api.Helper;
using FastTunnel.Core.Config;
using FastTunnel.Core.Extensions;
using Serilog;

namespace FastTunnel.Server;

public class Program
{
    [RequiresUnreferencedCode("")]
    public static void Main(string[] args)
    {
        Microsoft.Extensions.Logging.ILogger logger = null;
        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                Args = args
            });

            // Add services to the container.
            builder.Services.AddSingleton<CustomExceptionFilterAttribute>()
                            .AddSingleton<CacheHelper>()
                            .UseAkavache()
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                            .AddEndpointsApiExplorer()
                            .AddSwaggerGen()
                            .AddCors(options =>
                            {
                                options.AddPolicy("corsPolicy", policy =>
                                {
                                    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
                                        .WithExposedHeaders("Set-Token");
                                });
                            })
                            .AddControllers();

            builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console());

            builder.Configuration.AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true);
            builder.Configuration.AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true); ;

            // -------------------FastTunnel STEP1 OF 3------------------
            builder.Services.AddFastTunnelServer(builder.Configuration.GetSection("FastTunnel"));
            // -------------------FastTunnel STEP1 END-------------------

            var Configuration = builder.Configuration;
            var apioptions = Configuration.GetSection("FastTunnel").Get<DefaultServerConfig>();

            //builder.Host.UseWindowsService();

            var app = builder.Build();

            logger = app.Logger;

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("corsPolicy");
            //app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            // -------------------FastTunnel STEP2 OF 3------------------
            app.UseFastTunnelServer();
            // -------------------FastTunnel STEP2 END-------------------

            app.MapFastTunnelServer(apioptions.WebDomain);


            var mainTask = app.RunAsync();

            logger?.LogInformation("正在初始化检测有效端口池");
            var cacheHelper = app.Services.GetService<CacheHelper>();
            logger?.LogInformation("初始化检测有效端口池完毕");

            mainTask.Wait();
        }
        catch (System.Exception ex)
        {
            logger?.LogError(ex, "致命异常");
            Task.Delay(200).Wait();
            throw;
        }
        CacheHelperEx.StopAkavache();
    }
}
