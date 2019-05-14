namespace TankCommon
{
    interface ISettings
    {
        string ServerName { get; set; }
        Enum.ServerType ServerType { get; set; }
        System.DateTime SessionTime { get; set; }
        decimal GameSpeed { get; set; }
        decimal TankSpeed { get; set; }
        decimal BulletSpeed { get; set; }
        decimal TankDamage { get; set; }
    }
}