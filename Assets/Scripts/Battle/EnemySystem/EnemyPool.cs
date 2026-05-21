using System.Collections.Generic;
using UnityEngine;
using Visual;
using Zenject;

namespace Battle
{
    public class EnemyPool : MonoBehaviour
    {
        [SerializeField] private Unit enemyPrefab;
        [SerializeField] private int poolSize = 3;
        [SerializeField] public AttackResolver attackResolver;
        [SerializeField] private List<Transform> spawnPositions;
        [SerializeField] private List<Vector3> visualScalesBySpawnPosition = new();
        [Inject] private DiContainer _container;

        private List<Unit> _units = new();
        public List<Unit> Units => _units;

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var unit = _container.InstantiatePrefabForComponent<Unit>(enemyPrefab, transform);
                unit.gameObject.SetActive(false);
                unit.gameObject.transform.position = spawnPositions[i].position;
                _units.Add(unit);
            }
            attackResolver.SetNewEnemies(_units);
        }

        public void ApplyVisualScaleForSlot(Unit unit, int slotIndex)
        {
            if (unit == null)
            {
                return;
            }

            UnitVisual visual = unit.GetComponentInChildren<UnitVisual>(true);
            if (visual == null)
            {
                return;
            }

            visual.SetVisualScale(GetVisualScaleForSlot(slotIndex));
        }

        private Vector3 GetVisualScaleForSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= visualScalesBySpawnPosition.Count)
            {
                return Vector3.one;
            }

            Vector3 scale = visualScalesBySpawnPosition[slotIndex];
            return scale == Vector3.zero ? Vector3.one : scale;
        }
    }
}
