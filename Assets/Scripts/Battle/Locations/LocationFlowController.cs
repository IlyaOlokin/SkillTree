using System;
using UnityEngine;
using Zenject;

namespace Battle
{
    public class LocationFlowController : MonoBehaviour
    {
        public enum FlowMode
        {
            Map = 0,
            Battle = 1
        }

        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private GameObject mapRoot;
        [SerializeField] private GameObject battleRoot;
        [SerializeField] private bool startInMapMode = true;

        [Inject] private BattleTickSystem _battleTickSystem;
        [Inject] private PlayerUnit _player;

        private FlowMode _mode;

        public FlowMode Mode => _mode;
        public bool IsMapMode => _mode == FlowMode.Map;
        public bool IsBattleMode => _mode == FlowMode.Battle;

        public event Action<FlowMode> OnModeChanged;

        private void Awake()
        {
            if (enemySpawner == null)
                enemySpawner = FindFirstObjectByType<EnemySpawner>();

            if (enemySpawner == null)
            {
                enabled = false;
                return;
            }

            if (startInMapMode)
            {
                enemySpawner.ExitBattle();
                ApplyMode(FlowMode.Map);
                return;
            }

            ApplyMode(FlowMode.Battle);
        }

        private void Start()
        {
            if (startInMapMode)
                ReturnToMap();
            else
                EnterSelectedLocation();
        }

        public bool SelectLocation(string locationId)
        {
            return enemySpawner != null && enemySpawner.SelectLocation(locationId, false);
        }

        public void EnterSelectedLocation()
        {
            if (enemySpawner == null)
                return;

            _player.gameObject.SetActive(true);
            _player.ResetCombatState();
            _battleTickSystem.Resume();
            enemySpawner.EnterBattle();
            ApplyMode(FlowMode.Battle);
        }

        public void ReturnToMap()
        {
            if (enemySpawner == null)
                return;

            _battleTickSystem.Pause();
            enemySpawner.ExitBattle();
            _player.ResetCombatState();
            _player.gameObject.SetActive(false);
            ApplyMode(FlowMode.Map);
        }

        private void ApplyMode(FlowMode mode)
        {
            _mode = mode;

            if (mapRoot != null)
                mapRoot.SetActive(mode == FlowMode.Map);

            if (battleRoot != null)
                battleRoot.SetActive(mode == FlowMode.Battle);

            OnModeChanged?.Invoke(_mode);
        }
    }
}
