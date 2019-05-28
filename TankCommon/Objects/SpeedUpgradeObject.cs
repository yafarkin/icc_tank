using System;

namespace TankCommon.Objects
{
    public class SpeedUpgradeObject : UpgradeInteractObject
    {
        public int IncreaseSpeed { get; set; }

        public SpeedUpgradeObject()
        {
        }

        public SpeedUpgradeObject(Guid id, Rectangle rectangle, int increaseSpeed) : base(id, rectangle)
        {
            IncreaseSpeed = increaseSpeed;
            Type = UpgradeType.Speed;
        }
    }
}
