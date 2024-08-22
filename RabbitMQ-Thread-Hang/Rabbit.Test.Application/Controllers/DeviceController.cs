/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using EasyNetQ;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rabbit.Test.Application.Models;
using Rabbit.Test.Application.Models.Interfaces;
using System;
using System.Net;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.IO;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Rabbit.Test.Application.Controllers
{
    [Route("fresenius-kabi/dxt/apheresis")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private static string _curLogPath;
        private static string _logPath;
        private string _binPath = AppDomain.CurrentDomain.BaseDirectory;
        private static IRabbitMQPersistentConnection _rabbitConnection = null;
        private static int _logFileRolloverCounter = 1;
        private static RabbitMQChannelPool _nextCommandChannelPool = null;
        IOptions<NextCommandConfigurationOptions> _ncOptions;
        public DeviceController(IRabbitMQPersistentConnection rabbitMQPersistentConnection, IOptions<NextCommandConfigurationOptions> nextCommandConfigurationOptions)
        {
            _rabbitConnection = rabbitMQPersistentConnection ?? throw new ArgumentNullException(nameof(rabbitMQPersistentConnection));
            _ncOptions = nextCommandConfigurationOptions ?? throw new ArgumentNullException(nameof(nextCommandConfigurationOptions));
            if (string.IsNullOrEmpty(_curLogPath))
            {
                _curLogPath = System.IO.Path.Combine(_binPath, "Logs");
                System.IO.Directory.CreateDirectory(_curLogPath);
            }
            if (string.IsNullOrEmpty(_logPath))
            {
                _logPath = System.IO.Path.Combine(_curLogPath, $"Log_{DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss-fff")}.log");
            }
            if (_nextCommandChannelPool == null)
            {
                var poolOptions = new RabbitMQChannelPoolConfiguration
                {
                    RabbitConnection = _rabbitConnection,
                    MaxPoolRetry = _ncOptions.Value.MaxChannelPoolRetry,
                    MinPoolRetryPauseMS = _ncOptions.Value.MinChannelPoolRetryPauseMS,
                    MaxPoolRetryPauseMS = _ncOptions.Value.MaxChannelPoolRetryPauseMS,
                    MaxPoolSize = _ncOptions.Value.MaxChannels,
                };

                _nextCommandChannelPool = new RabbitMQChannelPool(poolOptions);
            }
        }

        /// <summary>
        /// Handles the long poll created by the instrument for next command
        /// </summary>
        /// <returns></returns>
        /// <response code="200">returns an XML for the command issued by DXT for the instrument</response>
        /// <response code="204">returns Status Code No Content - timeout occurs and no command is found</response>
        /// <response code="415">Returns Error Code  Unsupported Media Type - if the content type is invalid</response>
        /// <response code="500">If an unhandled exception occurs</response>
        [Route("device/next-command")]
        [HttpGet]
        public async Task<IActionResult> NextCommand([FromHeader(Name = "Fk-SourceId")] string fkSourceId)
        {
            //the default http status code sent to the instrument when the next command times out
            HttpStatusCode nextCommandDefaultStatus = HttpStatusCode.NoContent;
            string messageType = "PutNextCommand";
            string responseBody = "";
            const string ExchangeName = "Apheresis";
            HttpStatusCode httpStatus = HttpStatusCode.OK;
            IChannel ncChannel = null;
            try
            {
                int? nextCommandTimeout = _ncOptions.Value.TimeoutSeconds;

                ncChannel = _nextCommandChannelPool.GetChannelFromPool("NextCommandLog", fkSourceId);

                if (ncChannel == null)
                {
                    LogToFile($"Channel pool max has been hit and can no longer create a new channel, current channel count {_nextCommandChannelPool.CurrentChannelCount}");
                    httpStatus = HttpStatusCode.InternalServerError;
                }


                string consumerTag = string.Empty;
                try
                {
                    int consumerCount = -1;
                    string queueName = string.Format("{0}.NextCommand.{1}", ExchangeName, fkSourceId.ToUpper());
                    string routingKey = string.Format("Apheresis.3.0.0.0.PutNextCommand.{0}", fkSourceId.ToUpper());

                    string returnValue = string.Empty;
                    bool isMessageFound = false;
                    bool isTimeout = false;
                    DateTime nextCommandCycle = DateTime.Now.AddSeconds(nextCommandTimeout.Value);

                    var channelOptions = new BoundedChannelOptions(1)
                    {
                        FullMode = BoundedChannelFullMode.Wait
                    };
                    Channel<string> responseChannel = Channel.CreateBounded<string>(channelOptions);

                    await ncChannel.QueueDeclareAsync(queueName, true, false, false, null);
                    await ncChannel.ExchangeDeclareAsync(ExchangeName, RabbitMQ.Client.ExchangeType.Topic, true, false, null);
                    await ncChannel.QueueBindAsync(queueName, ExchangeName, routingKey);
                    AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(ncChannel);
                    AsyncEventHandler<BasicDeliverEventArgs> nextCommandHandler = (object sender, BasicDeliverEventArgs msg) =>
                    {
                        try
                        {
                            string msgBody = null;
                            if (msg != null && msg.Body.ToArray() != null)
                            {
                                msgBody = Encoding.UTF8.GetString(msg.Body.ToArray());
                                LogToFile($"NextCommandLog {fkSourceId}: NextCommand body from rabbitmq{Environment.NewLine}{msgBody}");
                            }
                            responseChannel.Writer.TryWrite(msgBody);
                            responseChannel.Writer.TryComplete();
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"NextCommandLog {fkSourceId}: Exception occurred while consuming next command via rabbitmq {ex}");
                        }
                        return Task.CompletedTask;
                    };
                    consumer.Received += nextCommandHandler;

                    bool autoAck = true;
                    consumerTag = await ncChannel.BasicConsumeAsync(queueName, autoAck, consumer);


                    string message = null;
                    //combine tokens for nextcommand timeout and request cancellation
                    CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(nextCommandTimeout.Value * 1000);
                    using (CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, HttpContext.RequestAborted))
                    {
                        try
                        {
                            isMessageFound = await responseChannel.Reader.WaitToReadAsync(linkedToken.Token);
                            if (isMessageFound)
                            {
                                message = await responseChannel.Reader.ReadAsync();
                                returnValue = message;
                                if (!string.IsNullOrEmpty(returnValue))
                                {
                                    isMessageFound = true;
                                    //dispose consumer
                                    consumer.Received -= nextCommandHandler;
                                    //attempt to kill consumer. Try catch added to handle edge cases where the consumer has already been terminated
                                    consumerCount = (int)await ncChannel.ConsumerCountAsync(queueName);
                                    try
                                    {
                                        if (consumerCount > 0 && !string.IsNullOrEmpty(consumerTag))
                                        {
                                            await ncChannel.BasicCancelAsync(consumerTag);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogToFile($"NextCommandLog {fkSourceId}: ConsumerCount:{consumerCount} ConsumerTag:{consumerTag} Exception encountered while trying to dispose consumer in NextCommand endpoint right after message receipt {ex}");
                                    }
                                }
                            }
                            else
                            {
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            isMessageFound = false;
                            if (timeoutTokenSource.IsCancellationRequested)
                            {
                                isTimeout = true;
                                httpStatus = nextCommandDefaultStatus;
                            }

                        }
                    }

                    if (isMessageFound && httpStatus != nextCommandDefaultStatus)
                    {
                        if (!HttpContext.RequestAborted.IsCancellationRequested)
                        {
                            try
                            {
                                responseBody = returnValue;
                                httpStatus = HttpStatusCode.OK;
                            }
                            catch (Exception ex)
                            {
                                LogToFile(ex);
                                httpStatus = HttpStatusCode.InternalServerError;
                            }
                        }
                        else if (!isTimeout)
                        {
                        }
                    }
                    consumerCount = -1;
                    //ensure consumer cancel
                    //dispose consumer
                    consumer.Received -= nextCommandHandler;
                    //attempt to kill consumer. Try catch added to handle edge cases where the consumer has already been terminated
                    consumerCount = (int)await ncChannel.ConsumerCountAsync(queueName);
                    try
                    {
                        if (consumerCount > 0 && !string.IsNullOrEmpty(consumerTag))
                        {
                            await ncChannel.BasicCancelAsync(consumerTag);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"NextCommandLog {fkSourceId}: ConsumerCount:{consumerCount} ConsumerTag:{consumerTag} Exception encountered while trying to dispose consumer in NextCommand endpoint at the end of the method {ex}");
                    }
                }
                catch (Exception ex)
                {
                    await ncChannel.BasicCancelAsync(consumerTag);
                    LogToFile($"NextCommandLog {fkSourceId}: Exception encountered while trying to process next command{Environment.NewLine}{ex}");
                }
            }
            catch (Exception e)
            {
                httpStatus = HttpStatusCode.InternalServerError;
                LogToFile(e);
            }
            finally
            {
                _nextCommandChannelPool.ReturnToPool(ncChannel);
            }

            if (string.IsNullOrEmpty(responseBody))
            {
                return httpStatus == HttpStatusCode.OK ? StatusCode((int)httpStatus) : StatusCode((int)httpStatus, null);
            }
            else
            {
                return SetResponseWithBody(httpStatus, responseBody);
            }
        }

        /// <summary>
        /// Sets the response body as a application/octet-stream and converts the given body string to the correct data type
        /// </summary>
        /// <param name="httpStatus"></param>
        /// <param name="responseBody"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        //ignore from swagger to avoid making the documentation error out since this method is for the base class
        [ApiExplorerSettings(IgnoreApi = true)]
        protected ObjectResult SetResponseWithBody(HttpStatusCode httpStatus, string responseBody, string contentType = null)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                contentType = "application/octet-stream";
            }
            Response.Headers["Content-Type"] = contentType;
            Stream responseStream = new MemoryStream(Encoding.UTF8.GetBytes(responseBody));
            return StatusCode((int)httpStatus, responseStream);
        }
        object lockLogObject = new object();
        [ApiExplorerSettings(IgnoreApi = true)]
        protected void LogToFile(string message)
        {
            string prefix = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff UTCz");
            message = $"{prefix}: {message}";
            string logPath = _logPath;
            lock (lockLogObject)
            {
                if (System.IO.File.Exists(logPath))
                {
                    long length = new FileInfo(logPath).Length;
                    long maxLength = 100 * 1024000;
                    int maxCount = 10;
                    if (length > maxLength)
                    {
                        //if rollover already reset the counter, delete the existing one to append
                        string newPath = $"{logPath}.{_logFileRolloverCounter}";
                        if (System.IO.File.Exists(newPath))
                        {
                            long newLength = new FileInfo(newPath).Length;
                            if (newLength > maxLength)
                            {
                                System.IO.File.Delete(newPath);
                            }
                        }
                        //rename file with .x at the end to continue logging
                        System.IO.File.Move(logPath, newPath);
                        //reset counter to 1 and overwrite the oldest rolled over log
                        if (_logFileRolloverCounter == maxCount)
                        {
                            _logFileRolloverCounter = 1;
                        }
                        _logFileRolloverCounter++;
                    }
                }
                System.IO.File.AppendAllLines(logPath, new List<string> { message });
            }
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        protected void LogToFile(Exception ex)
        {
            LogToFile($"Exception encountered:{Environment.NewLine}{ex}");
        }
    }
}
