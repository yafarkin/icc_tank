namespace TankCommon
{
    using System;
    using TankCommon.Enum;
    public class TankSettings : ISettings
    {
        public string ServerName { get; set; }
        public ServerType ServerType { get; set; }
        public DateTime SessionTime { get; set; }
        public decimal GameSpeed { get; set; }
        public decimal TankSpeed { get; set; }
        public decimal BulletSpeed { get; set; }
        public decimal TankDamage { get; set; }

        public TankSettings()
        {
            ServerName = null;
            ServerType = (int)ServerType.BattleCity;
            //kostyl' ne rabotaet
            var StartGame = DateTime.Now;
            var FinishGame = DateTime.Now.AddMinutes(2);
            //SessionTime = DateTime.Parse((FinishGame - StartGame).ToString("HH:mm:ss"));  
            SessionTime = DateTime.Parse(FinishGame.Subtract(StartGame).ToString("HH:mm:ss"));
            GameSpeed = 2;
            TankSpeed = 2;
            BulletSpeed = 4;
            TankDamage = 40;
        }
    }
}
