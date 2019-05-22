using System.ComponentModel;

namespace TankCommon.Objects
{
    public class ServerSettings
    {
        [Description("Имя сервера")]
        public string SessionName { get; set; }
        [Description("Тип загружаемого шаблона карты")]
        public Enum.MapType MapType { get; set; }
        [Description("Ширина генерируемой карты")]
        public int Width { get; set; } = 20;
        [Description("Высота генерируемой карты")]
        public int Height { get; set; } = 20;
        [Description("Максимальное количество одновременно играющих игроков")]
        public uint MaxClientCount { get; set; } = 2;
        [Description("Порт сервера")]
        public int Port { get; set; } = 1000;
        [Description("К какому типу игры относится данный сервер")]
        public Enum.ServerType ServerType { get; set; }
        [Description("Класс настройки темпа игры")]
        public TankSettings TankSettings { get; set; } = new TankSettings();
    }
}
