using UnityEngine;

namespace Battle
{
    public class ActiveEffect
    {
        public BaseEffect Effect;
        public float TimeLeft;

        public ActiveEffect(BaseEffect effect)
        {
            Effect = effect;
            TimeLeft = effect.Duration;
        }
    }
}

