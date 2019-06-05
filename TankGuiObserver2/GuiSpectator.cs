using System;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;

using WebSocket4Net;
using SuperSocket.ClientEngine;

namespace TankGuiObserver2
{
    /// <summary>
    /// Класс для соединения с сервером
    /// </summary>
    public class GuiObserverNet
    {
        public string Server => _server;
        public bool IsWebSocketOpen => _isWebSocketOpen;
        public bool WasMapCellsUpdated { get; set; }
        public bool WasMapUpdated { get; private set; }
        public TankSettings Settings { get; set; }
        public Map Map { get; set; }
        protected string _nickName;
        protected string _server;
        protected DateTime _lastMapUpdate;
        private bool _isWebSocketOpen;
        private NLog.Logger _logger;
        private WebSocket web;

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="server">
        /// IP сервера, к которому хотим подключиться
        /// </param>
        /// <param name="nickname">
        /// Наше имя на сервере, для Spectator'а всегда string.Empty ("") 
        /// </param>
        public GuiObserverNet(string server, string nickname)
        {
            _nickName = nickname;
            _logger = NLog.LogManager.GetCurrentClassLogger();
            _logger.Debug("Ctor is working fiine. [GuiObserverCore]");
            Initialize(server);
        }

        /// <summary>
        /// Инициализация GuiObserverNet
        /// </summary>
        /// <param name="server">
        /// IP сервера, к которому хотим подключиться
        /// </param>
        [System.Runtime.CompilerServices.MethodImpl(256)]
        public void Initialize(string server)
        {
            _server = server;
            web?.Close();
            web?.Dispose();
            web = new WebSocket(server);
            web.Opened += (object sender, EventArgs eventArgs) =>
            {
                _isWebSocketOpen = true;
                _logger.Debug($"Подсоединение к серверу {_server} как {_nickName}...");
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
                WasMapUpdated = false;
                ServerRequest serverRequest = messageReceivedEventArgs.Message.FromJson<ServerRequest>();
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

        /// <summary>
        /// Клиентский метод. Наш ответ серверу(ServerResponse) на его ответ нам(ServerRequest)
        /// </summary>
        /// <param name="request">
        /// Ответ полученный на запрос
        /// </param>
        /// <returns>Ответ серверу в виде запроса</returns>
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
                WasMapCellsUpdated = true;
                _lastMapUpdate = DateTime.Now;
            }
            else if (Map == null)
            {
                _logger.Debug("flag: Map == null");
                return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
            }

            _logger.Debug("set: Map.InteractObjects");
            Map.InteractObjects = request.Map.InteractObjects;
            _logger.Debug("set: _wasUpdate");
            WasMapUpdated = true;

            return new ServerResponse { ClientCommand = ClientCommandType.None };

        }
    }

}