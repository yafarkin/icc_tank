using System;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;
using System.Threading.Tasks;
using System.Threading;

using WebSocket4Net;
using Fleck;
using SuperSocket.ClientEngine;
//to delete
using System.Reflection;
using System.Linq;

namespace TankGuiObserver2
{
    public class Connector : System.IDisposable
    {
        public string Server { get; private set; }
        public bool ServerRunning { get; private set; }
        public TankSettings Settings { get; set; }
        private WebSocketProxy _webSocketProxy;
        static NLog.Logger _logger;

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
                    _logger.Debug($"Исключение во время выполнения: {e}");
                }

            }
        }

        public void Dispose()
        {
            _webSocketProxy.Dispose();
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
        public bool IsWebSocketOpen => _isWebSocketOpen;
        private bool _isWebSocketOpen;
        protected string _nickName;
        protected string _server;
        public string Server => _server;
        private WebSocketServer _webSocket;
        static NLog.Logger _logger;
        WebSocket web;
        /*
        GuiSpectator members        
        */
        public TankSettings Settings { get; set; }
        public Map Map { get; set; }
        protected DateTime _lastMapUpdate;
        public bool WasUpdate { get; private set; }

        public GuiObserverCore(string server, string nickname)
        {
            _nickName = nickname;
            _logger = NLog.LogManager.GetCurrentClassLogger();
            _logger.Debug("Ctor is working fiine. [GuiObserverCore]");
            Restart(server);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void Restart(string server)
        {
            _server = server;
            web?.Close();
            web?.Dispose();
            web = new WebSocket(server);
            web.Opened += (object sender, EventArgs eventArgs) =>
            {
                _isWebSocketOpen = true;
                _logger.Debug($"Подсоединение к серверу { _server} как { _nickName}...");
                ServerResponse loginResponse = new ServerResponse
                {
                    CommandParameter = _nickName,
                    ClientCommand = ClientCommandType.Login
                };
                _logger.Debug($"Логин на сервер как {_nickName}");
                web.Send(loginResponse.ToJson());
                _logger.Debug("Логин успешно выполнен.");
            };
            web.MessageReceived += (object sender, MessageReceivedEventArgs messageReceivedEventArgs) =>
            {
                ServerRequest serverRequest = messageReceivedEventArgs.Message.FromJson<ServerRequest>();
                WasUpdate = false;
                if (serverRequest != null)
                {
                    ServerResponse response = Client(serverRequest);
                    if (response != null)
                    {
                        web.Send(response.ToJson());
                    }
                }
            };
            web.Error += (object sender, ErrorEventArgs error) =>
            {
                _logger.Info(error.Exception.Message);
            };

            web.Closed += (object sender, EventArgs eventArgs) =>
            {
                _isWebSocketOpen = false;
                _logger.Info("Socket closed");
            };
            web.Open();
        }


        public ServerResponse Client(ServerRequest request)
        {
            if (request.Settings != null)
            {
                _logger.Debug("flag: request.Settings != null");
                Settings = request.Settings;
            }

            if (request.Map.Cells != null)
            {
                _logger.Debug("flag: request.Map.Cells != null");
                Map = request.Map;
                _lastMapUpdate = DateTime.Now;
            }
            else if (Map == null)
            {
                _logger.Debug("flag: Map == null");
                return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
            }

            _logger.Debug("set: Map.InteractObjects");
            if (request.Map.InteractObjects != null)
            {
                Map.InteractObjects = request.Map.InteractObjects;
            }
            _logger.Debug("set: _wasUpdate");
            WasUpdate = true;

            return new ServerResponse { ClientCommand = ClientCommandType.None };

        }
    }

}