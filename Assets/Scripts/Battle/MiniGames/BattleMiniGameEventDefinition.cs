using UnityEngine;

namespace Battle.MiniGames
{
    public enum BattleMiniGameSpawnPlacement
    {
        Default,
        Activator
    }

    [CreateAssetMenu(menuName = "Battle/Mini Games/Event Definition", fileName = "New BattleMiniGameEvent")]
    public sealed class BattleMiniGameEventDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private GameObject activatorPrefab;
        [SerializeField] private GameObject miniGamePrefab;
        [SerializeField] private BattleMiniGameReward reward;
        [SerializeField, Min(0.1f)] private float activationTime = 4f;
        [SerializeField, Min(0.1f)] private float miniGameTime = 2f;
        [SerializeField, Min(0f)] private float basePower = 1f;
        [Header("Mini Game Spawn")]
        [SerializeField] private BattleMiniGameSpawnPlacement miniGameSpawnPlacement = BattleMiniGameSpawnPlacement.Default;
        [SerializeField] private Vector2 miniGameSpawnOffset;
        [Header("Random Spawn")]
        [SerializeField] private bool randomSpawnEnabled = true;
        [SerializeField, Min(0.1f)] private float randomMinInterval = 20f;
        [SerializeField, Min(0.1f)] private float randomMaxInterval = 45f;
        [Header("Triggered Spawn")]
        [SerializeField, Min(0f)] private float triggerCooldown = 0f;

        public string Id => id;
        public GameObject ActivatorPrefab => activatorPrefab;
        public GameObject MiniGamePrefab => miniGamePrefab;
        public BattleMiniGameReward Reward => reward;
        public float ActivationTime => activationTime;
        public float MiniGameTime => miniGameTime;
        public float BasePower => basePower;
        public BattleMiniGameSpawnPlacement MiniGameSpawnPlacement => miniGameSpawnPlacement;
        public Vector2 MiniGameSpawnOffset => miniGameSpawnOffset;
        public bool RandomSpawnEnabled => randomSpawnEnabled;
        public float RandomMinInterval => randomMinInterval;
        public float RandomMaxInterval => randomMaxInterval;
        public float TriggerCooldown => triggerCooldown;

        private void OnValidate()
        {
            if (randomMaxInterval < randomMinInterval)
            {
                randomMaxInterval = randomMinInterval;
            }
        }
    }
}
