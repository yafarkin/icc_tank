using System;

namespace TankCommon.Objects
{
    public class MaxHpUpgradeObject : UpgradeInteractObject
    {
        public decimal IncreaseHP { get; set; }

        public MaxHpUpgradeObject()
        {
        }

        public MaxHpUpgradeObject(Guid id, Rectangle rectangle, int increaseHP) : base(id, rectangle)
        {
            IncreaseHP = increaseHP;
            Type = UpgradeType.MaxHp;
        }
    }
}
