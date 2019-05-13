using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TankCommon.Enum;
using TankCommon.Objects;

namespace TankCommon
{
    public static class MapManager
    {
        public static Map LoadMap(uint mapSize = 20, CellMapType primaryObject = CellMapType.Grass, int percentOfPrimObj = 50, int percentAnotherObj = 50)
        {
            var mapData = GenerateMap(mapSize, primaryObject, percentOfPrimObj, percentAnotherObj);
            var cells = new List<List<CellMapType>>();

            if (percentOfPrimObj + percentAnotherObj > 100)
            {
                throw new InvalidDataException("Это сочетание работоспособно, но нет смысла ставить процентов больше 100");
            }

            if (mapData.GetLength(0) <= 3 || mapData.GetLength(1) <= 3)
            {
                throw new InvalidDataException("Слишком маленькая карта");
            }

            if (mapData.GetLength(0) != mapData.GetLength(1))
            {
                throw new ArgumentNullException("Карта должа быть квадратной");
            }

            var cellArr = new CellMapType[mapSize * Constants.CellHeight, mapSize * Constants.CellWidth];
            var cellArrLen = cellArr.GetLength(0);
            for (var x = 0; x < cellArrLen; x++)
            {
                for (var y = 0; y < cellArrLen; y++)
                {
                    var x1 = (int)Math.Ceiling((decimal)(x / Constants.CellHeight));
                    var y1 = (int)Math.Ceiling((decimal)(y / Constants.CellHeight));
                    cellArr[x, y] = mapData[x1, y1];
                }
            }

            
            return new Map(cellArr);
        }

        public static List<KeyValuePair<Point, CellMapType>> WhatOnMap(Rectangle rectangle, Map map)
        {
            var left = rectangle.LeftCorner.LeftInt;
            var top = rectangle.LeftCorner.TopInt;

            var result = new List<KeyValuePair<Point, CellMapType>>(rectangle.WidthInt * rectangle.HeightInt);
            for (var i = top; i < top + rectangle.Height; i++)
            {
                for (var j = left; j < left + rectangle.Width; j++)
                {
                    result.Add(new KeyValuePair<Point, CellMapType>(new Point(j, i), map[i, j]));
                }
            }

            return result;
        }

        public static BaseInteractObject GetObjectAtPoint(Point location, IEnumerable<BaseInteractObject> interactObjects)
        {
            return interactObjects.FirstOrDefault(i => i.Rectangle.IsPointInRectange(location));
        }

        public static BaseInteractObject GetIntersectedObject(Rectangle rectangle, IEnumerable<BaseInteractObject> interactObjects)
        {
            return interactObjects.FirstOrDefault(i => i.Rectangle.IsRectangleIntersected(rectangle));
        }

        /// <summary>
        /// Генерирует квадратную карту по переданной длинне стороны
        /// </summary>
        /// <param name="mapSize"></param>
        /// <returns>Массив, представляет каждый элемент, как ширину карты</returns>
        private static CellMapType[,] GenerateMap(uint mapSize, CellMapType primaryObject, int percentOfPrimObj, int percentAnotherObj)
        {
            //создаю и заполняю массив
            var preMap = new CellMapType[mapSize,mapSize];
            for (var x = 0; x < mapSize; x++)
            {
                for (var y = 0; y < mapSize; y++)
                {
                    if (x == 0 || y == 0 || x == mapSize - 1 || y == mapSize - 1)
                    {
                        preMap[x, y] = CellMapType.Wall;
                    }
                    else
                    {
                        preMap[x, y] = CellMapType.Void;
                    }
                }
            }

            preMap = GeneratePrimitiveOnMap(preMap, primaryObject, percentOfPrimObj, percentAnotherObj);
            return preMap;
        }

        /// <summary>
        /// Генерирует псевдорандомную карту состоящую из сплошных линий
        /// </summary>
        /// <param name="map"></param>
        /// <param name="primaryObject"></param>
        /// <param name="percentOfPrimObj"></param>
        /// <param name="percentAnotherObj"></param>
        /// <returns></returns>
        private static CellMapType[,] GeneratePrimitiveOnMap(CellMapType[,] map, CellMapType primaryObject, int percentOfPrimObj, int percentAnotherObj)
        {
            var rnd = new Random();
            map = DrawHorizontals(map, primaryObject, percentOfPrimObj, rnd);
            map = DrawVerticals(map, percentOfPrimObj, percentAnotherObj, rnd);
            return map;
        }

