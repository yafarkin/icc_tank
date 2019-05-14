using System;
using TankClient;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;
using System.Threading.Tasks;
using System.Threading;

using WebSocket4Net;

namespace TankGuiObserver2
{
    public class Connector
    {
        Uri _serverUri;
        public Connector()
        {
            _serverUri = new Uri("ws://127.0.0.1:2000");
        }

        public bool IsServerRunnig()
        {
            using (var wc = new WebSocketProxy(_serverUri))
            {
                try
                {
                    wc.Open();
                    Task.Delay(1000);
                    return (wc.State == WebSocketState.Open);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TankGuiSpectator2.Connector.IsServerRunning {DateTime.Now:T} Исключение во время выполнения: {e}");
                }
            }

            return false;
        }
    }

    class GuiSpectator : IClientBot
    {
        public Map Map { get; set; }
        protected DateTime _lastMapUpdate;
        protected readonly CancellationToken _cancellationToken;
        protected readonly object _syncObject = new object();
        protected int _msgCount;
        protected bool _wasUpdate;

        public GuiSpectator(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
#pragma warning disable 4014
            DisplayMap();
#pragma warning restore 4014
        }

        protected async Task DisplayMap()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
                if (!_wasUpdate)
                {
                    continue;
                }

                Map map;
                lock (_syncObject)
                {
                    _wasUpdate = false;
                    map = new Map(Map, Map.InteractObjects);
                }
            }
        }
        public ServerResponse Client(int msgCount, ServerRequest request)
        {
            lock (_syncObject)
            {
                if (request.Map.Cells != null)
                {
                    Map = request.Map;
                    _lastMapUpdate = DateTime.Now;
                }
                else if (Map == null)
                {
                    return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
                }

                Map.InteractObjects = request.Map.InteractObjects;
                _msgCount = msgCount;
                _wasUpdate = true;

                return new ServerResponse { ClientCommand = ClientCommandType.None };
            }
        }
    }
}