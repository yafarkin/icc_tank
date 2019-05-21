using System;
using TankCommon.Enum;

namespace TankCommon
{    
    public class TankSettings
    {
        public int Version { get; set; }
        public decimal GameSpeed { get; set; }
        public decimal TankSpeed { get; set; }
        public decimal BulletSpeed { get; set; }
        public decimal TankDamage { get; set; }

        public TankSettings()
        {
            Version++;
            GameSpeed = 1;
            TankSpeed = 2;
            BulletSpeed = 4;
            TankDamage = 40;
        }

        public void UpdateAll(string _serverName, string _serverType, string _sessionTime, decimal _gameSpeed, decimal _tankSpeed, decimal _bulletSpeed, decimal _tankDamage)
        {
            Version++;
            if (_serverName == "")
            {
                _serverName = null;
            }
            GameSpeed = _gameSpeed;
            TankSpeed = _tankSpeed;
            BulletSpeed = _bulletSpeed;
            TankDamage = _tankDamage;
        }
    }
}
