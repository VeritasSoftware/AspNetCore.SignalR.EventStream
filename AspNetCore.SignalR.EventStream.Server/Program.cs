// See https://aka.ms/new-console-template for more information
using AspNetCore.SignalR.EventStream.Server;
using Microsoft.AspNetCore;
using Serilog;

Console.WriteLine("Event Stream Server");

CreateHostBuilder(args)
            // build the web host
            .Build()
            // and run the web host, i.e. your web application
            .Run();


static IHostBuilder CreateHostBuilder(string[] args) =>
        // create a default web host builder, with the default settings and configuration
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            // configure it to use your `Startup` class
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });