using UnityEngine;

namespace Battle
{
    public interface ITarget
    {
        public void ReceiveAttack(Damage damage);
    }
}