        /// <summary>
        /// Рисует горизонтальные линии на карте из тех объектов, которых должно быть больше
        /// </summary>
        /// <param name="map"></param>
        /// <param name="symbol"></param>
        /// <param name="percentOfPrimObj"></param>
        /// <param name="rnd"></param>
        /// <returns></returns>
        private static CellMapType[,] DrawHorizontals(CellMapType[,] map, CellMapType symbol, int percentOfPrimObj, Random rnd)
        {
            int rndNum;
            var mapLength = map.GetLength(0);
            for (var x = 2; x < mapLength - 2; x++)
            {
                rndNum = rnd.Next(0, 100);
                for (var y = 2; y < mapLength - 2; y++)
                {
                    if (rndNum < percentOfPrimObj)
                    {
                        if (map[x - 1, y - 1] != CellMapType.Wall && map[x - 1, y - 1] != CellMapType.Water && map[x - 1, y - 1] != CellMapType.DestructiveWall &&
                            map[x + 1, y - 1] != CellMapType.Wall && map[x + 1, y - 1] != CellMapType.Water && map[x + 1, y - 1] != CellMapType.DestructiveWall)
                        {
                            //создаю строку того типа, которого должно быть больше
                            map[x, y] = symbol;
                        }
                    }
                }
            }
            return map;
        }

        /// <summary>
        /// Рисует вертикальные линии на карте, проверяя не пересёкся ли он с непроходимой линией и устраняя непроходимость
        /// </summary>
        /// <param name="map"></param>
        /// <param name="rnd"></param>
        /// <param name="percentOfPrimObj">Для вычисления необходимого количества остальных объектов</param>
        /// <param name="percentAnotherObj">Сколько процентов площади должны занимать не преобладающие на карте объекты</param>
        /// <returns></returns>
        private static CellMapType[,] DrawVerticals(CellMapType[,] map, int percentOfPrimObj, int percentAnotherObj, Random rnd)
        {
            var wall = CellMapType.Wall;
            var water = CellMapType.Water;
            var dWall = CellMapType.DestructiveWall;
            var field = CellMapType.Void;
            var grass = CellMapType.Grass;

            int rndNum;
            var mapLength = map.GetLength(0);
            var arrSymbols = new CellMapType[] { wall, water, grass, dWall/*, field*/};
            for (var x = 1; x < mapLength - 1; x++)
            {
                rndNum = rnd.Next(0, 100);
                var rndForObj = rnd.Next(0, 4);
                for (var y = 1; y < mapLength - 1; y++)
                {
                    if (rndNum < (percentOfPrimObj + percentAnotherObj) && rndNum > percentOfPrimObj)
                    {
                        //Проверяю пересечения с линиями, а так же проверяю нет перекроет ли линия уже имеющийся проход
                        if (map[y, x] != wall && map[y, x] != water && map[y, x] != dWall &&
                            map[y + 1, x] != wall && map[y + 1, x] != water && map[y + 1, x] != dWall &&
                            map[y, x + 1] != wall && map[y, x + 1] != water && map[y, x + 1] != dWall &&
                            map[y - 1, x] != wall && map[y - 1, x] != water && map[y - 1, x] != dWall &&
                            map[y, x - 1] != wall && map[y, x - 1] != water && map[y, x - 1] != dWall)
                        {
                            map[y, x] = arrSymbols[rndForObj];
                        }
                        else
                        {
                            //Удаляю 4 клетки возле пересечения, проверяя, что не удалю стену карты
                            if (x + 1 != mapLength - 1 && y + 1 != mapLength - 1 && x - 1 != 0 && y - 1 != 0)
                            {
                                map[y, x] = arrSymbols[rndForObj];
                                map[y - 1, x] = field;
                                map[y, x - 1] = field;
                                map[y + 1, x] = field;
                                if (x > mapLength / 2)
                                {
                                    x--;
                                }
                                else
                                {
                                    x++;
                                }
                                y++;
                            }
                        }
                    }
                }
            }
            return map;
        }
    
    }
}
