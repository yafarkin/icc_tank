using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;
using NLog;
using System.Text.RegularExpressions;

namespace TankServer
{
    public class Server : IDisposable
    {
        protected readonly Random _random;
        protected readonly WebSocketServer _socketServer;

        protected TankSettings defaultTankSettings;

        protected readonly object _syncObject = new object();
        protected DateTime _lastCoreUpdate;
        
        public readonly ServerSettings serverSettings;
        
        public readonly Map Map;
        public Dictionary<IWebSocketConnection, ClientInfo> Clients;

        protected readonly Logger _logger;
        public delegate void LoggerDelegate(string log, LoggerType loggerType);
        LoggerDelegate LoggerDelegates;

        public Server(ServerSettings sSettings, Logger logger)
        {            
            Clients = new Dictionary<IWebSocketConnection, ClientInfo>();
            serverSettings = sSettings;
            defaultTankSettings = sSettings.TankSettings;
            var mapType = serverSettings.MapType;

            LoggerDelegates += DelegateMethod;

            switch (mapType)
            {
                case MapType.Default_map:
                    Map = MapManager.LoadMap(serverSettings.Height, serverSettings.Width, CellMapType.Wall, 50, 50);
                    break;
                case MapType.Empty_map:
                    Map = MapManager.LoadMap(serverSettings.Height, serverSettings.Width, CellMapType.Wall, 0, 0);
                    break;
                case MapType.Manual_Map_1:
                    Map = MapManager.ReadMap(mapType);
                    break;
                case MapType.Manual_Map_2:
                    Map = MapManager.ReadMap(mapType);
                    break;
                case MapType.Promotional:
                    Map = MapManager.ReadMap(mapType);
                    break;
                case MapType.Water_map:
                    Map = MapManager.LoadMap(serverSettings.Height, serverSettings.Width, CellMapType.Water, 40, 0);
                    break;
                default:
                    throw new Exception("Неизвестный тип карты");
            }

            _random = new Random();
            _logger = logger;

            _socketServer = new WebSocketServer($"ws://0.0.0.0:{serverSettings.Port}");
            _socketServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    LoggerDelegates($"[КЛИЕНТ+]: {socket.ConnectionInfo.ClientIpAddress}", LoggerType.Info);
                };
                socket.OnClose = () =>
                {
                    lock (_syncObject)
                    {
                        LoggerDelegates($"[КЛИЕНТ-]: {socket.ConnectionInfo.ClientIpAddress}", LoggerType.Info);
                        if (Clients.ContainsKey(socket))
                        {
                            Clients[socket].IsConnected = false;
                            if (!(Clients[socket].InteractObject is TankObject tank))
                            {
                                Clients[socket].NeedRemove = true;
                            }
                            else
                            {
                                tank.IsDead = true;
                            }
                        }

                        var firstInQueue = Clients.Values.FirstOrDefault(c => c.IsInQueue);
                        if (firstInQueue != null)
                        {
                            var bot = AddTankBot(firstInQueue.Nickname, firstInQueue.Tag);
                            firstInQueue.IsInQueue = false;
                            firstInQueue.InteractObject = bot;
                        }
                    }
                };
                socket.OnError = err =>
                {
                    lock (_syncObject)
                    {
                        LoggerDelegates($"[КЛИЕНТ-]: {socket.ConnectionInfo.ClientIpAddress}", LoggerType.Info);
                        if (Clients.ContainsKey(socket))
                        {
                            Clients[socket].IsConnected = false;
                            if (!(Clients[socket].InteractObject is TankObject tank))
                            {
                                Clients[socket].NeedRemove = true;
                            }
                            else
                            {
                                tank.IsDead = true;
                            }
                        }

                        var firstInQueue = Clients.Values.FirstOrDefault(c => c.IsInQueue);
                        if (firstInQueue != null)
                        {
                            var bot = AddTankBot(firstInQueue.Nickname, firstInQueue.Tag);
                            firstInQueue.IsInQueue = false;
                            firstInQueue.InteractObject = bot;
                        }
                    }
                };
                socket.OnMessage = msg =>
                {
                    var response = msg.FromJson<ServerResponse>();
                    if (response == null) return;
                    if (response.ClientCommand == null)
                    {
                        response.ClientCommand = ClientCommandType.Login;
                    }

                    lock (_syncObject)
                    {
                        if (response.ClientCommand == ClientCommandType.Logout)
                        {
                            try
                            {
                                var client = Clients.FirstOrDefault(x => x.Key == socket);
                                if (client.Key != null)
                                {
                                    client.Value.NeedRemove = true;
                                }
                            }
                            catch(Exception ex)
                            {
                                LoggerDelegates(ex.Message, LoggerType.Error);
                            }
                        }
                        else if (response.ClientCommand == ClientCommandType.Login)
                        {
                            if (!string.IsNullOrWhiteSpace(response.CommandParameter) && !serverSettings.IsMultipleConnectionAllow && Clients.Count(x => x.Key.ConnectionInfo.ClientIpAddress == socket.ConnectionInfo.ClientIpAddress && !string.IsNullOrWhiteSpace(x.Value.Nickname)) > 0)
                            {
                                return;
                            }
                            if (Clients.ContainsKey(socket))
                            {
                                return;
                            }

                            var request = new ServerRequest
                            {
                                Settings = defaultTankSettings,
                                IsSettingsChanged = true

                            };
                            var info = new ClientInfo { Request = request };

                            LoggerDelegates($"Вход на сервер: {(string.IsNullOrWhiteSpace(response.CommandParameter) ? "наблюдатель" : response.CommandParameter)}", LoggerType.Info);

                            info.IsLogined = true;
                            info.NeedUpdateMap = true;

                            //если настройки изменились, клиенту отправится новая версия и флаг об обновлении настроек
                            info.Request = new ServerRequest { Map = Map, IsSettingsChanged = true, Settings = defaultTankSettings };

                            socket.Send(info.Request.ToJson());

                            if (string.IsNullOrWhiteSpace(response.CommandParameter) || Clients.Count(x => !x.Value.IsSpecator) == serverSettings.MaxClientCount || Clients.Values.Count(x => x.Nickname == GetCorrectNickname(response)) > 0)
                            {
                                var spectator = AddSpectator();
                                info.IsSpecator = true;
                                info.InteractObject = spectator;
                            }
                            else
                            {
                                var nickname = GetCorrectNickname(response);
                                var tag = string.Empty;
                                var idx = nickname.IndexOf('\t');
                                if (idx > 0)
                                {
                                    tag = nickname.Substring(idx + 1);
                                    nickname = nickname.Substring(0, idx);
                                }

                                info.Nickname = nickname;
                                info.Tag = tag;
                                var bot = AddTankBot(nickname, tag);
                                info.InteractObject = bot;
                            }

                            Clients.Add(socket, info);
                        }
                        else if (Clients.ContainsKey(socket))
                        {
                            //Информация для клиента
                            var clientInfo = Clients[socket];

                            // ответ в этом цикле уже был получен, не обрабатываем до следующего цикла
                            if (clientInfo.Response != null)
                            {
                                return;
                            }

                            clientInfo.Response = response;
                        }

                        if (response.ClientCommand != ClientCommandType.None)
                        {
                            LoggerDelegates($"[КЛИЕНТ]: ответ от {socket.ConnectionInfo.ClientIpAddress} = {response.ClientCommand}", LoggerType.Info);
                        }
                    }
                };
            });
        }

        public void Dispose()
        {
            foreach (var kv in Clients)
            {
                kv.Key.Close();
            }

            _socketServer.Dispose();
        }

        public BaseInteractObject AddSpectator()
        {
            var spectator = new SpectatorObject(Guid.NewGuid());
            Map.InteractObjects.Add(spectator);

            return spectator;
        }

        public BaseInteractObject AddTankBot(string nickname, string tag)
        {
            var rectangle = PastOnPassablePlace();
            var tank = new TankObject(Guid.NewGuid(), rectangle, defaultTankSettings.TankSpeed * defaultTankSettings.GameSpeed, false, defaultTankSettings.TankMaxHP, defaultTankSettings.TankMaxHP, 
                serverSettings.CountOfLifes, serverSettings.CountOfLifes, nickname, tag, defaultTankSettings.TankDamage, defaultTankSettings.BulletSpeed * defaultTankSettings.GameSpeed);
            //При создании нового танка он бессмертен (передаём параметр длительности в миллисекундах)
            CallInvulnerability(tank, defaultTankSettings.TimeOfInvulnerability);
            Map.InteractObjects.Add(tank);

            return tank;
        }

        private void AddBullet(TankObject clientTank)
        {
            if (Map.InteractObjects.OfType<BulletObject>().Any(b => b.SourceId == clientTank.Id))
            {
                return;
            }

            var location = new Point(clientTank.Rectangle.LeftCorner);
            switch (clientTank.Direction)
            {
                case DirectionType.Right:
                    location.Left += clientTank.Rectangle.Width;
                    location.Top += clientTank.Rectangle.Height / 2;
                    break;
                case DirectionType.Left:
                    location.Left -= 1;
                    location.Top += clientTank.Rectangle.Height / 2;
                    break;
                case DirectionType.Up:
                    location.Left += clientTank.Rectangle.Width / 2;
                    location.Top -= 1;
                    break;
                case DirectionType.Down:
                    location.Left += clientTank.Rectangle.Width / 2;
                    location.Top += clientTank.Rectangle.Height;
                    break;
            }

            var bullet = new BulletObject(Guid.NewGuid(), new Rectangle(location, 1, 1), clientTank.BulletSpeed, 
                true, clientTank.Direction, clientTank.Id, clientTank.Damage);

            Map.InteractObjects.Add(bullet);
        }

        public async Task Run(CancellationToken cancellationToken)
        {
            try
            {
                // запускаем фоновую задачу на изменение игровой карты
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                UpdateEngine(cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                while (true)
                {
                    // сбрасываем данные для клиентов и от клиентов
                    ResetClientsData();

                    // высылаем всем состояние движка
                    await SendUpdates(false);

                    if (defaultTankSettings.FinishSesison <= DateTime.Now && cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    else if (defaultTankSettings.StartSesison > DateTime.Now)
                    {
                        await Task.Delay(1000);
                        continue;
                    }
                    else
                    {
                        // в более частом цикле высылаем состояние движка для наблюдателей
                        var botTimer = serverSettings.PlayerTickRate;
                        while (botTimer > 0 && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(serverSettings.SpectatorTickRate);
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            botTimer -= serverSettings.SpectatorTickRate;
                            await SendUpdates(true);
                        }

                        // обрабатываем команды от клиентов
                        ApplyClientsData();
                    }

                    // удаляем ненужных клиентов
                    RemoveClients();
                }
            }
            catch (Exception e)
            {
                LoggerDelegates(e.Message, LoggerType.Error);
            }
        }

        private void RemoveClients()
        {
            lock (_syncObject)
            {
                var socketsToRemove = new List<IWebSocketConnection>();
                foreach (var clientInfo in Clients)
                {
                    if (!clientInfo.Value.NeedRemove)
                    {
                        continue;
                    }

                    if (clientInfo.Value.InteractObject != null)
                    {
                        var objToRemove = Map.InteractObjects.FirstOrDefault(o => o.Id == clientInfo.Value.InteractObject.Id);
                        if (objToRemove is TankObject objTank)
                        {
                            objTank.IsMoving = false;
                            objTank.IsDead = true;
                        }
                        else
                        {
                            socketsToRemove.Add(clientInfo.Key);
                        }
                    }
                }

                foreach (var connection in socketsToRemove)
                {
                    connection.Close();
                    Clients.Remove(connection);
                }
            }
        }

        protected void ResetClientsData()
        {
            lock (_syncObject)
            {
                foreach (var kv in Clients)
                {
                    kv.Value.Request = null;
                    kv.Value.Response = null;
                }
            }
        }

        protected void ApplyClientsData()
        {
            lock (_syncObject)
            {
                foreach (var client in Clients)
                {
                    if (!client.Value.IsLogined || client.Value.IsInQueue)
                    {
                        continue;
                    }

                    var response = client.Value.Response;
                    if (null == response || response.ClientCommand == ClientCommandType.None)
                    {
                        continue;
                    }

                    var clientTank = Map.InteractObjects.OfType<TankObject>().FirstOrDefault(t => t.Id == client.Value.InteractObject.Id);
                    if (null == clientTank && !client.Value.IsSpecator)
                    {
                        continue;
                    }

                    switch (response.ClientCommand)
                    {
                        case ClientCommandType.Logout:
                            client.Value.NeedRemove = true;
                            break;
                        case ClientCommandType.UpdateMap:
                            client.Value.NeedUpdateMap = true;
                            break;
                    }

                    if (client.Value.IsSpecator || ((client.Value.InteractObject as TankObject)?.IsDead ?? true))
                    {
                        continue;
                    }

                    switch (response.ClientCommand)
                    {
                        case ClientCommandType.Stop:
                            clientTank.IsMoving = false;
                            break;
                        case ClientCommandType.Go:
                            clientTank.IsMoving = true;
                            break;
                        case ClientCommandType.TurnLeft:
                            clientTank.Direction = DirectionType.Left;
                            break;
                        case ClientCommandType.TurnRight:
                            clientTank.Direction = DirectionType.Right;
                            break;
                        case ClientCommandType.TurnUp:
                            clientTank.Direction = DirectionType.Up;
                            break;
                        case ClientCommandType.TurnDown:
                            clientTank.Direction = DirectionType.Down;
                            break;
                        case ClientCommandType.Fire:
                            AddBullet(clientTank);
                            break;
                    }
                }

                ResetClientsData();
            }
        }


        protected async Task SendUpdates(bool onlySpectators)
        {
            List<BaseInteractObject> visibleObjects;
            List<BaseInteractObject> allObjects;
            Map clientMap;

            Dictionary<IWebSocketConnection, ClientInfo> clientsCopy;

            lock (_syncObject)
            {
                //Лист видимых объектов
                visibleObjects = new List<BaseInteractObject>(Map.InteractObjects.Count);
                allObjects = new List<BaseInteractObject>(Map.InteractObjects.Count);

                //Для всех интерактивных объектов
                foreach (var interactObj in Map.InteractObjects.OfType<BaseInteractObject>())
                {
                    //Если этот объект это наблюдатель
                    if (interactObj is SpectatorObject)
                    {
                        continue;
                    }
                    
                    //Если интерактивный объект это танк или апгрейд
                    var cells = MapManager.WhatOnMap(interactObj.Rectangle, Map);
                    if (!((interactObj as TankObject)?.IsDead ?? false))
                    {
                        if (cells.Any(c => c.Value != CellMapType.Grass))
                        {
                            visibleObjects.Add(interactObj);
                        }
                    }
                    allObjects.Add(interactObj);
                }

                clientsCopy = new Dictionary<IWebSocketConnection, ClientInfo>(Clients);
            }

            var emptyMap = new Map(null, visibleObjects);

            // Для всёх игроков
            foreach (var client in clientsCopy)
            {
                //Если только смотрящие
                if ((onlySpectators && !client.Value.IsSpecator) || !client.Value.IsLogined || client.Value.IsInQueue)
                {
                    continue;
                }

                // запоминаем значение, требовалось высылать ли обновление карты
                var needUpdate = client.Value.NeedUpdateMap;
                var needSettings = client.Value.NeedUpdateSettings;

                clientMap = emptyMap;
                if (needUpdate)
                {
                    lock (_syncObject)
                    {
                        clientMap.MapWidth = Map.MapWidth;
                        clientMap.MapHeight = Map.MapHeight;
                        clientMap.Cells = Map.Cells;
                    }
                }

                //если настройки изменились, клиенту отправится новая версия и флаг об обновлении настроек
                var request = new ServerRequest
                {
                    Map = clientMap, Tank = client.Value.InteractObject as TankObject,
                    IsSettingsChanged = needSettings,
                    Settings = needSettings ? defaultTankSettings : null
                };

                request.Map.InteractObjects = client.Value.IsSpecator ? allObjects : visibleObjects;

                var json = request.ToJson();

                try
                {
                    if (client.Value.IsConnected)
                    {
                        await client.Key.Send(json);
                    }
                    if (needUpdate)
                    {
                        LoggerDelegates($"Передача полной карты для {client.Key.ConnectionInfo.ClientIpAddress} / {(client.Value.IsSpecator ? "наблюдатель" : client.Value.Nickname)}", LoggerType.Info);
                    }
                }
                catch (Exception e)
                {
                    LoggerDelegates(e.Message, LoggerType.Error);
                }

                if (needUpdate)
                {
                    // сбрасываем значение, что требовалось ли высылать карту
                    // только в том случае, если мы высылали, т.к. в момент отсылки флаг мог быть сброшен,
                    // но пока шла отсылка - движок мог потребовать обновить карты
                    client.Value.NeedUpdateMap = false;
                }

                if (needSettings)
                {
                    // сброс флага об обновлении настроек
                    client.Value.NeedUpdateSettings = false;
                }
            }
        }

        public async Task UpdateEngine(CancellationToken cancellationToken)
        {
            var reincarnationArr = new List<TankObject>();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UpdateSettings();
                    await Task.Delay(serverSettings.ServerTickRate);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (defaultTankSettings.StartSesison > DateTime.Now)
                    {
                        await Task.Delay(serverSettings.ServerTickRate > 1000
                            ? serverSettings.ServerTickRate
                            : 1000 - serverSettings.ServerTickRate);
                        continue;
                    }
                    else if(defaultTankSettings.FinishSesison <= DateTime.Now)
                    {
                        break;
                    }

                    if (_lastCoreUpdate == default(DateTime))
                    {
                        _lastCoreUpdate = DateTime.Now;
                        continue;
                    }

                    var tsDelta = DateTime.Now - _lastCoreUpdate;
                    _lastCoreUpdate = DateTime.Now;
                    var delta = Convert.ToDecimal(tsDelta.TotalSeconds);

                    lock (_syncObject)
                    {
                        AddUpgrades();

                        var objsToRemove = new List<BaseInteractObject>();
                        foreach (var clientInfo in Clients)
                        {
                            if (clientInfo.Value.NeedRemove && clientInfo.Value.InteractObject != null)
                            {
                                var objToRemove = 
                                    Map.InteractObjects.FirstOrDefault(x => x.Id == clientInfo.Value.InteractObject.Id);
                                if (objToRemove is TankObject objTank)
                                {
                                    objTank.IsMoving = false;
                                    objTank.IsDead = true;
                                }
                                else
                                {
                                    objsToRemove.Add(objToRemove);
                                }
                            }
                        }

                        var upgradeItem = Map.InteractObjects.OfType<UpgradeInteractObject>()
                            .FirstOrDefault(t => t.DespawnTime < DateTime.Now);

                        if (upgradeItem != null)
                            objsToRemove.Add(upgradeItem);

                        foreach (var movingObject in Map.InteractObjects.OfType<BaseMovingObject>())
                        {
                            if (!movingObject.IsMoving)
                            {
                                continue;
                            }

                            var newPoint = new Point(movingObject.Rectangle.LeftCorner);
                            var newRectangle = new Rectangle(newPoint, movingObject.Rectangle.Width,
                                movingObject.Rectangle.Height);

                            var speed = movingObject.Speed * delta;
                            var shift = speed > 1 ? 1 : speed;

                            while (speed >= 0)
                            {

                                var canMove = newPoint.Left >= 0 &&
                                              newPoint.Left < Map.MapWidth - Constants.CellWidth && newPoint.Top >= 0 &&
                                              newPoint.Top < Map.MapHeight - Constants.CellHeight;

                                if (canMove)
                                {
                                    var intersectedObject = MapManager.GetIntersectedObject(newRectangle,
                                        Map.InteractObjects.Where(o => o.Id != movingObject.Id));
                                    var cells = MapManager.WhatOnMap(newRectangle, Map);

                                    //Если двигающийся объект - это пуля
                                    if (movingObject is BulletObject bulletObject)
                                    {
                                        if (cells.Any(c => c.Value == CellMapType.Wall))
                                        {
                                            objsToRemove.Add(bulletObject);
                                            canMove = false;
                                        }

                                        var destructiveWalls = cells.Where(c => c.Value == CellMapType.DestructiveWall)
                                            .ToList();
                                        if (destructiveWalls.Count > 0)
                                        {
                                            foreach (var destructiveWall in destructiveWalls)
                                            {
                                                Map.Cells[destructiveWall.Key.TopInt, destructiveWall.Key.LeftInt] =
                                                    CellMapType.Void;
                                            }

                                            //удаляем пулю
                                            objsToRemove.Add(bulletObject);

                                            // т.к. изменилась карта, то надо всем клиентам выслать новую карту
                                            foreach (var clientInfo in Clients)
                                            {
                                                clientInfo.Value.NeedUpdateMap = true;
                                            }

                                            canMove = false;
                                        }

                                        //Если пуля попала в танк
                                        if (intersectedObject is TankObject tankIntersectedObject)
                                        {
                                            if (tankIntersectedObject.IsInvulnerable == false)
                                            {
                                                //Удалить пулю
                                                objsToRemove.Add(bulletObject);
                                                canMove = false;

                                                //Если здоровья больше, чем урон пули
                                                var hpToRemove = tankIntersectedObject.Hp > bulletObject.DamageHp
                                                    ? bulletObject.DamageHp
                                                    : tankIntersectedObject.Hp;
                                                bool isFrag = false;

                                                //Уменьшить здоровье танка на урон пули
                                                tankIntersectedObject.Hp -= hpToRemove;

                                                //Если здоровье танка меньше нуля и у него ещё есть жизни
                                                if (tankIntersectedObject.Hp <= 0)
                                                {
                                                    var bullet = Map.InteractObjects.FirstOrDefault(x => (x as BulletObject)?.SourceId == tankIntersectedObject.Id);
                                                    if (bullet != null)
                                                    {
                                                        objsToRemove.Add(bullet);
                                                    }

                                                    if (tankIntersectedObject.Lives != 1)
                                                    {
                                                        Reborn(tankIntersectedObject);
                                                    }
                                                    else
                                                    {
                                                        objsToRemove.Add(tankIntersectedObject);
                                                    }

                                                    break;
                                                }

                                                var sourceTank = Map.InteractObjects.OfType<TankObject>()
                                                    .FirstOrDefault(t => t.Id == bulletObject.SourceId);
                                                if (sourceTank != null)
                                                {
                                                    sourceTank.Score += hpToRemove;
                                                    if (isFrag)
                                                    {
                                                        sourceTank.Score += 50;
                                                        LoggerDelegates($"{tankIntersectedObject.Nickname} was killed by {sourceTank.Nickname}", LoggerType.Info);
                                                    }
                                                }
                                            }
                                        }
                                        else if (intersectedObject is BulletObject bulletIntersectedObject)
                                        {
                                            objsToRemove.Add(bulletObject);
                                            objsToRemove.Add(bulletIntersectedObject);
                                        }
                                    }
                                    else if (movingObject is TankObject tankObject)
                                    {
                                        if (cells.Any(c =>
                                            c.Value == CellMapType.DestructiveWall || c.Value == CellMapType.Wall))
                                        {
                                            canMove = false;
                                        }
                                        else if ((decimal)cells.Count(c => c.Value == CellMapType.Water) / cells.Count >= 0.5m)
                                        {
                                            var bullet = Map.InteractObjects.FirstOrDefault(x => (x as BulletObject)?.SourceId == tankObject.Id);
                                            if (bullet != null)
                                            {
                                                objsToRemove.Add(bullet);
                                            }

                                            if (tankObject.Lives != 1)
                                            {
                                                Reborn(tankObject);
                                            }
                                            else
                                            {
                                                CallAbsoluteDeath(ref tankObject);
                                                canMove = false;
                                            }

                                            break;
                                        }

                                        if (intersectedObject is UpgradeInteractObject upgradeObject)
                                        {
                                            var tank = tankObject;

                                            // Применяем эффект улудшения на танк время указывается в секундах
                                            SetUpgrade(tank, upgradeObject);

                                            objsToRemove.Add(intersectedObject);
                                        }
                                        if (intersectedObject is TankObject && (intersectedObject as TankObject).IsDead)
                                        {

                                        }
                                        if (canMove && (intersectedObject == null))
                                        {
                                            canMove = true;
                                        }
                                        else
                                        {
                                            if (intersectedObject is TankObject && (intersectedObject as TankObject).IsDead)
                                            {
                                                canMove = true;
                                            }
                                            else
                                            {
                                                canMove = false;
                                            }
                                        }
                                    }
                                }

                                if (canMove)
                                {
                                    movingObject.Rectangle = new Rectangle(newRectangle);
                                    Rectangle rec;
                                    List<KeyValuePair<Point, CellMapType>> cells;

                                    switch (movingObject.Direction)
                                    {
                                        case DirectionType.Left:
                                            newPoint.Left -= shift;
                                            break;
                                        case DirectionType.Right:
                                            if (movingObject is TankObject)
                                            {
                                                rec = new Rectangle()
                                                {
                                                    Height = 5, Width = 1,
                                                    LeftCorner = new Point
                                                    {
                                                        Left = (int) newPoint.Left + movingObject.Rectangle.Width,
                                                        Top = newPoint.Top
                                                    }
                                                };
                                                cells = MapManager.WhatOnMap(rec, Map);
                                                if (cells.Any(c =>
                                                    c.Value == CellMapType.DestructiveWall ||
                                                    c.Value == CellMapType.Wall))
                                                    break;
                                            }

                                            newPoint.Left += shift;
                                            break;
                                        case DirectionType.Up:
                                            newPoint.Top -= shift;
                                            break;
                                        case DirectionType.Down:
                                            if (movingObject is TankObject)
                                            {
                                                rec = new Rectangle()
                                                {
                                                    Height = 1, Width = 5,
                                                    LeftCorner = new Point
                                                    {
                                                        Left = newPoint.Left,
                                                        Top = newPoint.Top + movingObject.Rectangle.Height
                                                    }
                                                };
                                                cells = MapManager.WhatOnMap(rec, Map);
                                                if (cells.Any(c =>
                                                    c.Value == CellMapType.DestructiveWall ||
                                                    c.Value == CellMapType.Wall))
                                                    break;
                                            }

                                            newPoint.Top += shift;
                                            break;
                                    }

                                    speed = speed - shift;
                                }
                                else
                                {
                                    if (movingObject is BulletObject)
                                    {
                                        objsToRemove.Add(movingObject);
                                    }

                                    break;
                                }
                            }
                        }

                        foreach (var objToRemove in objsToRemove)
                        {
                            //Если ссылка на удаляемый объект не ссылается на нулевой объект и айди объекта == айди удаляемого объекта
                            var client = Clients.FirstOrDefault(c =>
                                c.Value.InteractObject != null && c.Value.InteractObject.Id == objToRemove.Id);
                            if (client.Key != null)
                            {
                                client.Value.NeedRemove = true;
                            }
                            else
                            {
                                //Удаляем объект
                                Map.InteractObjects.Remove(objToRemove);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LoggerDelegates(e.Message, LoggerType.Error);
                }
            }
        }

        private void CallAbsoluteDeath(ref TankObject tankObject)
        {
            tankObject.IsDead = true;
            tankObject.IsInvulnerable = true;

        }

        private void Reborn(TankObject tank, int normalHP = 100)
        {
            //уменьшаем жизни
            if (tank.Lives > 1)
            {
                tank.Lives--;
            }

            //Делаем танку здоровье нормальным(не увеличенным)
            tank.Hp = normalHP;
            tank.Rectangle = PastOnPassablePlace();

            CallInvulnerability(tank, defaultTankSettings.TimeOfInvulnerability);
        }

        private Rectangle PastOnPassablePlace()
        {
            Rectangle rectangle;

            while (true)
            {
                var left = _random.Next(Map.MapWidth - Map.CellWidth);
                var top = _random.Next(Map.MapHeight - Map.CellHeight);

                rectangle = new Rectangle(new Point(left, top), Map.CellWidth, Map.CellHeight);

                if (MapManager.GetIntersectedObject(rectangle, Map.InteractObjects) != null)
                {
                    continue;
                }

                var cellTypes = MapManager.WhatOnMap(rectangle, Map);
                if (cellTypes.Any(ct => ct.Value != CellMapType.Void && ct.Value != CellMapType.Grass))
                {
                    continue;
                }

                break;
            }

            return rectangle;
        }

        protected void AddUpgrades()
        {
            var rnd = _random.NextDouble();
            if (rnd >= defaultTankSettings.ChanceSpawnUpgrades)
            {
                return;
            }

            if (Map.InteractObjects.OfType<UpgradeInteractObject>().Count() >= serverSettings.MaxCountOfUpgrade)
            {
                return;
            }

            var rectangle = PastOnPassablePlace();
            var upgradeObj = GetUpgradeToCreate(rectangle);
            Map.InteractObjects.Add(upgradeObj);
        }

        protected UpgradeInteractObject GetUpgradeToCreate(Rectangle rectangle)
        {
            var rnd = _random.Next(1, Enum.GetValues(typeof(UpgradeType)).Length);
            var objId = Guid.NewGuid();
            UpgradeInteractObject result;

            switch (rnd)
            {
                case 1:
                    result = new BulletSpeedUpgradeObject(objId, rectangle, defaultTankSettings.IncreaseBulletSpeed, serverSettings.SecondsToDespawn);
                    break;
                case 2:
                    result = new DamageUpgradeObject(objId, rectangle, defaultTankSettings.IncreaseDamage, serverSettings.SecondsToDespawn);
                    break;
                case 3:
                    result = new HealthUpgradeObject(objId, rectangle, defaultTankSettings.RestHP, serverSettings.SecondsToDespawn);
                    break;
                case 4:
                    result = new MaxHpUpgradeObject(objId, rectangle, defaultTankSettings.IncreaseHP, serverSettings.SecondsToDespawn);
                    break;
                case 5:
                    result = new SpeedUpgradeObject(objId, rectangle, defaultTankSettings.IncreaseSpeed, serverSettings.SecondsToDespawn);
                    break;
                case 6:
                    result = new InvulnerabilityUpgradeObject(objId, rectangle, defaultTankSettings.TimeOfInvulnerability, serverSettings.SecondsToDespawn);
                    break;
                default:
                    throw new NotSupportedException("Incorrect object");
            }

            return result;
        }

        //в параметре передаётся танк и длительность неуязвимости в миллисекундах
        protected async void CallInvulnerability(TankObject tank, int sec)
        {
            await Task.Run(() => Invulnerability(tank, sec));
        }

        protected void Invulnerability(TankObject tank, int sec)
        {
            tank.IsInvulnerable = true;
            Thread.Sleep(sec);
            tank.IsInvulnerable = false;
        }

        private async void SetUpgrade(TankObject tank, UpgradeInteractObject upgradeObj)
        {
            var time = defaultTankSettings.TimeOfActionUpgrades;

            switch (upgradeObj.Type)
            {
                case UpgradeType.BulletSpeed:
                {
                    var upgrade = upgradeObj as BulletSpeedUpgradeObject;
                    await Task.Run((() =>
                    {
                        tank.BulletSpeed += upgrade.IncreaseBulletSpeed;
                        Thread.Sleep(time);
                        tank.BulletSpeed -= upgrade.IncreaseBulletSpeed;
                    }));

                    break;
                }
                case UpgradeType.Damage:
                {
                    var upgrade = upgradeObj as DamageUpgradeObject;
                    await Task.Run((() =>
                    {
                        tank.Damage += upgrade.IncreaseDamage;
                        Thread.Sleep(time);
                        tank.Damage -= upgrade.IncreaseDamage;
                    }));
                        
                    break;
                }
                case UpgradeType.Health:
                {
                    var upgrade = upgradeObj as HealthUpgradeObject;
                    var newHP = tank.Hp + upgrade.RestHP;
                    tank.Hp = newHP > tank.MaximumHp ? tank.MaximumHp : newHP;
                    break;
                }
                case UpgradeType.MaxHp:
                {
                    var upgrade = upgradeObj as MaxHpUpgradeObject;
                    await Task.Run(() =>
                        {
                            tank.MaximumHp += upgrade.IncreaseHP;
                            tank.Hp += upgrade.IncreaseHP;
                            Thread.Sleep(time);
                            tank.MaximumHp -= upgrade.IncreaseHP;
                            if (tank.Hp > tank.MaximumHp)
                            {
                                tank.Hp = tank.MaximumHp;
                            }
                        });
                        
                    break;
                }
                case UpgradeType.Invulnerability:
                {
                    var upgrade = upgradeObj as InvulnerabilityUpgradeObject;
                    CallInvulnerability(tank, defaultTankSettings.TimeOfInvulnerability);
                    break;
                }
                case UpgradeType.Speed:
                {
                    var upgrade = upgradeObj as SpeedUpgradeObject;
                    await Task.Run((() =>
                        {
                            tank.Speed += upgrade.IncreaseSpeed;
                            Thread.Sleep(time);
                            tank.Speed -= upgrade.IncreaseSpeed;
                        }));

                    break;
                }
            }
        }

        public void UpdateSettings()
        {
            var settings = serverSettings.TankSettings;
            if (settings == null || settings == defaultTankSettings)
            {
                return;
            }

            if (settings.TimeOfActionUpgrades < 100)
            {
                settings.TimeOfActionUpgrades *= 1000;
            }

            if (settings.TimeOfInvulnerability < 100)
            {
                settings.TimeOfInvulnerability *= 1000;
            }

            if (settings.ChanceSpawnUpgrades > 1)
            {
                settings.ChanceSpawnUpgrades /= 100;
            }

            lock (_syncObject)
            {
                Map.InteractObjects.OfType<TankObject>().ToList().ForEach(x =>
                {
                    x.BulletSpeed = x.BulletSpeed == defaultTankSettings.BulletSpeed
                        ? settings.BulletSpeed * settings.GameSpeed
                        : settings.BulletSpeed * settings.GameSpeed + (x.BulletSpeed - defaultTankSettings.BulletSpeed);
                    x.Damage = x.Damage == settings.TankDamage
                        ? settings.TankDamage
                        : settings.TankDamage + (x.Damage - defaultTankSettings.TankDamage);
                    x.Speed = x.Speed == defaultTankSettings.TankSpeed * defaultTankSettings.GameSpeed
                        ? settings.TankSpeed * settings.GameSpeed
                        : settings.TankSpeed * settings.GameSpeed + (x.Speed - defaultTankSettings.TankSpeed * defaultTankSettings.GameSpeed);
                    x.Hp = x.Hp > settings.TankMaxHP + (x.MaximumHp - defaultTankSettings.TankMaxHP)
                        ? settings.TankMaxHP + (x.MaximumHp - defaultTankSettings.TankMaxHP)
                        : x.Hp;
                    x.MaximumHp = x.MaximumHp == defaultTankSettings.TankMaxHP
                        ? settings.TankMaxHP
                        : settings.TankMaxHP + (x.MaximumHp - defaultTankSettings.TankMaxHP);
                });

                Map.InteractObjects.OfType<UpgradeInteractObject>().ToList().ForEach(x =>
                {
                    switch (x.Type)
                    {
                        case UpgradeType.BulletSpeed:
                            ((BulletSpeedUpgradeObject) x).IncreaseBulletSpeed = settings.IncreaseBulletSpeed;
                            break;
                        case UpgradeType.Damage:
                            ((DamageUpgradeObject) x).IncreaseDamage = settings.IncreaseDamage;
                            break;
                        case UpgradeType.Health:
                            ((HealthUpgradeObject) x).RestHP = settings.RestHP;
                            break;
                        case UpgradeType.Invulnerability:
                            ((InvulnerabilityUpgradeObject) x).ActionTime = settings.TimeOfInvulnerability;
                            break;
                        case UpgradeType.MaxHp:
                            ((MaxHpUpgradeObject) x).IncreaseHP = settings.IncreaseHP;
                            break;
                        case UpgradeType.Speed:
                            ((SpeedUpgradeObject) x).IncreaseSpeed = settings.IncreaseSpeed;
                            break;
                    }
                });
            }

            defaultTankSettings = settings;
            serverSettings.TankSettings = null;

            lock (Clients)
            {
                foreach (var client in Clients)
                {
                    client.Value.NeedUpdateSettings = true;
                }
            }
        }
        
        public void DelegateMethod(string log, LoggerType loggerType)
        {
            switch (loggerType)
            {
                case LoggerType.Debug:
                    LoggerDelegates = _logger.Debug;
                    break;
                case LoggerType.Error:
                    LoggerDelegates = _logger.Error;
                    break;
                case LoggerType.Fatal:
                    LoggerDelegates = _logger.Fatal;
                    break;
                case LoggerType.Info:
                    LoggerDelegates = _logger.Info;
                    break;
                case LoggerType.Trace:
                    LoggerDelegates = _logger.Trace;
                    break;
                case LoggerType.Warn:
                    LoggerDelegates = _logger.Warn;
                    break;
                default:
                    LoggerDelegates = _logger.Debug;
                    break;
            }
        }
        public string GetCorrectNickname(ServerResponse response)
        {
            var regex = new Regex(@"[!?@#$%^_\-;:\'*&()<>/|\\\s]", RegexOptions.IgnoreCase);

            var nickname = regex.Replace(response.CommandParameter, "");

            return nickname.Length > 15 ? nickname.Substring(0, 15) : nickname;
        }
    }
}
