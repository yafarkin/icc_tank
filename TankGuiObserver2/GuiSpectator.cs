using System;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;

using WebSocket4Net;
using SuperSocket.ClientEngine;

namespace TankGuiObserver2
{
    public class GuiObserverCore
    {
        public bool IsWebSocketOpen => _isWebSocketOpen;
        private bool _isWebSocketOpen;
        protected string _nickName;
        protected string _server;
        public string Server => _server;
        static NLog.Logger _logger;
        WebSocket web;

        /*
        GuiSpectator members        
        */
        public bool WasMapCellsUpdated { get; set; }
        public bool WasMapUpdated { get; private set; }
        public TankSettings Settings { get; set; }
        public Map Map { get; set; }
        protected DateTime _lastMapUpdate;

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