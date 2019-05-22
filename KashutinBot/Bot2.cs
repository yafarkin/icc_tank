using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;

namespace TankClient
{
    class Bot2 : IClientBot
    {
        protected Rectangle rectangle;
        protected Map _map;
        //private delegate ServerResponse Script();
        //private static int stepsBeforeOver = 0;
        //private static Script delegateScript = Stop;
        private static Rectangle lastRectangle;

        public ServerResponse Client(int msgCount, ServerRequest request)
        {
            var myTank = request.Tank;

            rectangle = myTank.Rectangle;
            _map = SetClassMap(_map, request);
            _map.MapHeight = request.Map.MapHeight;
            _map.MapWidth = request.Map.MapWidth;

            CheckClassMapOnNull(_map);

            //Записать в карту все интерактивные объекты из ответа сервера
            _map.InteractObjects = request.Map.InteractObjects;

            
            if (null == myTank)
            {
                return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
            }

            //если размер танка не известен, то узнать
            if (null == rectangle)
            {
                rectangle = myTank.Rectangle;
            }

            var nearestObj = FindNearestInteractiveObjWithoutWalls(_map, myTank);
            GoToPoint(_map, myTank, GetX(myTank), GetY(myTank));

            return None();

        }

        /// <summary>
        /// Находит ближайший объект (без стен)
        /// </summary>
        /// <param name="map"></param>
        /// <param name="myTank"></param>
        /// <returns></returns>
        private static BaseInteractObject FindNearestInteractiveObjWithoutWalls(Map map, TankObject myTank)
        {
            int shortestDistToElem = map.Cells.Length;
            //BaseInteractObject lastIndex/* = map.InteractObjects.Count - 1*/;
            BaseInteractObject nearestObject = null/* = map.InteractObjects[lastIndex]*/;

            //Если есть интерактивные объекты
            if (map.InteractObjects != null)
            {
                foreach (var elem in map.InteractObjects)
                {
                    //Если этот элемент это 
                    if (!Equals(elem.Id, myTank.Id))
                    {
                        var distToElem = (Math.Abs(elem.Rectangle.LeftCorner.LeftInt - GetX(myTank)) + Math.Abs(elem.Rectangle.LeftCorner.TopInt - GetY(myTank)));
                        if (distToElem < shortestDistToElem)
                        {
                            shortestDistToElem = distToElem;
                            nearestObject = elem;
                        }
                    }
                }
            }

            return nearestObject;
        }


        /// <summary>
        /// Идёт к точке на карте
        /// </summary>
        /// <param name="map"></param>
        /// <param name="myTank"></param>
        /// <param name="destinationX"></param>
        /// <param name="destinationY"></param>
        /// <returns></returns>
        private static ServerResponse GoToPoint(Map map, TankObject myTank, int destinationX, int destinationY)
        {
            if (map.InteractObjects != null)
            {
                if (Math.Abs(destinationX - GetX(myTank)) > Math.Abs(destinationY - GetY(myTank)))
                {
                    //Если левый угол моего танка по Х меньше, чем левый угол достигаемого объекта и я не повёрнут направо
                    if (GetX(myTank) < destinationX && myTank.Direction != DirectionType.Right)
                    {
                        //повернуть направо
                        return TurnRight();
                    }
                    else
                    {
                        //Если мой левый угол меньше, чем левый угол  апгрейда
                        if (GetX(myTank) < destinationX)
                        {
                            //Ехать
                            if (lastRectangle is null || (lastRectangle.LeftCorner.Left != myTank.Rectangle.LeftCorner.Left && lastRectangle.LeftCorner.Top != myTank.Rectangle.LeftCorner.Top))
                            {
                                lastRectangle = myTank.Rectangle;
                                return Go();
                            }
                            else
                            {
                                return Fire();
                            }
                        }
                    }

                    //Если левый угол моего танка по Х больше, чем левый угол достигаемого объекта и я не повёрнут на
                    if (GetX(myTank) > destinationX && myTank.Direction != DirectionType.Left)
                    {
                        //повернуть направо
                        return TurnLeft();
                    }
                    else
                    {
                        //Если мой левый угол меньше, чем левый угол  апгрейда
                        if (GetX(myTank) > destinationX)
                        {
                            //Ехать
                            if (lastRectangle is null || (lastRectangle.LeftCorner.Left != myTank.Rectangle.LeftCorner.Left && lastRectangle.LeftCorner.Top != myTank.Rectangle.LeftCorner.Top))
                            {
                                lastRectangle = myTank.Rectangle;
                                return Go();
                            }
                            else
                            {
                                return Fire();
                            }
                        }
                    }
                }
                else
                {
                    //Если левый угол моего танка по Y меньше, чем левый угол улучшающего объекта и я не повёрнут вниз
                    if (GetY(myTank) < destinationY && myTank.Direction != DirectionType.Down)
                    {
                        //повернуть вниз
                        return TurnDown();
                    }
                    else
                    {
                        //Если мой левый угол меньше по Y, чем левый угол  апгрейда
                        if (GetY(myTank) < destinationY)
                        {
                            //Ехать
                            if (lastRectangle is null || (lastRectangle.LeftCorner.Left != myTank.Rectangle.LeftCorner.Left && lastRectangle.LeftCorner.Top != myTank.Rectangle.LeftCorner.Top))
                            {
                                lastRectangle = myTank.Rectangle;
                                return Go();

                            }
                            else
                            {
                                return Fire();
                            }
                        }
                    }

                    //Если левый угол моего танка по Y больше, чем левый угол улучшающего объекта и я не повёрнут вверх
                    if (GetY(myTank) > destinationY && myTank.Direction != DirectionType.Up)
                    {
                        //повернуть вверх
                        return TurnUp();
                    }
                    else
                    {
                        //Если мой левый угол меньше, чем левый угол объекта
                        if (GetY(myTank) > destinationY)
                        {
                            //Ехать
                            if (lastRectangle is null || (lastRectangle.LeftCorner.Left != myTank.Rectangle.LeftCorner.Left && lastRectangle.LeftCorner.Top != myTank.Rectangle.LeftCorner.Top))
                            {
                                lastRectangle = myTank.Rectangle;
                                return Go();
                            }
                            else
                            {
                                return Fire();
                            }
                        }
                        else
                        {
                            return Fire();
                        }
                    }
                }
            }

            return new ServerResponse { ClientCommand = ClientCommandType.None };
        }

        private Map SetClassMap(Map map, ServerRequest request)
        {
            //Если карта существует присвоить карте карту из ответа сервера
            if (request.Map.Cells != null)
            {
                _map = request.Map;
                //var translMap = TranslMapForPassfind(_map);
            }
            return _map;
        }

        private ServerResponse CheckClassMapOnNull(Map _map)
        {
            if (_map == null)
            {
                return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
            }
            else
            {

            }
            return null;
        }

        private static ServerResponse Fire()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.Fire };
        }

        private static ServerResponse TurnRight()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnRight };
        }

        private static ServerResponse TurnLeft()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnLeft };
        }

        private static ServerResponse TurnUp()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnUp };
        }

        private static ServerResponse TurnDown()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnDown };
        }

        private static ServerResponse Go()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.Go };
        }

        private static ServerResponse Stop()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.Stop };
        }

        private static ServerResponse None()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.None };
        }

        private static int GetX(BaseInteractObject tank)
        {
            return tank.Rectangle.LeftCorner.LeftInt;
        }

        private static int GetY(BaseInteractObject tank)
        {
            return tank.Rectangle.LeftCorner.TopInt;
        }
    }
}
