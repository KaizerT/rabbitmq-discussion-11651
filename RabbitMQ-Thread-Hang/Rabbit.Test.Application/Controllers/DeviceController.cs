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
        IBus _rabbitBus;
        IOptions<NextCommandConfigurationOptions> _ncOptions;
        public DeviceController(IRabbitMQPersistentConnection rabbitMQPersistentConnection, IBus rabbitBus, IOptions<NextCommandConfigurationOptions> nextCommandConfigurationOptions)
        {
            _rabbitConnection = rabbitMQPersistentConnection ?? throw new ArgumentNullException(nameof(rabbitMQPersistentConnection));
            _rabbitBus = rabbitBus ?? throw new ArgumentNullException(nameof(rabbitBus));
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
            //IModel ncChannel = null;
            IDisposable consumer = null;
            try
            {
                int? nextCommandTimeout = _ncOptions.Value.TimeoutSeconds;

                //ncChannel = _nextCommandChannelPool.GetChannelFromPool("NextCommandLog", fkSourceId);

                //if (ncChannel == null)
                //{
                //    LoggingService.Log(LogLevel.Error, $"Channel pool max has been hit and can no longer create a new channel, current channel count {_nextCommandChannelPool.CurrentChannelCount}");
                //    httpStatus = HttpStatusCode.InternalServerError;
                //}


                string consumerTag = string.Empty;
                try
                {
                    //int consumerCount = -1;
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

                    //ncChannel.QueueDeclare(queueName, true, false, false, null);
                    //ncChannel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, true, false, null);
                    //ncChannel.QueueBind(queueName, ExchangeName, routingKey);
                    //AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(ncChannel);
                    //AsyncEventHandler<BasicDeliverEventArgs> nextCommandHandler = async (object sender, BasicDeliverEventArgs msg) =>
                    //{
                    //    try
                    //    {
                    //        _logger.Log(LogLevel.Info, $"NextCommandLog {fkSourceId}: NextCommand received from rabbitmq");
                    //        if (msg != null && msg.Body.ToArray() != null)
                    //        {
                    //            string msgBody = Encoding.UTF8.GetString(msg.Body.ToArray());
                    //            _logger.Log(LogLevel.Info, $"NextCommandLog {fkSourceId}: NextCommand body from rabbitmq{Environment.NewLine}{msgBody}");
                    //        }
                    //        else
                    //        {
                    //            _logger.Log(LogLevel.Info, $"NextCommandLog {fkSourceId}: NextCommand received from rabbitmq is null or has null body");
                    //        }
                    //        await responseChannel.Writer.WriteAsync(msg);
                    //        responseChannel.Writer.TryComplete();
                    //        await Task.Yield();
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        _logger.Log(LogLevel.Error, $"NextCommandLog {fkSourceId}: Exception occurred while consuming next command via rabbitmq {ex}");
                    //    }
                    //};
                    //consumer.Received += nextCommandHandler;

                    //bool autoAck = true;
                    //consumerTag = ncChannel.BasicConsume(queueName, autoAck, consumer);
                    //_logger.Log(LogLevel.Info, $"NextCommandLog {fkSourceId}: Current consumer tag {consumerTag}");


                    var queue = _rabbitBus.Advanced.QueueDeclare(queueName, true, false, false);
                    consumer = _rabbitBus.Advanced.Consume(queue, (body, properties, info) => Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            string msgBody = null;
                            if (body.ToArray() != null)
                            {
                                msgBody = Encoding.UTF8.GetString(body.ToArray());
                                LogToFile($"NextCommandLog {fkSourceId}: NextCommand body from rabbitmq{Environment.NewLine}{msgBody}");
                            }
                            else
                            {
                                LogToFile($"NextCommandLog {fkSourceId}: NextCommand received from rabbitmq is null or has null body");
                            }
                            responseChannel.Writer.TryWrite(msgBody);
                            responseChannel.Writer.TryComplete();
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"NextCommandLog {fkSourceId}: Exception occurred while consuming next command via rabbitmq {ex}");
                        }
                    }));

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
                                    consumer.Dispose();
                                    ////dispose consumer
                                    //consumer.Received -= nextCommandHandler;
                                    ////attempt to kill consumer. Try catch added to handle edge cases where the consumer has already been terminated
                                    //consumerCount = (int)ncChannel.ConsumerCount(queueName);
                                    //try
                                    //{
                                    //    if (ncChannel.ConsumerCount(queueName) > 0 && !string.IsNullOrEmpty(consumerTag))
                                    //    {
                                    //        ncChannel.BasicCancelNoWait(consumerTag);
                                    //    }
                                    //}
                                    //catch (Exception ex)
                                    //{
                                    //    _logger.Log(LogLevel.Error, $"NextCommandLog {fkSourceId}: ConsumerCount:{consumerCount} ConsumerTag:{consumerTag} Exception encountered while trying to dispose consumer in NextCommand endpoint right after message receipt {ex}");
                                    //}
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
                    consumer.Dispose();
                    //consumerCount = -1;
                    ////ensure consumer cancel
                    ////dispose consumer
                    //consumer.Received -= nextCommandHandler;
                    ////attempt to kill consumer. Try catch added to handle edge cases where the consumer has already been terminated
                    //consumerCount = (int)ncChannel.ConsumerCount(queueName);
                    //try
                    //{
                    //    if (ncChannel.ConsumerCount(queueName) > 0 && !string.IsNullOrEmpty(consumerTag))
                    //    {
                    //        ncChannel.BasicCancelNoWait(consumerTag);
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    _logger.Log(LogLevel.Error, $"NextCommandLog {fkSourceId}: ConsumerCount:{consumerCount} ConsumerTag:{consumerTag} Exception encountered while trying to dispose consumer in NextCommand endpoint at the end of the method {ex}");
                    //}
                }
                catch (Exception ex)
                {
                    //ncChannel.BasicCancelNoWait(consumerTag);
                    LogToFile($"NextCommandLog {fkSourceId}: Exception encountered while trying to process next command{Environment.NewLine}{ex}");
                    consumer?.Dispose();
                }
            }
            catch (Exception e)
            {
                httpStatus = HttpStatusCode.InternalServerError;
                LogToFile(e);
            }
            finally
            {
                //_nextCommandChannelPool.ReturnToPool(ncChannel);
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
