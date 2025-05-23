﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;

namespace TankClient
{
    class Bot3 : IClientBot
    {
        protected Rectangle rectangle;
        protected Map _map;
        //private delegate ServerResponse Script();
        //private static int stepsBeforeOver = 0;
        //private static Script delegateScript = Stop;
        private static Rectangle lastRectangle;
        private bool searchingWay = false;
        private DirectionType lastDirection;
        private bool needStep = false;

        public ServerResponse Client(int msgCount, ServerRequest request)
        {
            var myTank = request.Tank;

            rectangle = myTank?.Rectangle;
            _map = SetClassMap(_map, request);
            _map.MapHeight = request.Map.MapHeight;
            _map.MapWidth = request.Map.MapWidth;

            CheckClassMapOnNull(_map);

            //Записать в карту все интерактивные объекты из ответа сервера
            _map.InteractObjects = request.Map.InteractObjects;

            //Если танка нет - запросить обновления карты
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
            rectangle = myTank.Rectangle;

            //Если не упрёшься в стену
            // Иди своей дорогой, танчик ... Проходи, не задерживайся!
            if (searchingWay == false)
            {
                //Если не ищешь проход
                if (CanGoToPoint(_map, myTank, myTank.Direction))
                {
                    //Идти к цели
                    return GoToPoint(_map, myTank, GetX(nearestObj), GetY(nearestObj));
                }
                else
                {
                    //Сказать, что ищешь проход
                    searchingWay = true;
                    //Запомнить направление в которое не можешь пройти
                    lastDirection = myTank.Direction;
                }
            }
            else
            {
                //Если наше направление это предыдущее направление и нам не нужен шаг
                if (myTank.Direction == lastDirection && !needStep)
                {
                    //Локальноповернуть влево
                    return TurnLocalLeft(myTank);
                }

                //Если можем пройти в предыдущее направление
                if (CanGoToPoint(_map,myTank,lastDirection))
                {
                    if (myTank.Direction != lastDirection)
                    {
                        ///Здесь отклонился от задумки, возможна проблема с поворотом
                        //Повернуть в локальное право  и сказать, что нужен шаг
                        needStep = true;
                        return TurnLocalRight(myTank);
                    }
                    else
                    {
                        //Не ищем проход
                        searchingWay = false;
                        //Не нужен шаг
                        needStep = false;
                        //Идти один шаг
                        return Go();
                    }

                }
                else
                {
                    //Если можешь идти в своём направлении
                    if (CanGoToPoint(_map, myTank, myTank.Direction))
                    {
                        return Go();
                    }
                    else
                    {
                        searchingWay = true;
                    }
                }
            }
            //return CheckMyRect(myTank);
            return Fire();
        }

        private ServerResponse CheckMyRect(TankObject myTank)
        {
            if(myTank.Rectangle == rectangle && myTank.Direction == lastDirection)
            {
                return doSomething();
            }
            else
            {
                return Fire();
            }
        }

        private ServerResponse doSomething()
        {
            Random rnd = new Random();
            var WhatCanDo = new ClientCommandType[] { ClientCommandType.Fire, ClientCommandType.TurnDown, ClientCommandType.TurnLeft, ClientCommandType.TurnRight, ClientCommandType.Go, };

            return new ServerResponse { ClientCommand =  WhatCanDo[rnd.Next(0, 5)] };

        }

        private ServerResponse FindSomeWay(Map map, TankObject myTank, BaseInteractObject nearestObj, DirectionType lastDirection)
        {
            //Если со стороны, откуда мы отвернулись уже нет непроходимости, то повернуть обратно

            //если со стороны откуда отвернулись нет непроходимости
            if (CanGoToPoint(map, myTank, lastDirection))
            {
                //повернуть направо
                return TurnLocalRight(myTank);
            }
            //иначе повернуть налево
            return TurnLocalLeft(myTank);
        }

        private ServerResponse TurnLocalRight(TankObject myTank)
        {
            ServerResponse changeDirection = TurnDown();

            //Если смотрим налево - повернуть вверх
            if (myTank.Direction == DirectionType.Left)
            {
                changeDirection = TurnUp();
            }
            else
            {
                //Если смотрим вниз - повернуть влево
                if (myTank.Direction == DirectionType.Down)
                {
                    changeDirection = TurnLeft();
                }
                else
                {
                    //Если смотрим направо - повернуть вниз
                    if (myTank.Direction == DirectionType.Right)
                    {
                        changeDirection = TurnDown();
                    }
                    //Если смотрим вверх - повернуть вправо
                    else
                    {
                        changeDirection = TurnRight();
                    }
                }
            }
            return changeDirection;
        }

        /// <summary>
        /// Поворачивает танк налево в локальных координатах
        /// </summary>
        /// <param name="myTank"></param>
        private ServerResponse TurnLocalLeft(TankObject myTank)
        {
            ServerResponse changeDirection = TurnDown();

            //Если смотрим налево - повернуть вниз
            if (myTank.Direction == DirectionType.Left)
            {
                changeDirection = TurnDown();
            }
            else
            {
                //Если смотрим вниз - повернуть направо
                if (myTank.Direction == DirectionType.Down)
                {
                    changeDirection = TurnRight();
                }
                else
                {
                    //Если смотрим направо - повернуть вверх
                    if (myTank.Direction == DirectionType.Right)
                    {
                        changeDirection = TurnUp();
                    }
                    //Если смотрим вверх - повернуть влево
                    else
                    {
                        changeDirection = TurnLeft();
                    }
                }
            }
            return changeDirection;
        }

