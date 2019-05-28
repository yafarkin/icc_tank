using System;

namespace TankCommon.Objects
{
    public class BulletSpeedUpgradeObject : UpgradeInteractObject
    {
        public int IncreaseBulletSpeed { get; set; }

        public BulletSpeedUpgradeObject()
        {
        }

        public BulletSpeedUpgradeObject(Guid id, Rectangle rectangle, int increaseBulletSpeed) : base(id, rectangle)
        {
            IncreaseBulletSpeed = increaseBulletSpeed;
            Type = UpgradeType.BulletSpeed;
        }
    }
}
