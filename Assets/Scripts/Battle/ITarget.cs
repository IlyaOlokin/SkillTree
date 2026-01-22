using UnityEngine;

namespace Battle
{
    public interface ITarget
    {
        public Unit UnitObject { get; set; }

        public void ReceiveDamage(DamageInstance damageInstance);
    }
}

