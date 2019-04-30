using System.Collections.Generic;
using System.IO;
using System.Linq;
using TankCommon.Enum;
using TankCommon.Objects;

namespace TankCommon
{
    public static class MapManager
    {
        /// <summary>
        /// Генерирует (пустую) квадратную карту по переданной длинне стороны
        /// </summary>
        /// <param name="mapSize"></param>
        /// <returns>Массив, используя каждый элемент, как ширину карты</returns>
        private static string[] GenerateMap(uint mapSize = 10)
        {
            var lineMap = "";
            var generatedMap = new string[mapSize]; //Сгенерированная карта, для передачи из метода
            for (var j = 0; j< mapSize; j++)
            {
                lineMap = lineMap + " "; //Наполняю строковую переменную пробелами для пустоты в карте
            }
            
            for (int i = 0; i < mapSize ;i++)
            {
                generatedMap[i] = lineMap; // Пустые строки кладу в массив
            }
            return generatedMap; // возвращаю пустую карту
        }

        public static Map LoadMap()
        {
            var width = 0;
            var height = 0;
            var cells = new List<List<CellMapType>>();
            string[] mapData = GenerateMap();
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
    }
}
