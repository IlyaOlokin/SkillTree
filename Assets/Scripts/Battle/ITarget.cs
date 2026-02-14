using UnityEngine;

namespace Battle
{
    public interface ITarget
    {
        public Unit UnitObject { get; set; }

        public DamageInstance ReceiveDamage(DamageInstance damageInstance);
        public void ReceiveDoT(DamageInstance damageInstance);
        public void OnEvaded(DamageInstance damageInstance);
    }
}

