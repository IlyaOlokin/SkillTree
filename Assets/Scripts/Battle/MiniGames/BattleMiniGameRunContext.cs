using System;

namespace Battle.MiniGames
{
    public sealed class BattleMiniGameRunContext
    {
        private bool _completed;

        public BattleMiniGameEventDefinition Definition { get; }
        public Unit Player { get; }
        public float Power { get; }
        public float Duration { get; }

        public event Action<BattleMiniGameResult> Completed;

        public BattleMiniGameRunContext(
            BattleMiniGameEventDefinition definition,
            Unit player,
            float power,
            float duration)
        {
            Definition = definition;
            Player = player;
            Power = power;
            Duration = duration;
        }

        public void Complete(BattleMiniGameResult result)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;
            Completed?.Invoke(result);
        }
    }
}
