namespace TankCommon
{
    using TankCommon.Enum;
    interface ISettings
    {
        string ServerName { get; set; }
        ServerType ServerType { get; set; }
        System.DateTime SessionTime { get; set; }
        decimal GameSpeed { get; set; }
    }
}