        /// <summary>
        /// Проверяет, что танк не утонет и не упрётся в стену через клетку в той стороне, которую передали
        /// </summary>
        /// <param name="map"></param>
        /// <param name="myTank"></param>
        /// <param name="direction"> то направление в котором проверяется непроходимость </param>
        /// <returns></returns> 
        private bool CanGoToPoint(Map map, TankObject myTank, DirectionType direction)
        {
            var firstCanGo = false;
            var secondCanGo = false;
            //Если танк повёрнут влево и слева поле или трава на одну клетку
            if (direction == DirectionType.Left &&
                (map.Cells[GetY(myTank), GetX(myTank) - (Constants.CellWidth - 2)] != CellMapType.Wall &&
                map.Cells[GetY(myTank), GetX(myTank) - (Constants.CellWidth - 2)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank), GetX(myTank) - (Constants.CellWidth - 2)] != CellMapType.Water))
            {
                firstCanGo = true;
            }
            if (direction == DirectionType.Up &&
                (map.Cells[GetY(myTank) - (Constants.CellWidth - 2), GetX(myTank)] != CellMapType.Wall &&
                map.Cells[GetY(myTank) - (Constants.CellWidth - 2), GetX(myTank)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank) - (Constants.CellWidth - 2), GetX(myTank)] != CellMapType.Water))
            {
                firstCanGo = true;
            }
            if (direction == DirectionType.Down &&
                map.Cells[GetY(myTank) + ((Constants.CellWidth * 2) - 2), GetX(myTank)] != CellMapType.Wall &&
                map.Cells[GetY(myTank) + ((Constants.CellWidth * 2) - 2), GetX(myTank)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank) + ((Constants.CellWidth * 2) - 2), GetX(myTank)] != CellMapType.Water)
            {
                firstCanGo = true;
            }
            if (direction == DirectionType.Right &&
                (map.Cells[GetY(myTank), GetX(myTank) + (Constants.CellWidth * 2 - 2)] != CellMapType.Wall &&
                map.Cells[GetY(myTank), GetX(myTank) + (Constants.CellWidth * 2 - 2)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank), GetX(myTank) + (Constants.CellHeight * 2 - 2)] != CellMapType.Water))
            {
                firstCanGo = true;
            }
            //А теперь так же для дальнего угла танка
            if (direction == DirectionType.Left &&
                (map.Cells[GetY(myTank) + (Constants.CellHeight - 1), GetX(myTank) - (Constants.CellWidth - 2)] != CellMapType.Wall &&
                map.Cells[GetY(myTank) + (Constants.CellHeight - 1), GetX(myTank) - (Constants.CellWidth - 2)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank) + (Constants.CellHeight - 1), GetX(myTank) - (Constants.CellWidth - 2)] != CellMapType.Water))
            {
                secondCanGo = true;
            }
            if (direction == DirectionType.Up &&
                (map.Cells[GetY(myTank) - (Constants.CellWidth - 2), GetX(myTank) + (Constants.CellHeight - 1)] != CellMapType.Wall &&
                map.Cells[GetY(myTank) - (Constants.CellWidth - 2), GetX(myTank) + (Constants.CellHeight - 1)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank) - (Constants.CellWidth - 2), GetX(myTank) + (Constants.CellHeight - 1)] != CellMapType.Water))
            {
                secondCanGo = true;
            }
            if (direction == DirectionType.Down &&
                (map.Cells[GetY(myTank) + (Constants.CellWidth * 2 - 2), GetX(myTank) + (Constants.CellHeight - 1)] != CellMapType.Wall &&
                map.Cells[GetY(myTank) + (Constants.CellWidth * 2 - 2), GetX(myTank) + (Constants.CellHeight - 1)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank) + (Constants.CellWidth * 2 - 2), GetX(myTank) + (Constants.CellHeight - 1)] != CellMapType.Water))
            {
                secondCanGo = true;
            }
            if (direction == DirectionType.Right &&
                (map.Cells[GetY(myTank) + (Constants.CellHeight - 1), GetX(myTank) + (Constants.CellWidth * 2 - 2)] != CellMapType.Wall &&
                map.Cells[GetY(myTank) + (Constants.CellHeight - 1), GetX(myTank) + (Constants.CellWidth * 2 - 2)] != CellMapType.DestructiveWall &&
                map.Cells[GetY(myTank) + (Constants.CellWidth - 1), GetX(myTank) + (Constants.CellHeight * 2 - 2)] != CellMapType.Water))
            {
                secondCanGo = true;
            }
            return (firstCanGo && secondCanGo);
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
            BaseInteractObject nearestObject = myTank;

            //Если есть интерактивные объекты
            if (map.InteractObjects != null)
            {
                foreach (var elem in map.InteractObjects)
                {
                    //Если этот элемент это не мой танк и не пуля
                    if ((!Equals(elem.Id, myTank.Id)) && !(elem is BulletObject))
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
                                //return Fire();
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
                                //return Fire();
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
                                //return Fire();
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
                                //return Fire();
                            }
                        }
                        else
                        {
                            //return Fire();
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

        private static int GetX(BaseInteractObject interObj)
        {
            return interObj.Rectangle.LeftCorner.LeftInt;
        }

        private static int GetY(BaseInteractObject interObj)
        {
            return interObj.Rectangle.LeftCorner.TopInt;
        }
    }
}
