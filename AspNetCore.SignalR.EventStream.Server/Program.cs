// See https://aka.ms/new-console-template for more information
using AspNetCore.SignalR.EventStream.Server;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

Console.WriteLine("Event Stream Server");

CreateWebHostBuilder(args)
            // build the web host
            .Build()
            // and run the web host, i.e. your web application
            .Run();


static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        // create a default web host builder, with the default settings and configuration
        WebHost.CreateDefaultBuilder(args)
            // configure it to use your `Startup` class
            .UseStartup<Startup>();