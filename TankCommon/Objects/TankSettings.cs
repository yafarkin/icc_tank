using System;
using System.ComponentModel;
using TankCommon.Enum;

namespace TankCommon.Objects
{    
    public class TankSettings
    {
        [Description("Версия настроек")]
        public int Version { get; set; }
        [Description("Скорость игры")]
        public int GameSpeed { get; set; } = 1;
        [Description("Множитель скорости танка")]
        public decimal TankSpeed { get; set; } = 2;
        [Description("Множитель скорости пули")]
        public decimal BulletSpeed { get; set; } = 4;
        [Description("Урон танка")]
        public decimal TankDamage { get; set; } = 40;

        public TankSettings()
        {
            Version++;
        }

        public void UpdateAll(int gameSpeed, decimal tankSpeed, decimal bulletSpeed, decimal tankDamage)
        {
            Version++;
            GameSpeed = gameSpeed;
            TankSpeed = tankSpeed;
            BulletSpeed = bulletSpeed;
            TankDamage = tankDamage;
        }
    }
}
