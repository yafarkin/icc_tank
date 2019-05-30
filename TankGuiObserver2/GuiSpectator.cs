//#define LOGGED_CONNECTOR
#define LOGGED_GUI_SPECTATOR
#define LOGGED_GUI_OBSERVER_CORE

using System;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;
using System.Threading.Tasks;
using System.Threading;

using WebSocket4Net;

namespace TankGuiObserver2
{
    public class Connector : System.IDisposable
    {
        public string Server { get; private set; }
        public bool ServerRunning { get; private set; }
        public TankSettings Settings { get; set; }
        private WebSocketProxy _webSocketProxy;
        static NLog.Logger _logger;

        [System.Runtime.CompilerServices.MethodImpl(256)]
        private void LogInfo(string info)
        {
#if LOGGED_CONNECTOR
            _logger.Info(info);
#endif
        }

        public Connector(string server)
        {
            Server = server;
            _webSocketProxy = new WebSocketProxy(new Uri(Server));
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public void IsServerRunning()
        {
            if (_webSocketProxy.State == WebSocketState.Open)
            {
                ServerRunning = true;
            }
            else
            {
                try
                {
                    if (_webSocketProxy.State != WebSocketState.Open &&
                        _webSocketProxy.State != WebSocketState.Connecting)
                    {
                        _webSocketProxy.Open();
                        Thread.Sleep(300);
                        if (_webSocketProxy.State == WebSocketState.Open)
                        {
                            ServerRunning = true;
                            Settings = _webSocketProxy.GetMessage().FromJson<TankSettings>();
                        }
                        else
                        {
                            ServerRunning = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogInfo($"Исключение во время выполнения: {e}");
                }

            }
        }

        public void Dispose()
        {
            _webSocketProxy.Dispose();
        }
    }

    /*не зависит от socket*/
    class GuiSpectator
    {
        public TankSettings Settings { get; set; }
        public Map Map { get; set; }
        private object _syncObject = new object();
        protected DateTime _lastMapUpdate;
        protected readonly CancellationToken _cancellationToken;
        protected int _msgCount;
        protected bool _wasUpdate;

        static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        [System.Runtime.CompilerServices.MethodImpl(256)]
        private void LogInfo(string info)
        {
#if LOGGED_GUI_SPECTATOR
            _logger.Info(info);
#endif
#pragma warning disable 4014
            DisplayMap();
#pragma warning restore 4014

        }

        public void ResetSyncObject()
        {
            _syncObject = new object();
        }

        protected async Task DisplayMap()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(400);
                if (!_wasUpdate)
                {
                    LogInfo("flag: !_wasUpdate [GuiSpectator]");
                    continue;
                }

                lock (_syncObject)
                {
                    _wasUpdate = false;
                }
            }
        }

        public GuiSpectator(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }
        public ServerResponse Client(int msgCount, ServerRequest request)
        {
            lock(_syncObject)
            {
                if (request.Map.Cells != null)
                {
                    LogInfo("flag: request.Map.Cells != null");
                    Map = request.Map;
                    _lastMapUpdate = DateTime.Now;
                }
                else if (Map == null)
                {
                    LogInfo("flag: Map == null");
                    return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
                }
                if (request.Settings != null)
                {
                    LogInfo("flag: request.Settings != null");
                    Settings = request.Settings;
                }

                LogInfo("set: Map.InteractObjects");
                Map.InteractObjects = request.Map.InteractObjects;
                LogInfo("set: _msgCount");
                _msgCount = msgCount;
                LogInfo("set: _wasUpdate");
                _wasUpdate = true;

                return new ServerResponse { ClientCommand = ClientCommandType.None };
            }
        }
    }

    /// <summary>
    /// WebSocket implementation from
    /// https://www.codeproject.com/Articles/618032/Using-WebSocket-in-NET-Part
    /// </summary>
    public class WebSocketProxy : IDisposable
    {
        private Uri serverUri;
        private WebSocket ws;

        public WebSocketProxy(Uri url)
        {
            serverUri = url;

            var protocol = serverUri.Scheme;
            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
            {
                throw new ArgumentException("Unsupported protocol: " + protocol);
            }

            ws = new WebSocket(serverUri.ToString()) { ReceiveBufferSize = 1024 * 1024 };
            ws.MessageReceived += WsOnMessageReceived;
        }

        protected readonly System.Collections.Generic.Queue<string> _messages = new System.Collections.Generic.Queue<string>();
        protected readonly object _syncObject = new object();

