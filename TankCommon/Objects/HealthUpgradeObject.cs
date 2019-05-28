using System;

namespace TankCommon.Objects
{
    public class HealthUpgradeObject : UpgradeInteractObject
    {
        public decimal RestHP { get; set; }

        public HealthUpgradeObject()
        {
        }

        public HealthUpgradeObject(Guid id, Rectangle rectangle, int restHP) : base(id, rectangle)
        {
            RestHP = restHP;
            Type = UpgradeType.Health;
        }
    }
}
