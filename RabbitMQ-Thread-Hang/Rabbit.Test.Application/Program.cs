/////////////////////////////////////////////////////////////////////////////////
using EasyNetQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using Rabbit.Test.Application.Filters;
using Rabbit.Test.Application.Infrastructure.Services;
using Rabbit.Test.Application.Infrastructure.Services.Interfaces;
using Rabbit.Test.Application.Models;
using Rabbit.Test.Application.Models.Interfaces;
using RabbitMQ.Client;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.AddLog4Net();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rabbit.Test.Application", Version = "v1" });
    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddTransient<IComponentInformationService, ComponentInformationService>();
// Add single connection for controllers
builder.Services.AddSingleton<IRabbitMQPersistentConnection>(pc =>
{
    var section = builder.Configuration.GetSection(nameof(BrokerConnection));
    var brokerConnection = section.Get<BrokerConnection>();

    var _busConnection = brokerConnection;
    var rabbitConnectionFactory = new ConnectionFactory()
    {
        HostName = _busConnection.Host,
        UserName = _busConnection.UserName,
        Password = _busConnection.Password,
        RequestedHeartbeat = TimeSpan.FromSeconds(_busConnection.RequestedHeartbeat),
        AutomaticRecoveryEnabled = _busConnection.AutomaticRecoveryEnabled
    };

    //Create connection and start connection attempts
    var connection = new RabbitMQPersistentConnection(rabbitConnectionFactory, connectionName: "Rabbit.Test.Web.Consumer");
    if (connection.TryConnect())
        return connection;
    else
        return null;
});

//builder.Services.AddSingleton<IBus>(sp =>
//{
//    var section = builder.Configuration.GetSection(nameof(BrokerConnection));
//    var brokerConnection = section.Get<BrokerConnection>();

//    return RabbitHutch.CreateBus(brokerConnection.ToString());
//});
builder.Services.AddMvc(options =>
{
    options.Filters.Add(typeof(HttpGlobalExceptionFilter));
    options.EnableEndpointRouting = true;
});

builder.Services.AddOptions<NextCommandConfigurationOptions>().Bind(builder.Configuration.GetSection("NextCommandConfigurations"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
