using System;
using System.ComponentModel;
using TankCommon.Enum;

namespace TankCommon.Objects
{    
    public class TankSettings
    {
        [Description("Скорость игры")]
        public int GameSpeed { get; set; } = 1;
        [Description("Множитель скорости танка")]
        public decimal TankSpeed { get; set; } = 2;
        [Description("Множитель скорости пули")]
        public decimal BulletSpeed { get; set; } = 4;
        [Description("Урон танка")]
        public decimal TankDamage { get; set; } = 40;
    }
}
