using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle.MiniGames
{
    public sealed class BattleMiniGameController : MonoBehaviour
    {
        private static readonly List<BattleMiniGameController> ActiveControllers =
            new List<BattleMiniGameController>();

        [SerializeField] private Unit player;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private Transform activatorParent;
        [SerializeField] private Transform miniGameParent;
        [SerializeField] private CanvasGroup blockedInterfaceGroup;
        [SerializeField] private GameObject miniGameOverlayRoot;
        [Header("Activator Spawn")]
        [SerializeField] private bool randomizeActivatorPosition;
        [SerializeField] private Vector2 activatorMinAnchoredPosition = new Vector2(-300f, -150f);
        [SerializeField] private Vector2 activatorMaxAnchoredPosition = new Vector2(300f, 150f);
        [SerializeField] private List<BattleMiniGameEventDefinition> initialEvents =
            new List<BattleMiniGameEventDefinition>();

        private readonly List<BattleMiniGameActivator> _activators =
            new List<BattleMiniGameActivator>();
        private readonly Dictionary<BattleMiniGameEventDefinition, float> _triggerCooldownEnds =
            new Dictionary<BattleMiniGameEventDefinition, float>();

        private GameObject _activeMiniGameObject;
        private IBattleMiniGameView _activeMiniGameView;
        private BattleMiniGameRunContext _activeContext;
        private bool _activeResultResolved;
        private bool _isBattleActive;

        public Unit Player => player;
        public BattleMiniGameRules Rules { get; } = new BattleMiniGameRules();
        public bool IsMiniGameActive => _activeMiniGameView != null;
        public bool IsBattleActive => _isBattleActive;

        public event Action<BattleMiniGameEventDefinition> EventSpawned;
        public event Action<BattleMiniGameEventDefinition, BattleMiniGameResult> EventResolved;
        public event Action<BattleMiniGameEventDefinition> EventExpired;

        public static BattleMiniGameController For(Unit unit)
        {
            if (unit == null)
            {
                return null;
            }

            for (int i = 0; i < ActiveControllers.Count; i++)
            {
                BattleMiniGameController controller = ActiveControllers[i];
                if (controller != null && controller.Player == unit)
                {
                    return controller;
                }
            }

            return null;
        }

        private void OnEnable()
        {
            if (!ActiveControllers.Contains(this))
            {
                ActiveControllers.Add(this);
            }

            if (enemySpawner != null)
            {
                enemySpawner.OnBattleActivityChanged += SetBattleActive;
                SetBattleActive(enemySpawner.IsBattleActive);
            }

            SetOverlayActive(false);
        }

        private void OnDisable()
        {
            ActiveControllers.Remove(this);

            if (enemySpawner != null)
            {
                enemySpawner.OnBattleActivityChanged -= SetBattleActive;
            }

            ClearActivators();
            EndActiveMiniGame(BattleMiniGameResult.Cancelled(), false);
        }

        private void Update()
        {
            bool pauseActivators = IsMiniGameActive;
            for (int i = _activators.Count - 1; i >= 0; i--)
            {
                BattleMiniGameActivator activator = _activators[i];
                if (activator == null)
                {
                    _activators.RemoveAt(i);
                    continue;
                }

                activator.Tick(Time.deltaTime, pauseActivators);
            }
        }

        public void SetPlayer(Unit value)
        {
            player = value;
        }

        public void SetBattleActive(bool active)
        {
            _isBattleActive = active;
            if (!active)
            {
                ClearActivators();
                EndActiveMiniGame(BattleMiniGameResult.Cancelled(), false);
            }
        }

        public bool TrySpawnRandom()
        {
            BattleMiniGameEventDefinition definition = PickRandomAvailableEvent();
            return TrySpawn(definition);
        }

        public bool TrySpawnRandom(BattleMiniGameEventDefinition definition)
        {
            return definition != null
                && definition.RandomSpawnEnabled
                && IsEventAvailable(definition)
                && TrySpawn(definition);
        }

        public bool TrySpawnById(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            BattleMiniGameEventDefinition definition = FindAvailableEventById(eventId);
            return TrySpawn(definition);
        }

        public bool TrySpawnTriggeredById(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            BattleMiniGameEventDefinition definition = FindAvailableEventById(eventId);
            return TrySpawnTriggered(definition);
        }

        public bool TrySpawnTriggered(BattleMiniGameEventDefinition definition)
        {
            if (definition == null || IsTriggerOnCooldown(definition))
            {
                return false;
            }

            bool spawned = TrySpawn(definition);
            if (spawned && definition.TriggerCooldown > 0f)
            {
                _triggerCooldownEnds[definition] = Time.time + definition.TriggerCooldown;
            }

            return spawned;
        }

        public bool IsTriggerOnCooldown(BattleMiniGameEventDefinition definition)
        {
            return GetTriggerCooldownLeft(definition) > 0f;
        }

        public float GetTriggerCooldownLeft(BattleMiniGameEventDefinition definition)
        {
            if (definition == null || !_triggerCooldownEnds.TryGetValue(definition, out float endTime))
            {
                return 0f;
            }

            float timeLeft = endTime - Time.time;
            if (timeLeft <= 0f)
            {
                _triggerCooldownEnds.Remove(definition);
                return 0f;
            }

            return timeLeft;
        }

        public bool TrySpawn(BattleMiniGameEventDefinition definition)
        {
            if (!_isBattleActive || definition == null || definition.ActivatorPrefab == null)
            {
                return false;
            }

            Transform parent = activatorParent != null ? activatorParent : transform;
            GameObject instance = Instantiate(definition.ActivatorPrefab, parent);
            BattleMiniGameActivator activator = instance.GetComponent<BattleMiniGameActivator>();
            if (activator == null)
            {
                activator = instance.AddComponent<BattleMiniGameActivator>();
            }

            PositionActivator(instance);
            activator.Bind(this, definition, Rules.GetActivationTime(definition));
            activator.Expired += HandleActivatorExpired;
            _activators.Add(activator);

            EventSpawned?.Invoke(definition);
            return true;
        }

        public bool TryActivate(BattleMiniGameActivator activator)
        {
            if (activator == null || IsMiniGameActive)
            {
                return false;
            }

            BattleMiniGameEventDefinition definition = activator.Definition;
            if (definition == null || definition.MiniGamePrefab == null)
            {
                return false;
            }

            Transform parent = miniGameParent != null ? miniGameParent : transform;
            _activeMiniGameObject = Instantiate(definition.MiniGamePrefab, parent);
            PositionMiniGame(_activeMiniGameObject, activator, definition);
            RemoveActivator(activator, true);

            _activeMiniGameView = FindMiniGameView(_activeMiniGameObject);
            if (_activeMiniGameView == null)
            {
                Debug.LogError($"Mini-game prefab '{definition.MiniGamePrefab.name}' has no IBattleMiniGameView component.", this);
                EndActiveMiniGame(BattleMiniGameResult.Fail(), false);
                return false;
            }

            _activeContext = new BattleMiniGameRunContext(
                definition,
                player,
                Rules.GetPower(definition),
                Rules.GetMiniGameTime(definition));

            _activeResultResolved = false;
            _activeContext.Completed += HandleMiniGameResult;
            _activeMiniGameView.Completed += HandleMiniGameViewCompleted;

            SetOverlayActive(true);
            _activeMiniGameView.StartGame(_activeContext);
            return true;
        }

        private void HandleMiniGameResult(BattleMiniGameResult result)
        {
            ResolveActiveMiniGameResult(result, true);
        }

        private void HandleMiniGameViewCompleted(BattleMiniGameResult result)
        {
            EndActiveMiniGame(result, true);
        }

        private void EndActiveMiniGame(BattleMiniGameResult result, bool applyReward)
        {
            ResolveActiveMiniGameResult(result, applyReward);

            if (_activeContext != null)
            {
                _activeContext.Completed -= HandleMiniGameResult;
            }

            if (_activeMiniGameView != null)
            {
                _activeMiniGameView.Completed -= HandleMiniGameViewCompleted;
            }

            if (_activeMiniGameObject != null)
            {
                Destroy(_activeMiniGameObject);
            }

            _activeMiniGameObject = null;
            _activeMiniGameView = null;
            _activeContext = null;
            _activeResultResolved = false;
            SetOverlayActive(false);
        }

        private void ResolveActiveMiniGameResult(BattleMiniGameResult result, bool applyReward)
        {
            if (_activeContext == null || _activeResultResolved)
            {
                return;
            }

            _activeResultResolved = true;

            BattleMiniGameEventDefinition definition = _activeContext.Definition;
            float power = _activeContext.Power;

            if (applyReward && result.IsSuccess && definition?.Reward != null && player != null)
            {
                var rewardContext = new BattleMiniGameRewardContext(definition, result, power, 1f);
                definition.Reward.Apply(player, rewardContext);
            }

            if (definition != null)
            {
                EventResolved?.Invoke(definition, result);
            }
        }

        private void HandleActivatorExpired(BattleMiniGameActivator activator)
        {
            BattleMiniGameEventDefinition definition = activator != null ? activator.Definition : null;
            RemoveActivator(activator, true);

            if (definition?.Reward != null && player != null && Rules.IgnoreRewardPercent > 0f)
            {
                var context = new BattleMiniGameRewardContext(
                    definition,
                    BattleMiniGameResult.Cancelled(),
                    Rules.GetPower(definition),
                    1f);

                definition.Reward.ApplyPartial(player, context, Rules.IgnoreRewardPercent);
            }

            if (definition != null)
            {
                EventExpired?.Invoke(definition);
            }
        }

        private BattleMiniGameEventDefinition PickRandomAvailableEvent()
        {
            List<BattleMiniGameEventDefinition> available = GetAvailableEvents();
            for (int i = available.Count - 1; i >= 0; i--)
            {
                BattleMiniGameEventDefinition definition = available[i];
                if (definition == null || !definition.RandomSpawnEnabled)
                {
                    available.RemoveAt(i);
                }
            }

            if (available.Count == 0)
            {
                return null;
            }

            return available[UnityEngine.Random.Range(0, available.Count)];
        }

        private BattleMiniGameEventDefinition FindAvailableEventById(string eventId)
        {
            List<BattleMiniGameEventDefinition> available = GetAvailableEvents();
            for (int i = 0; i < available.Count; i++)
            {
                BattleMiniGameEventDefinition definition = available[i];
                if (definition != null && definition.Id == eventId)
                {
                    return definition;
                }
            }

            return null;
        }

        public List<BattleMiniGameEventDefinition> GetAvailableEvents()
        {
            var result = new List<BattleMiniGameEventDefinition>();

            for (int i = 0; i < initialEvents.Count; i++)
            {
                BattleMiniGameEventDefinition definition = initialEvents[i];
                if (definition != null && !result.Contains(definition))
                {
                    result.Add(definition);
                }
            }

            Rules.AppendUnlockedEventsTo(result);
            return result;
        }

        public bool IsEventAvailable(BattleMiniGameEventDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            List<BattleMiniGameEventDefinition> availableEvents = GetAvailableEvents();
            return availableEvents.Contains(definition);
        }

        private void RemoveActivator(BattleMiniGameActivator activator, bool destroy)
        {
            if (activator == null)
            {
                return;
            }

            activator.Expired -= HandleActivatorExpired;
            _activators.Remove(activator);

            if (destroy)
            {
                Destroy(activator.gameObject);
            }
        }

        private void ClearActivators()
        {
            for (int i = _activators.Count - 1; i >= 0; i--)
            {
                BattleMiniGameActivator activator = _activators[i];
                if (activator != null)
                {
                    activator.Expired -= HandleActivatorExpired;
                    Destroy(activator.gameObject);
                }
            }

            _activators.Clear();
        }

        private void SetOverlayActive(bool active)
        {
            if (miniGameOverlayRoot != null)
            {
                miniGameOverlayRoot.SetActive(active);
            }

            if (blockedInterfaceGroup != null)
            {
                blockedInterfaceGroup.interactable = !active;
                blockedInterfaceGroup.blocksRaycasts = !active;
            }
        }

        private static IBattleMiniGameView FindMiniGameView(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IBattleMiniGameView view)
                {
                    return view;
                }
            }

            return null;
        }

        private void PositionActivator(GameObject activatorObject)
        {
            if (!randomizeActivatorPosition || activatorObject == null)
            {
                return;
            }

            float x = UnityEngine.Random.Range(activatorMinAnchoredPosition.x, activatorMaxAnchoredPosition.x);
            float y = UnityEngine.Random.Range(activatorMinAnchoredPosition.y, activatorMaxAnchoredPosition.y);
            Vector2 position = new Vector2(x, y);

            if (activatorObject.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = position;
                return;
            }

            activatorObject.transform.localPosition = position;
        }

        private static void PositionMiniGame(
            GameObject miniGameObject,
            BattleMiniGameActivator activator,
            BattleMiniGameEventDefinition definition)
        {
            if (miniGameObject == null
                || activator == null
                || definition == null
                || definition.MiniGameSpawnPlacement == BattleMiniGameSpawnPlacement.Default)
            {
                return;
            }

            if (definition.MiniGameSpawnPlacement != BattleMiniGameSpawnPlacement.Activator)
            {
                return;
            }

            RectTransform miniGameRect = miniGameObject.transform as RectTransform;
            RectTransform activatorRect = activator.transform as RectTransform;
            if (miniGameRect == null || activatorRect == null)
            {
                miniGameObject.transform.position = activator.transform.position;
                return;
            }

            RectTransform parentRect = miniGameRect.parent as RectTransform;
            if (parentRect == null)
            {
                miniGameRect.position = activatorRect.position;
                return;
            }

            Canvas activatorCanvas = activatorRect.GetComponentInParent<Canvas>();
            Camera activatorCamera = activatorCanvas != null && activatorCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? activatorCanvas.worldCamera
                : null;

            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(activatorCamera, activatorRect.position);

            Canvas parentCanvas = parentRect.GetComponentInParent<Canvas>();
            Camera parentCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? parentCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPosition,
                    parentCamera,
                    out Vector2 localPoint))
            {
                miniGameRect.anchoredPosition = localPoint + definition.MiniGameSpawnOffset;
            }
        }
    }
}
