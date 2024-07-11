/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2022 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using RabbitMQ.Client;
using System;

namespace Rabbit.Test.Application.Models.Interfaces
{
    public interface IRabbitMQPersistentConnection : IDisposable
    {
        bool IsConnected { get; }

        bool TryConnect();

        IModel CreateModel();

    }
}
