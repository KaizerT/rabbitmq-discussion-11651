/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using Rabbit.Test.Application.Models.Interfaces;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace Rabbit.Test.Application.Models
{
    public class RabbitMQChannelPool
    {
        IRabbitMQPersistentConnection _rabbitConnection;
        private static ConcurrentQueue<IChannel> _channelPool = new ConcurrentQueue<IChannel>();
        private readonly int _maxPoolRetry = 5;
        private readonly int _minPoolRetryPauseMS = 100;
        private readonly int _maxPoolRetryPauseMS = 500;
        private readonly int _maxPoolSize = 2000;
        private int _channelCounter = 0;
        private static Random generator = new Random();

        public int CurrentChannelCount
        {
            get
            {
                return _channelCounter;
            }
        }

        public RabbitMQChannelPool(RabbitMQChannelPoolConfiguration config)
        {
            _maxPoolRetry = config.MaxPoolRetry;
            _minPoolRetryPauseMS = config.MinPoolRetryPauseMS;
            _maxPoolRetryPauseMS = config.MaxPoolRetryPauseMS;
            _maxPoolSize = config.MaxPoolSize;
            _rabbitConnection = config.RabbitConnection;
        }

        private IChannel CreateChannel()
        {
            IChannel channel = null;
            if (_channelCounter <= _maxPoolSize)
            {
                channel = _rabbitConnection.CreateModel();
                Interlocked.Increment(ref _channelCounter);
            }
            return channel;
        }
        public IChannel GetChannelFromPool(string logPrefix, string clientIdentifier)
        {
            int retries = 1;
            IChannel channel = null;
            while (retries <= _maxPoolRetry)
            {
                //get channel from pool or create a new one
                if (!_channelPool.TryDequeue(out channel) && _channelPool.IsEmpty)
                {
                    channel = CreateChannel();
                }
                //if retrieved channel is closed, dispose and overwrite with a new instance
                else if (channel != null && channel.IsClosed)
                {
                    channel.Dispose();
                    channel = CreateChannel();
                }

                //break if channel was successfully dequeued/created
                if (channel != null)
                {
                    break;
                }
                int pause = generator.Next(_minPoolRetryPauseMS, _maxPoolRetryPauseMS);
                retries++;
                Task.Delay(pause).Wait();
            }
            //if all else fails retrieving from the pool
            if (channel == null)
            {
                channel = CreateChannel();
            }

            return channel;
        }
        public void ReturnToPool(IChannel channel)
        {
            if (channel != null)
            {
                _channelPool.Enqueue(channel);
            }
        }
    }

    public class RabbitMQChannelPoolConfiguration
    {
        public int MaxPoolSize { get; set; }
        public int MaxPoolRetry { get; set; }
        public int MinPoolRetryPauseMS { get; set; }
        public int MaxPoolRetryPauseMS { get; set; }
        public IRabbitMQPersistentConnection RabbitConnection { get; set; }
    }
}
