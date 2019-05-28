using System;

namespace TankCommon.Objects
{
    public class DamageUpgradeObject : UpgradeInteractObject
    {
        public int IncreaseDamage { get; set; }

        public DamageUpgradeObject()
        {
        }

        public DamageUpgradeObject(Guid id, Rectangle rectangle, int increaseDamage) : base(id, rectangle)
        {
            IncreaseDamage = increaseDamage;
            Type = UpgradeType.Damage;
        }
    }
}
