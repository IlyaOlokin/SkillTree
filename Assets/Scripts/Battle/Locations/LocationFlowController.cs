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
            Battle = 1,
            Shop = 2
        }

        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private GameObject mapRoot;
        [SerializeField] private GameObject battleRoot;
        [SerializeField] private GameObject shopRoot;
        [SerializeField] private bool startInMapMode = true;

        [Inject] private BattleTickSystem _battleTickSystem;
        [Inject] private PlayerUnit _player;
        [Inject] private SaveSystem.GameSaveCoordinator _saveCoordinator;
        [Inject] private Tutorials.TutorialService _tutorials;

        private FlowMode _mode;

        public FlowMode Mode => _mode;
        public bool IsMapMode => _mode == FlowMode.Map;
        public bool IsBattleMode => _mode == FlowMode.Battle;
        public bool IsShopMode => _mode == FlowMode.Shop;

        public event Action<FlowMode> OnModeChanged;
        public event Action<LocationDefinition> OnShopEntered;

        private void Awake()
        {
            if (enemySpawner == null)
                enemySpawner = FindAnyObjectByType<EnemySpawner>();

            if (enemySpawner == null)
            {
                enabled = false;
                return;
            }

            if (startInMapMode)
            {
                enemySpawner.ExitBattle();
                _mode = FlowMode.Map;
                return;
            }

            ApplyMode(FlowMode.Battle);
        }

        private void Start()
        {
            if (startInMapMode && !_saveCoordinator.IsFreshProfile)
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

            if (enemySpawner.SelectedLocation != null && enemySpawner.SelectedLocation.IsShop)
            {
                _battleTickSystem.Pause();
                enemySpawner.ExitBattle();
                _player.gameObject.SetActive(true);
                _player.RequestModRecalculation();
                _player.ResetCombatState();
                ApplyMode(FlowMode.Shop);
                OnShopEntered?.Invoke(enemySpawner.SelectedLocation);
                return;
            }

            _player.gameObject.SetActive(true);
            _player.RequestModRecalculation();
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
            _player.gameObject.SetActive(true);
            _player.RequestModRecalculation();
            _player.ResetCombatState();
            ApplyMode(FlowMode.Map);
            // Report only after the map is visible and the first boss has been defeated.
            // Completion/skip state and pending presentation are persisted by TutorialService.
            if (enemySpawner.IsLocationCompleted("level-1"))
                _tutorials.Report(Tutorials.TutorialEvents.WorldMapOpened, eventLocationId: "level-1");
        }

        private void ApplyMode(FlowMode mode)
        {
            _mode = mode;

            if (mapRoot != null)
                mapRoot.SetActive(mode == FlowMode.Map);

            if (battleRoot != null)
                battleRoot.SetActive(mode == FlowMode.Battle);

            if (shopRoot != null)
                shopRoot.SetActive(mode == FlowMode.Shop);

            OnModeChanged?.Invoke(_mode);
        }
    }
}
