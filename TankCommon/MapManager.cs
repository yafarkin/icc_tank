using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TankCommon.Enum;
using TankCommon.Objects;
using System.Linq;

namespace TankCommon
{
    public static class MapManager
    {
        public static Map LoadMap(uint mapSize = 12, char primaryObject = 'т', int percentOfPrimObj = 25, int percentAnotherObj = 12)
        {
            var mapData = GenerateMap(mapSize, primaryObject, percentOfPrimObj, percentAnotherObj);
            var width = 0;
            var height = 0;
            var cells = new List<List<CellMapType>>();
            foreach (var line in mapData)
            {
                height++;
                if (0 == width)
                {
                    width = line.Length;
                }
                else if (line.Length != width)
                {
                    throw new InvalidDataException("Карта содержит разное количество элементов в строках");
                }

                var cellLine = new List<CellMapType>();
                foreach (var c in line)
                {
                    CellMapType cellMapType;
                    switch (c)
                    {
                        case 'с':
                            cellMapType = CellMapType.Wall;
                            break;
                        case ' ':
                            cellMapType = CellMapType.Void;
                            break;
                        case 'в':
                            cellMapType = CellMapType.Water;
                            break;
                        case '*':
                            cellMapType = CellMapType.DestructiveWall;
                            break;
                        case 'т':
                            cellMapType = CellMapType.Grass;
                            break;
                        default:
                            throw new InvalidDataException("Неизвестный тип элемента карты");
                    }

                    for (var b = 0; b < Constants.CellWidth; b++)
                    {
                        cellLine.Add(cellMapType);
                    }
                }

                for (var a = 0; a < Constants.CellHeight; a++)
                {
                    cells.Add(cellLine);
                }
            }

            if (0 == width || 0 == height)
            {
                throw new InvalidDataException("Карта не может быть пустой");
            }

            var cellArr = new CellMapType[height * Constants.CellHeight, width * Constants.CellWidth];
            for (var i = 0; i < height * Constants.CellHeight; i++)
            {
                for (var j = 0; j < width * Constants.CellWidth; j++)
                {
                    cellArr[i, j] = cells[i][j];
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
        private static string[] GenerateMap(uint mapSize, char primaryObject, int percentOfPrimObj, int percentAnotherObj)
        {
            //var Walls = 'с';
            //var DestructiveWalls = '*';
            //var Water = 'в';
            //var Grass = 'т';
            //var Void = ' ';

            //создаю и заполняю массив (чаров для удобства генерации карты)
            var preMap = new char[mapSize, mapSize];

            for (var x = 0; x < mapSize; x++)
            {
                for (var y = 0; y < mapSize; y++)
                {
                    if (x == 0 || y == 0 || x == mapSize - 1 || y == mapSize - 1)
                    {
                        preMap[x, y] = 'с';
                    }
                    else
                    {
                        preMap[x, y] = ' ';
                    }
                }
            }
            //preMap = GenerateRndMapObjects(preMap, primaryObject, percentOfPrimObj, percentAnotherObj);
            preMap = GeneratePrimitiveOnMap(preMap, primaryObject, percentOfPrimObj, percentAnotherObj);
            // возвращаю карту в виде массива стрингов
            return GetStringedArray(preMap);
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
            //PassableCheck(map);
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
        private static char[,] GeneratePrimitiveOnMap(char [,] map, char primaryObject, int percentOfPrimObj,int percentAnotherObj)
        {
            int rndNum;
            var rnd = new Random();
            var arrSymbols = new char[] {' ','т','*'};
            

            for (var x = 1; x < map.GetLength(0) - 1; x++)
            {
                rndNum = rnd.Next(0, 100);
                var rndForObj = rnd.Next(0,3);
                for (var y = 1; y < map.GetLength(0) - 1; y++)
                {
                    if (rndNum < (percentOfPrimObj + percentAnotherObj))
                    {
                        map[y,x] = arrSymbols[rndForObj];
                    }
                }
            }

            return map;
        }

        private static char[,] drawHorizontals(char[,] map, char symbol, int percentOfPrimObj, Random rnd)
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
