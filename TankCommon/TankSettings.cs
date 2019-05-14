namespace TankCommon
{
    using TankCommon.Enum;

    public class TankSettings : ISettings
    {
        public string          ServerName  { get; set; }
        public ServerType      ServerType  { get; set; }
        public System.DateTime SessionTime { get; set; }
        public decimal         GameSpeed   { get; set; }
        public decimal         TankSpeed   { get; set; }
        public decimal         BulletSpeed { get; set; }
        public decimal         TankDamage  { get; set; }
    }
}