﻿using System;
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
        public static Map LoadMap(uint mapSize = 12, CellMapType primaryObject = CellMapType.Grass, int percentOfPrimObj = 25, int percentAnotherObj = 12)
        {
            var mapData = GenerateMap(mapSize, primaryObject, percentOfPrimObj, percentAnotherObj);
            var width = 0;
            var height = mapSize;
            var cells = new List<List<CellMapType>>();
            //foreach (var line in mapData)
            //{
                //Проверка на квадратность
                //height++;
                //    if (0 == width)
                //    {
                //        width = line.Length;
                //    }
                //    else if (line.Length != width)
                //    {
                //        throw new InvalidDataException("Карта содержит разное количество элементов в строках");
                //    }

                //    //Преобразование из массива стрингов в лист CellMapType
                //    var cellLine = new List<CellMapType>();
                //    foreach (var elem in line)
                //    {
                //        CellMapType cellMapType;
                //        switch (elem)
                //        {
                //            case 'с':
                //                cellMapType = CellMapType.Wall;
                //                break;
                //            case ' ':
                //                cellMapType = CellMapType.Void;
                //                break;
                //            case 'в':
                //                cellMapType = CellMapType.Water;
                //                break;
                //            case '*':
                //                cellMapType = CellMapType.DestructiveWall;
                //                break;
                //            case 'т':
                //                cellMapType = CellMapType.Grass;
                //                break;
                //            default:
                //                throw new InvalidDataException("Неизвестный тип элемента карты");
                //        }

                //        for (var b = 0; b < Constants.CellWidth; b++)
                //        {
                //            cellLine.Add(cellMapType);
                //        }
                //    }

                //    for (var a = 0; a < Constants.CellHeight; a++)
                //    {
                //        cells.Add(cellLine);
                //    }
            //}


            if (mapData.GetLength(0) <= 3 || mapData.GetLength(1) <= 3)
            {
                throw new InvalidDataException("Слишком маленькая карта");
            }

            if (mapData.GetLength(0) != mapData.GetLength(1))
            {
                throw new ArgumentNullException("Карта должа быть квадратной");
            }

            //Преобразование в массив c увеличением ячеек
            //var cellArr = new CellMapType[height * Constants.CellHeight, height * Constants.CellWidth];
            //for (var i = 0; i < height-1 * Constants.CellHeight; i++)
            //{
            //    for (var j = 0; j < height-1 * Constants.CellWidth; j++)
            //    {
            //        cellArr[i * Constants.CellHeight, j * Constants.CellHeight] = mapData[i, j];
            //    }
            //}

            var cellArr2 = new CellMapType[mapSize * Constants.CellHeight, mapSize * Constants.CellWidth];
            var cellArrLen = cellArr2.GetLength(0);
            for (var x = 0; x < ((mapSize -1) * (Constants.CellHeight - 1)); x++)
            {
                Console.Write('\n');
                for (var y = 0; y < ((mapSize -1) *  Constants.CellWidth - 1); y++)
                {
                    cellArr2[(x * (Constants.CellHeight - 1) ), y * (Constants.CellWidth - 1)] = mapData[(int)x/(Constants.CellHeight - 1), (int)y / (Constants.CellWidth - 1)];
                    Console.Write(mapData[(int)x / (Constants.CellHeight - 1), (int)y / (Constants.CellWidth - 1)]);
                }
            }

            
            return new Map(cellArr2);
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
            var preMap1 = new CellMapType[mapSize,mapSize];
            for (var x = 0; x < mapSize; x++)
            {
                for (var y = 0; y < mapSize; y++)
                {
                    if (x == 0 || y == 0 || x == mapSize - 1 || y == mapSize - 1)
                    {
                        preMap1[x, y] = CellMapType.Wall;
                    }
                    else
                    {
                        preMap1[x, y] = CellMapType.Void;
                    }
                }
            }

            preMap1 = GeneratePrimitiveOnMap(preMap1, primaryObject, percentOfPrimObj, percentAnotherObj);
            return preMap1;
        }

        /// <summary>
        /// Генерирует объекты на переданной карте и возвращает её
        /// </summary>
        /// <param name="map">Пустая карта с краями</param>
        /// <param name="primaryObject">Какого типа объектов должно быть больше всего</param>
        /// <param name="percentOfPrimObj">Какой процент карты должны занимать объекты выбранного типа</param>
        /// <returns></returns>
        private static char[,] GenerateRndMapObjects(char[,] map, char primaryObject, int percentOfPrimObj, int percentAnotherObj)
        {
            var mapObjects = new char[] { 'с', ' ', '*', 'т', 'в' };
            var rnd = new Random();
            int rndNum;

            for (var x = 0; x < map.GetLength(0); x++)
            {
                for (var y = 0; y < map.GetLength(1); y++)
                {
                    if (map[x, y] == ' ')
                    {
                        //Для каждого элемента карты беру рандомное число
                        rndNum = rnd.Next(0, 100); 
                        if (rndNum < percentOfPrimObj)
                        {
                            map[x, y] = primaryObject;
                        }
                        else
                        {
                            if (rndNum < (percentOfPrimObj + percentAnotherObj))
                            {
                                map[x, y] = mapObjects[rnd.Next(0, 4)];
                            }
                        }
                    }
                }
            }
            return map;
        }

        /// <summary>
        /// Генерирует псевдорандомную карту состоящую из сплошных линий
        /// </summary>
        /// <param name="map"></param>
        /// <param name="primaryObject"></param>
        /// <param name="percentOfPrimObj"></param>
        /// <param name="percentAnotherObj"></param>
        /// <returns></returns>
        private static char[,] GeneratePrimitiveOnMap(char [,] map, char primaryObject, int percentOfPrimObj, int percentAnotherObj)
        {
            var rnd = new Random();
            map = DrawHorizontals(map, primaryObject, percentOfPrimObj, rnd);
            map = DrawVerticals(map, percentOfPrimObj, percentAnotherObj, rnd);
            map = CheckLineOnPass(map);
            return map;
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
            //map = CheckLineOnPass(map);
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
        private static char[,] DrawHorizontals(char[,] map, char symbol, int percentOfPrimObj, Random rnd)
        {
            int rndNum;
            for (var x = 1; x < map.GetLength(0) - 1; x++)
            {
                rndNum = rnd.Next(0, 100);
                for (var y = 1; y < map.GetLength(0) - 1; y++)
                {
                    if (rndNum < percentOfPrimObj)
                    {
                        //создаю строку того типа, которого должно быть больше
                        map[x, y] = symbol;
                      
                    }
                }
            }
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
            for (var x = 1; x < mapLength - 1; x++)
            {
                rndNum = rnd.Next(0, 100);
                for (var y = 1; y < mapLength - 1; y++)
                {
                    if (rndNum < percentOfPrimObj)
                    {
                        //создаю строку того типа, которого должно быть больше
                        map[x, y] = symbol;

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
        private static char[,] DrawVerticals(char[,] map, int percentOfPrimObj, int percentAnotherObj, Random rnd)
        {
            int rndNum;
            var arrSymbols = new char[] { ' ', 'т', '*', 'в'};
            for (var x = 1; x < map.GetLength(0) - 1; x++)
            {
                rndNum = rnd.Next(0, 100);
                var rndForObj = rnd.Next(0, 4);
                for (var y = 1; y < map.GetLength(0) - 1; y++)
                {
                    if (rndNum < (percentOfPrimObj + percentAnotherObj) && rndNum > percentOfPrimObj)
                    {
                        //Проверяю пересечения с линиями, а так же проверяю нет перекроет ли линия уже имеющийся проход
                        if (map[y, x] != 'с' && map[y, x] != 'в' && map[y, x] != '*' &&
                            map[y + 1, x] != 'с' && map[y + 1, x] != 'в' && map[y + 1, x] != '*' &&
                            map[y, x + 1] != 'с' && map[y, x + 1] != 'в' && map[y, x + 1] != '*')
                        {
                            map[y, x] = arrSymbols[rndForObj];
                        }
                        else
                        {
                            //Удаляю 4 клетки возле пересечения, проверяя, что не удалю стену карты
                            if (x + 1 != map.GetLength(0)-1 && y + 1 != map.GetLength(0)-1 && x-1 != 0 && y-1 != 0)
                            {
                                map[y, x] = ' ';
                                map[y - 1, x] = ' ';
                                map[y, x - 1] = ' ';
                                map[y + 1, x] = ' ';
                                x++;
                                y++;
                            }
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
            var arrSymbols = new CellMapType[] { wall, water, wall, wall, wall, };
            var a = (int)CellMapType.Grass;
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
                                map[y, x] = field;
                                map[y - 1, x] = field;
                                map[y, x - 1] = field;
                                map[y + 1, x] = field;
                                x++;
                                y++;
                            }
                        }
                    }
                }
            }
            return map;
        }

        /// <summary>
        /// Проверяет не сплошная ли линия и стерает предпоследний в строке элемент, если сплошная
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        private static char[,] CheckLineOnPass( char[,] map)
        {
            var mapLength = map.GetLength(0);
            for (var y = 1; y < mapLength - 1; y++)
            {
                var counInLine = 0;
                for (var x = 1; x < mapLength - 1; x++)
                {
                    if (map[x,y] == 'с' || map[x, y] == 'в' || map[x, y] == '*' )
                    {
                        counInLine++;
                    }
                }
                if(counInLine >= mapLength - 2)
                {
                    map[mapLength - 2, y] = ' ';
                }
            }

            for (var y = 1; y < mapLength - 1; y++)
            {
                var counInLine = 0;
                for (var x = 1; x < mapLength - 1; x++)
                {
                    if (map[y, x] == 'с' || map[x, y] == 'в' || map[x, y] == '*')
                    {
                        counInLine++;
                    }
                }
                if (counInLine >= mapLength - 2)
                {
                    map[y, mapLength - 2] = ' ';
                }
            }
            return map;
        }

        /// <summary>
        /// Переводит карту из массива чаров в массив стрингов. 
        /// </summary>
        /// <param name="charMap"></param>
        /// <returns></returns>
        private static string[] GetStringedArray(char[,] charMap)
        {
            var stringedMap = new string[charMap.GetLength(0)];
            var sBuilder = new StringBuilder();
            for (var x = 0; x < charMap.GetLength(0); x++)
            {
                for (var y = 0; y < charMap.GetLength(1); y++)
                {
                    sBuilder.Append(charMap[x, y]);
                }
                stringedMap[x] = sBuilder.ToString();
                sBuilder.Remove(0, sBuilder.Length);
            }
            return stringedMap;
        }
    
    }
}
