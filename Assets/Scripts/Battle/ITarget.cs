using UnityEngine;

namespace Battle
{
    public interface ITarget
    {
        public Unit UnitObject { get; set; }

        public DamageInstance ReceiveDamage(DamageInfo damageInfo);
        public void ReceiveDoT(DamageInstance damageInstance);
        public void OnHitEvaded(DamageInstance damageInstance);
        public void OnHitBlock(DamageInstance damageInstance);
    }
}

