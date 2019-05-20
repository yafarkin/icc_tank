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
        /// <summary>
        /// Создаёт карту по переданным параметрам
        /// </summary>
        /// <param name="mapHeight">Высота создаваемой карты</param>
        /// <param name="mapWidth">Ширина создаваемой карты</param>
        /// <param name="primaryObject">Объекты, которых должно быть больше всего на карте</param>
        /// <param name="percentOfPrimObj">Процент объектов преобладающих на карте</param>
        /// <param name="percentAnotherObj">Процент присутствия на карте второстепенных объектов</param>
        /// <returns>Объект типа карта</returns>
        public static Map LoadMap(int mapHeight = 20, int mapWidth = 20, CellMapType primaryObject = CellMapType.Grass, int percentOfPrimObj = 50, int percentAnotherObj = 50)
        {
            var mapData = GenerateMap(mapHeight, mapWidth, primaryObject, percentOfPrimObj, percentAnotherObj);
            var cells = new List<List<CellMapType>>();

            if (percentOfPrimObj + percentAnotherObj > 100)
            {
                throw new InvalidDataException("Это сочетание работоспособно, но нет смысла ставить процентов больше 100");
            }

            if (mapData.GetLength(0) <= 3 || mapData.GetLength(1) <= 3)
            {
                throw new InvalidDataException("Слишком маленькая карта");
            }

            //Финальный массив должен быть умножен на ширину и длинну константных клеток
            var cellArr = new CellMapType[mapHeight * Constants.CellHeight, mapWidth * Constants.CellWidth];
            var cellArrHeight = cellArr.GetLength(0);
            var cellArrWidth = cellArr.GetLength(1);
            //Масштабирую карту исходя из констант
            for (var height = 0; height < cellArrHeight; height++)
            {
                for (var width = 0; width < cellArrWidth; width++)
                {
                    var oldHeight = (int)Math.Ceiling((decimal)(height / Constants.CellHeight));
                    var oldWidth = (int)Math.Ceiling((decimal)(width / Constants.CellHeight));
                    cellArr[height, width] = mapData[oldHeight, oldWidth];
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
        ///Генерирует квадратную карту по переданным высоте и ширине
        /// </summary>
        /// <param name="mapHeight"></param>
        /// <param name="mapWidth"></param>
        /// <param name="primaryObject"></param>
        /// <param name="percentOfPrimObj"></param>
        /// <param name="percentAnotherObj"></param>
        /// <returns>Двумерный массив сосотоящий из CellMapType</returns>
        private static CellMapType[,] GenerateMap(int mapHeight, int mapWidth, CellMapType primaryObject, int percentOfPrimObj, int percentAnotherObj)
        {
            //создаю и заполняю массив, по краям карты ставлю стены
            var preMap = new CellMapType[mapHeight,mapWidth];
            for (var y = 0; y < mapHeight; y++)
            {
                for (var x = 0; x < mapWidth; x++)
                {
                    if (y == 0 || x == 0 || y == mapHeight - 1 || x == mapWidth - 1)
                    {
                        preMap[y, x] = CellMapType.Wall;
                    }
                    else
                    {
                        preMap[y, x] = CellMapType.Void;
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
            map = DrawVerticals(map, percentOfPrimObj, percentAnotherObj, rnd);
            map = DrawHorizontals(map, primaryObject, percentOfPrimObj, rnd);
            
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
            var mapWidth = map.GetLength(1);
            var mapHeight = map.GetLength(0);
            for (var y = 2; y < mapHeight - 2; y++)
            {
                rndNum = rnd.Next(0, 100);
                for (var x = 2; x < mapWidth - 2; x++)
                {
                    if (rndNum < percentOfPrimObj)
                    {
                        if (map[y - 1, x - 1] != CellMapType.Wall && map[y - 1, x - 1] != CellMapType.Water && map[y - 1, x - 1] != CellMapType.DestructiveWall &&
                            map[y + 1, x - 1] != CellMapType.Wall && map[y + 1, x - 1] != CellMapType.Water && map[y + 1, x - 1] != CellMapType.DestructiveWall)
                        {
                            //создаю строку того типа, которого должно быть больше
                            map[y, x] = symbol;
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
            var mapHeight = map.GetLength(0);
            var mapWidth = map.GetLength(1);
            var arrSymbols = new CellMapType[] { wall, water, grass, dWall};
            for (var x = 1; x < mapWidth - 1; x++)
            {
                rndNum = rnd.Next(0, 100);
                var rndForObj = rnd.Next(0, 4);
                for (var y = 1; y < mapHeight - 1; y++)
                {
                    if (rndNum < (percentOfPrimObj + percentAnotherObj) && rndNum > percentOfPrimObj)
                    {
                        //Проверяю пересечения с линиями, а так же проверяю нет перекроет ли линия уже имеющийся проход
                        if (//В месте постановки нет стен и воды
                            map[y, x] != wall && map[y, x] != water && map[y, x] != dWall &&
                            //Снизу от места постановки нет стен и воды
                            map[y + 1, x] != wall && map[y + 1, x] != water && map[y + 1, x] != dWall &&
                            //справа от места постановки нет стен и воды
                            map[y, x + 1] != wall && map[y, x + 1] != water && map[y, x + 1] != dWall &&
                            //сверху от постановки нет стен и воды
                            map[y - 1, x] != wall && map[y - 1, x] != water && map[y - 1, x] != dWall &&
                            //слева от места постановки нет стен и воды
                            map[y, x - 1] != wall && map[y, x - 1] != water && map[y, x - 1] != dWall)
                        {
                            map[y, x] = arrSymbols[rndForObj];
                        }
                        else
                        {
                            //Удаляю 4 клетки возле пересечения, проверяя, что не удалю стену карты
                            if (x + 1 != mapWidth - 1 && y + 1 != mapHeight - 1 && x - 1 != 0 && y - 1 != 0)
                            {
                                map[y, x] = arrSymbols[rndForObj];
                                //map[y - 1, x] = field;
                                map[y, x - 1] = field;
                                map[y + 1, x] = field;
                                
                                //x++;
                                //y++;
                            }
                        }
                    }
                }
            }
            return map;
        }

    }
}
