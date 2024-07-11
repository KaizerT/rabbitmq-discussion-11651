/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2022 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using Polly;
using Polly.Retry;
using Rabbit.Test.Application.Models.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Net.Sockets;

namespace Rabbit.Test.Application.Models
{
    public class RabbitMQPersistentConnection : IRabbitMQPersistentConnection, IDisposable
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private readonly string _connectionName;
        IConnection _connection;
        bool _disposed;
        readonly object sync_root = new object();

        public RabbitMQPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 5, string connectionName = null)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _retryCount = retryCount;
            _connectionName = connectionName ?? string.Empty;
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to create IModel/Channel");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                // Dispose managed resources.
                if (_connection == null)
                {
                    _disposed = true;
                    return;
                }
                try
                {
                    if (_connection.IsOpen)
                    {
                        _connection.ConnectionShutdown -= OnConnectionShutdown;
                        _connection.CallbackException -= OnCallbackException;
                        _connection.ConnectionBlocked -= OnConnectionBlocked;
                        _connection.Close(TimeSpan.FromMilliseconds(5000));
                    }
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    //LoggingService.Log(ex);
                }
                _connection = null;
            }
            _disposed = true;
        }
        ~RabbitMQPersistentConnection()
        {
            Dispose(false);
        }

        /// <summary>
        /// Attempts to connect to rabbitmq and retries a set amount of time if a failure occurs
        /// </summary>
        /// <returns></returns>
        public bool TryConnect()
        {
            //LoggingService.Log(LogLevel.Info, "RabbitMQ Client is trying to connect");

            lock (sync_root)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        //LoggingService.Log(LogLevel.Warn, $"RabbitMQ Client for connection {_connectionName} could not connect after {time.TotalSeconds:n1}s ({ex.Message})");
                    }
                );

                policy.Execute(() =>
                {
                    if (_connection == null)
                    {
                        _connection = _connectionFactory.CreateConnection(_connectionName);
                    }
                    else
                    {
                        if(!_connection.IsOpen) 
                        {
                            _connection.ConnectionShutdown -= OnConnectionShutdown;
                            _connection.CallbackException -= OnCallbackException;
                            _connection.ConnectionBlocked -= OnConnectionBlocked;
                            _connection = null;
                            _connection = _connectionFactory.CreateConnection(_connectionName);
                        }
                    }
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    //LoggingService.Log(LogLevel.Info, $"RabbitMQ Client for connection {_connectionName} acquired a persistent connection to '{ _connection.Endpoint.HostName}' and is subscribed to failure events");

                    return true;
                }
                else
                {
                    //LoggingService.Log(LogLevel.Error, "RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }

        /// <summary>
        /// Logs and retries to connect if the connection is not disposed and the connection is blocked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            //LoggingService.Log(LogLevel.Warn, $"A RabbitMQ connection for connection {_connectionName} is shutdown-blocked. Trying to re-connect...");
            //LoggingService.Log(LogLevel.Warn, $"Connection blocked for connection {_connectionName} exception details: {e.ToString()}");

            TryConnect();
        }

        /// <summary>
        /// Logs and retries to connect if the connection is not disposed and the connection throws a callback exception
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            //LoggingService.Log(LogLevel.Warn, $"A RabbitMQ connection for connection {_connectionName} throw exception. Trying to re-connect...");
            //LoggingService.Log(LogLevel.Warn, $"Callback exception for connection {_connectionName} details: {e.Exception}");
            TryConnect();
        }


        /// <summary>
        /// Logs and retries to connect if the connection is not disposed and the connection shuts down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reason"></param>
        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            //LoggingService.Log(LogLevel.Warn, $"A RabbitMQ connection for connection {_connectionName} is on shutdown. Reason: {reason.ReplyText} Trying to re-connect...");

            TryConnect();
        }
    }
}
