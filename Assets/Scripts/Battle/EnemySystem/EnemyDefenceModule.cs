using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Modules/Defence Module")]
    public class EnemyDefenceModule : ScriptableObject
    {
        [SerializeField] private EnemyDefenceWeights weights = new();

        public EnemyDefenceWeights Weights => weights;

        private void OnValidate()
        {
            weights ??= new EnemyDefenceWeights();
            weights.NormalizeWeights();
        }

        public void AddEntries(List<EnemyStatWeightEntry> entries)
        {
            weights?.AddEntries(entries);
        }
    }
}
