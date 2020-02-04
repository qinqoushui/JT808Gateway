﻿using JT808.Gateway.Abstractions.Enums;
using JT808.Gateway.ReplyMessage;
using JT808.Gateway.MsgLogging;
using JT808.Gateway.Traffic;
using JT808.Gateway.MsgIdHandler;
using JT808.Gateway.SessionNotice;
using JT808.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using JT808.Gateway.SimpleServer.Impl;
using JT808.Gateway.SimpleServer.Services;
using JT808.Gateway.Abstractions;
using JT808.Gateway.Transmit;
using JT808.Gateway.SimpleServer.Jobs;

namespace JT808.Gateway.SimpleServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serverHostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILoggerFactory, LoggerFactory>();
                    services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
                    //使用内存队列实现会话通知
                    services.AddSingleton<JT808SessionService>();
                    services.AddSingleton<IJT808SessionProducer, JT808SessionProducer>();
                    services.AddSingleton<IJT808SessionConsumer, JT808SessionConsumer>();
                    services.AddJT808Configure()                         
                            .AddNormalGateway(hostContext.Configuration)
                            .ReplaceNormalReplyMessageHandler<JT808NormalReplyMessageHandlerImpl>()
                            .AddMsgLogging<JT808MsgLogging>()
                            .AddTraffic()
                            .AddSessionNotice()
                            .AddTransmit(hostContext.Configuration)
                            .AddTcp()
                            .AddUdp()
                            .AddGrpc()
                            .Builder();
                    //流量统计
                    services.AddHostedService<TrafficJob>();
                });

            await serverHostBuilder.RunConsoleAsync();
        }
    }
}