        public int MsgCount
        {
            get
            {
                lock (_syncObject)
                {
                    return _messages.Count;
                }
            }
        }

        public string GetMessage()
        {
            lock (_syncObject)
            {
                return _messages.Count > 0 ? _messages.Dequeue() : (string)null;
            }
        }

        private void WsOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            lock (_syncObject)
            {
                _messages.Enqueue(e.Message);
            }
        }

        public void Open()
        {
            ws.Open();
        }

        public async Task SendAsync(string str, CancellationToken cancellationToken)
        {
            Send(str, cancellationToken);
            await Task.FromResult(0);
        }

        public void Send(string str, CancellationToken cancellationToken)
        {
            ws.Send(str);
        }

        public WebSocketState State => ws.State;

        public void Close()
        {
            if (State == WebSocketState.Open)
            {
                ws.Close();
            }

            ws.Dispose();
        }

        public void Dispose()
        {
            Close();
        }
    }

    public class GuiObserverCore
    {
        public WebSocketProxy WebSocketProxy => _webSocketProxy;
        protected Uri _serverUri;
        protected readonly string _nickName;

        private WebSocketProxy _webSocketProxy;
        static NLog.Logger _logger;

        [System.Runtime.CompilerServices.MethodImpl(256)]
        private void LogInfo(string info)
        {
#if LOGGED_GUI_OBSERVER_CORE
            _logger.Info(info);
#endif
        }

        public GuiObserverCore(string server, string nickname)
        {
            _nickName = nickname;
            _logger = NLog.LogManager.GetCurrentClassLogger();
            Restart(server);
            LogInfo("Ctor is working fiine. [GuiObserverCore]");
            //_webSocketProxy = new WebSocketProxy(_serverUri);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void Restart(string server)
        {
            _serverUri = new Uri(server);
            _webSocketProxy?.Dispose();
            _webSocketProxy = new WebSocketProxy(_serverUri);
        }

        public void Run(Func<int, ServerRequest, ServerResponse> bot, CancellationToken cancellationToken)
        {
            LogInfo($"Подсоединение к серверу { _serverUri} как { _nickName}...");
            try
            {
                if (_webSocketProxy.State != WebSocketState.Open &&
                    _webSocketProxy.State != WebSocketState.Connecting)
                    _webSocketProxy.Open();

                ServerResponse loginResponse = new ServerResponse
                {
                    CommandParameter = _nickName,
                    ClientCommand = ClientCommandType.Login
                };
                LogInfo($"Логин на сервер как {_nickName}");
                _webSocketProxy.Send(loginResponse.ToJson(), cancellationToken);
                LogInfo("Логин успешно выполнен.");

                CancellationTokenSource tokenSource = new CancellationTokenSource();

                while (!cancellationToken.IsCancellationRequested && 
                    _webSocketProxy.State == WebSocketState.Open)
                {
                    var inputData = _webSocketProxy.GetMessage();
                    if (string.IsNullOrWhiteSpace(inputData))
                    {
                        Thread.Sleep(10);
                        LogInfo("Run method: interupted [string.IsNullOrWhiteSpace(inputData)]");
                        continue;
                    }

                    var serverRequest = inputData.FromJson<ServerRequest>();
                    if (serverRequest?.Map == null)
                    {
                        LogInfo("Run method: interupted [serverRequest?.Map == null]");
                        continue;
                    }

                    var serverResponse = bot(_webSocketProxy.MsgCount, serverRequest);
                    var outputData = serverResponse.ToJson();

                    _webSocketProxy.Send(outputData, cancellationToken);


                }

                if (_webSocketProxy.State != WebSocketState.Open)
                {
                    LogInfo($"Закрыто соединение с сервером [_webSocketProxy.State != WebSocketState.Open {cancellationToken.IsCancellationRequested}]");
                }
                else
                {
                    LogInfo($"Запрос на прекращение работы [_webSocketProxy.State == WebSocketState.Open] {cancellationToken.IsCancellationRequested}");
                    var logoutResponse = new ServerResponse
                    {
                        ClientCommand = ClientCommandType.Logout
                    };
                    var outputData = logoutResponse.ToJson();

                    try
                    {
                        _webSocketProxy.Send(outputData, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        LogInfo($"catch (Exception ex) [_webSocketProxy.Send(outputData, CancellationToken.None);]");
                    }

                    Thread.Sleep(1000);
                }

                tokenSource.Cancel();
                LogInfo("call: tokenSource.Cancel()");
            }
            catch (Exception e)
            {
                LogInfo($"catch (Exception ex) [Run]");
            }
        }

    }

}