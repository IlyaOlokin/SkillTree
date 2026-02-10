using SkillTree;
using UnityEngine;

namespace Battle
{
    public abstract class BaseEffect
    {
        public abstract bool IsStackable { get; set; }
        public float Duration = -1;

        public virtual void OnApply(Unit unit){}
        public virtual void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing){}
        public virtual void OnTick(Unit unit, float deltaTime){}
        public virtual bool IsReadyToBeRemoved(Unit unit)
        {
            return false;
        }
        public virtual void OnRemove(Unit unit){}
    }
}



