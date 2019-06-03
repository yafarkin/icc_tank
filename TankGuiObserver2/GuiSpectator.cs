/*
 TODO: WebSocketServer изменяем WebSocket
 */

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
using Fleck;

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
        protected readonly string _nickName;
        protected readonly string _server;
        private WebSocketServer _webSocket;
        static NLog.Logger _logger;
        WebSocket web;

        /*
        GuiSpectator members        
        */
        public TankSettings Settings { get; set; }
        public Map Map { get; set; }
        private object _syncObject = new object();
        protected DateTime _lastMapUpdate;
        protected readonly CancellationToken _cancellationToken;
        protected int _msgCount;
        protected bool _wasUpdate;


        [System.Runtime.CompilerServices.MethodImpl(256)]
        private void LogInfo(string info)
        {
#if LOGGED_GUI_OBSERVER_CORE
            _logger.Debug(info);
#endif
        }

        public GuiObserverCore(string server, string nickname)
        {
            _nickName = nickname;
            _server = server;
            _logger = NLog.LogManager.GetCurrentClassLogger();
            Restart(server);
            LogInfo("Ctor is working fiine. [GuiObserverCore]");
            //_webSocketProxy = new WebSocketProxy(_serverUri);
        }

        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void Restart(string server)
        {
            //_webSocket?.Close();
            _webSocket?.Dispose();
            _webSocket = new WebSocketServer(server);
        }

        public event EventHandler SocketOpened;
        public delegate void OnOpened();
        OnOpened dOnOpened;
        public void MathodSocketOpened(object sender, EventArgs eventArgs)
        {
            //
        }

        public void Run(CancellationToken cancellationToken)
        {
            LogInfo($"Подсоединение к серверу { _server} как { _nickName}...");
            try
            {
                SocketOpened += MathodSocketOpened;
                /*web.Opened += DelegateSocketOpened;*/
                //web.MessageReceived;
                //web.Error;
                //web.Closed;

                SocketOpened.Invoke(this, new EventArgs());

                _webSocket.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        _isWebSocketOpen = true;
                        LogInfo($"Подсоединение к серверу { _server} как { _nickName}...");
                        ServerResponse loginResponse = new ServerResponse
                        {
                            CommandParameter = _nickName,
                            ClientCommand = ClientCommandType.Login
                        };
                        LogInfo($"Логин на сервер как {_nickName}");
                        socket.Send(loginResponse.ToJson());
                        LogInfo("Логин успешно выполнен.");
                    };
                    socket.OnClose = () =>
                    {
                        _isWebSocketOpen = false;
                        LogInfo($"Закрыто соединение с сервером");
                        var logoutResponse = new ServerResponse
                        {
                            ClientCommand = ClientCommandType.Logout
                        };
                        var outputData = logoutResponse.ToJson();

                        try
                        {
                            socket.Send(outputData);
                        }
                        catch (Exception ex)
                        {
                            LogInfo($"catch (Exception ex) [_webSocketProxy.Send(outputData, CancellationToken.None);]");
                        }
                    };
                    socket.OnError = err =>
                    {
                       
                    };
                    socket.OnMessage = message =>
                    {
                        ServerRequest serverRequest = message.FromJson<ServerRequest>();
                        ServerResponse serverResponse = Client(serverRequest);
                        string outputData = serverResponse.ToJson();
                        socket.Send(outputData);
                    };
                });
            } 
            catch (Exception e)
            {
                LogInfo($"catch (Exception ex) [Run]");
            }
        }

        public ServerResponse Client(ServerRequest request)
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
            LogInfo("set: _wasUpdate");
            _wasUpdate = true;

            return new ServerResponse { ClientCommand = ClientCommandType.None };
            
        }

    }

}