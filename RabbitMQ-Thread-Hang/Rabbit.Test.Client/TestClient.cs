/////////////////////////////////////////////////////////////////////////////////
///         Confidential and Proprietary
///         Copyright 2024 Fresenius Kabi – All Rights Reserved
///         This software is considered a Trade Secret of Fresenius Kabi
/////////////////////////////////////////////////////////////////////////////////
using System.Net;
using System.Threading;
namespace Rabbit.Test.Client
{
    public class TestClient
    {
        Form1 _parentWindow;
        string _serialNumber;
        CancellationTokenSource _cancellationToken;
        private const string _nextCommandUrl = "/fresenius-kabi/dxt/apheresis/device/next-command";
        private const int _nextCommandTimeout = 180;
        HttpClient _httpClient;

        public string SerialNumber
        {
            get
            {
                return _serialNumber;
            }
        }
        public TestClient(Form1 parent, int insCount, string host)
        {
            _parentWindow = parent;
            _serialNumber = $"TestConsole_{insCount:0000}";
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(host),
                Timeout = TimeSpan.FromSeconds(_nextCommandTimeout)
            };
            _cancellationToken = new CancellationTokenSource();
        }

        public void CancelThreads()
        {
            try
            {
                _cancellationToken.Cancel();
                LogToConsole($"Stopped threads for {SerialNumber}");
            }
            catch (Exception ex)
            {
                LogToConsole($"Exception encountered while stopping threads for {SerialNumber}");
                LogToConsole(ex);
            }
        }

        public void CreateNextCommandThread()
        {
            Task.Run(async () =>
            {
                while (!_cancellationToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        CheckForCancellation();

                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _nextCommandUrl);
                        request.Headers.Add("FK-SourceId", _serialNumber);
                        request.Headers.ExpectContinue = true;
                        bool success = request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                        //create cancellation token for custom timeout
                        using var cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromSeconds(_nextCommandTimeout));
                        _parentWindow.UpdateInstrumentLastAction(_serialNumber, "NextCommandRequest");
                        CheckForCancellation();

                        var result = await _httpClient.SendAsync(request);
                        var content = await result.Content.ReadAsStringAsync().ConfigureAwait(true);
                        //execute next command after logging it
                        if (result.StatusCode == HttpStatusCode.OK)
                        {
                            _parentWindow.UpdateInstrumentLastAction(_serialNumber, $"NextCommandResponse '{content}'", true);
                        }
                        else
                        {
                            _parentWindow.UpdateInstrumentLastAction(_serialNumber, $"NextCommandResponse '{result.StatusCode}'", true);
                        }

                    }
                    catch (Exception e)
                    {
                        LogToConsole(e);
                    }
                }
            });
        }
        private void CheckForCancellation()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
        }

        private void LogToConsole(Exception e)
        {
            LogToConsole($"Exception encountered by Instrument {_serialNumber}{Environment.NewLine}{e}");
        }
        private void LogToConsole(string message)
        {
            _parentWindow.LogToConsole(message, _serialNumber);
        }
    }
}
