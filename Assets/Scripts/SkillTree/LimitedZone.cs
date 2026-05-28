using System;
using System.Collections.Generic;
using Battle;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace SkillTree
{
    public class LimitedZone : MonoBehaviour, ITooltipDescriptionProvider
    {
        private const string TooltipTitleLocalizationKey = "ui.limitedZone.tooltip.title";
        private const string TooltipLimitLocalizationKey = "ui.limitedZone.tooltip.limit";
        private const string TooltipPlayerLevelLimitLocalizationKey = "ui.limitedZone.tooltip.playerLevelLimit";
        private static readonly List<LimitedZone> ActiveZones = new();

        [Inject(Optional = true)] private UnitLevel _unitLevel;

        [SerializeField] private List<Node> nodes = new List<Node>();
        [SerializeField] private LimitMode limitMode = LimitMode.Fixed;
        [SerializeField, FormerlySerializedAs("<MaxAllocatedNode>k__BackingField")] private int maxAllocatedNode = 1;
        [SerializeField] private List<PlayerLevelLimitRule> playerLevelLimitRules = new()
        {
            new PlayerLevelLimitRule(1, 0),
            new PlayerLevelLimitRule(25, 2),
            new PlayerLevelLimitRule(50, 4)
        };

        private readonly List<Node> _allocationOrder = new();
        private int _lastMaxAllocatedNode;
        private bool _playerLevelLimitInitialized;
        private int _saveDataRestoreDepth;
        private bool _isSubscribedToPlayerProgress;

        public int MaxAllocatedNode => limitMode == LimitMode.PlayerLevel
            ? GetMaxAllocatedNodeForPlayerLevel()
            : maxAllocatedNode;

        public bool UsesPlayerLevelLimit => limitMode == LimitMode.PlayerLevel;
        public int CurrentAllocatedCount { get; private set; }
        
        public event Action OnAllocatedCountChanged;


        private void Awake()
        {
            if (!ActiveZones.Contains(this))
                ActiveZones.Add(this);

            foreach (var node in nodes)
            {
                if (node == null)
                    continue;

                node.OnAllocatedChanged += OnNodeChanged;
                node.AdditionalAllocatedCondition = LimitCondition;
                node.AdditionalActivationCondition = ActivationCondition;
            }

            SubscribeToPlayerProgressIfPossible();

            _playerLevelLimitInitialized = limitMode != LimitMode.PlayerLevel || _unitLevel == null;
            RebuildAllocationOrderFromNodes();
            _lastMaxAllocatedNode = MaxAllocatedNode;
            RebuildActiveNodes();
        }

        private void Start()
        {
            SubscribeToPlayerProgressIfPossible();
            RefreshFromCurrentNodes();
        }

        private void OnDestroy()
        {
            ActiveZones.Remove(this);

            if (_unitLevel != null && _isSubscribedToPlayerProgress)
                _unitLevel.OnExpChanged -= OnPlayerProgressChanged;

            foreach (var node in nodes)
            {
                if (node == null)
                    continue;

                node.OnAllocatedChanged -= OnNodeChanged;
                if (node.AdditionalAllocatedCondition == LimitCondition)
                    node.AdditionalAllocatedCondition = null;
                if (node.AdditionalActivationCondition == ActivationCondition)
                    node.AdditionalActivationCondition = null;
            }
        }

        private bool LimitCondition()
        {
            if (_saveDataRestoreDepth > 0)
                return true;

            if (!IsLimitReady())
                return true;

            return CurrentAllocatedCount < MaxAllocatedNode;
        }

        private bool ActivationCondition()
        {
            if (_saveDataRestoreDepth > 0)
                return true;

            if (!IsLimitReady())
                return true;

            return CurrentAllocatedCount < MaxAllocatedNode;
        }

        private void OnNodeChanged(Node node)
        {
            if (node != null)
            {
                if (node.IsAllocated)
                {
                    if (!_allocationOrder.Contains(node))
                        _allocationOrder.Add(node);
                }
                else
                {
                    _allocationOrder.Remove(node);
                }
            }

            if (_saveDataRestoreDepth > 0)
                return;

            RebuildActiveNodes();
            OnAllocatedCountChanged?.Invoke();
        }

        private void RebuildActiveNodes()
        {
            int activeSlots = IsLimitReady() ? MaxAllocatedNode : int.MaxValue;
            int activeCount = 0;

            foreach (var node in nodes)
            {
                if (node != null && !_allocationOrder.Contains(node))
                    node.SetActiveFromLimitZone(false);
            }

            foreach (var node in _allocationOrder)
            {
                if (node == null || !node.IsAllocated)
                    continue;

                bool shouldBeActive = activeSlots > 0 && node.HasActiveRootConnection();
                if (shouldBeActive)
                {
                    activeSlots--;
                    activeCount++;
                }

                node.SetActiveFromLimitZone(shouldBeActive);
            }

            CurrentAllocatedCount = activeCount;
        }

        private void RebuildAllocationOrderFromNodes()
        {
            _allocationOrder.Clear();

            foreach (var node in nodes)
            {
                if (node != null && node.IsAllocated)
                    _allocationOrder.Add(node);
            }
        }

        private int GetMaxAllocatedNodeForPlayerLevel()
        {
            int playerLevel = _unitLevel != null ? _unitLevel.Level : 1;
            int maxAllocated = 0;

            if (playerLevelLimitRules == null)
                return maxAllocated;

            foreach (var rule in playerLevelLimitRules)
            {
                if (rule == null || playerLevel < rule.RequiredPlayerLevel)
                    continue;

                maxAllocated = Mathf.Max(maxAllocated, rule.MaxAllocatedNode);
            }

            return maxAllocated;
        }

        public bool TryGetNextPlayerLevelLimit(out int requiredPlayerLevel, out int nextMaxAllocatedNode)
        {
            requiredPlayerLevel = 0;
            nextMaxAllocatedNode = 0;

            if (limitMode != LimitMode.PlayerLevel || playerLevelLimitRules == null)
                return false;

            int playerLevel = _unitLevel != null ? _unitLevel.Level : 1;
            int currentMaxAllocatedNode = MaxAllocatedNode;

            foreach (var rule in playerLevelLimitRules)
            {
                if (rule == null ||
                    rule.RequiredPlayerLevel <= playerLevel ||
                    rule.MaxAllocatedNode <= currentMaxAllocatedNode)
                {
                    continue;
                }

                if (requiredPlayerLevel == 0 || rule.RequiredPlayerLevel < requiredPlayerLevel)
                {
                    requiredPlayerLevel = rule.RequiredPlayerLevel;
                    nextMaxAllocatedNode = rule.MaxAllocatedNode;
                }
            }

            return requiredPlayerLevel > 0;
        }

        public string GetTooltipTitle()
        {
            return GameLocalization.GetGameUI(TooltipTitleLocalizationKey, "Limited Zone");
        }

        public bool ShouldShowTooltipTitle()
        {
            return true;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            List<string> descriptions = new List<string>()
            {
                GameLocalization.FormatGameUI(
                    TooltipLimitLocalizationKey,
                    "Only [[0]] nodes in this zone can be active.",
                    MaxAllocatedNode)
            };

            if (limitMode == LimitMode.PlayerLevel)
            {
                descriptions.Add(GameLocalization.GetGameUI(
                    TooltipPlayerLevelLimitLocalizationKey,
                    "Reach higher levels to expand the limit."));
            }

            return descriptions;
        }

        private void OnPlayerProgressChanged()
        {
            if (limitMode == LimitMode.PlayerLevel)
            {
                bool wasInitialized = _playerLevelLimitInitialized;
                _playerLevelLimitInitialized = true;
                int currentMaxAllocatedNode = MaxAllocatedNode;
                if (_saveDataRestoreDepth == 0 && (!wasInitialized || currentMaxAllocatedNode != _lastMaxAllocatedNode))
                {
                    _lastMaxAllocatedNode = currentMaxAllocatedNode;
                    RebuildAllocationOrderFromNodes();
                    RebuildActiveNodes();
                }

                OnAllocatedCountChanged?.Invoke();
            }
        }

        private void SubscribeToPlayerProgressIfPossible()
        {
            if (_unitLevel == null || _isSubscribedToPlayerProgress)
                return;

            _unitLevel.OnExpChanged += OnPlayerProgressChanged;
            _isSubscribedToPlayerProgress = true;
        }

        public static void BeginSaveDataRestore()
        {
            for (int i = 0; i < ActiveZones.Count; i++)
                ActiveZones[i]?.BeginSaveDataRestoreInternal();
        }

        public static void EndSaveDataRestore()
        {
            for (int i = 0; i < ActiveZones.Count; i++)
                ActiveZones[i]?.EndSaveDataRestoreInternal();
        }

        private void BeginSaveDataRestoreInternal()
        {
            _saveDataRestoreDepth++;
        }

        private void EndSaveDataRestoreInternal()
        {
            if (_saveDataRestoreDepth <= 0)
                return;

            _saveDataRestoreDepth--;
            if (_saveDataRestoreDepth > 0)
                return;

            RefreshFromCurrentNodes();
        }

        private void RefreshFromCurrentNodes()
        {
            if (limitMode == LimitMode.PlayerLevel)
                _playerLevelLimitInitialized = true;

            RebuildAllocationOrderFromNodes();
            _lastMaxAllocatedNode = MaxAllocatedNode;
            RebuildActiveNodes();
            OnAllocatedCountChanged?.Invoke();
        }

        private bool IsLimitReady()
        {
            return limitMode != LimitMode.PlayerLevel || _playerLevelLimitInitialized;
        }

        private enum LimitMode
        {
            Fixed,
            PlayerLevel
        }

        [Serializable]
        private class PlayerLevelLimitRule
        {
            [SerializeField, Min(1)] private int requiredPlayerLevel = 1;
            [SerializeField, Min(0)] private int maxAllocatedNode;

            public int RequiredPlayerLevel => requiredPlayerLevel;
            public int MaxAllocatedNode => maxAllocatedNode;

            public PlayerLevelLimitRule(int requiredPlayerLevel, int maxAllocatedNode)
            {
                this.requiredPlayerLevel = Mathf.Max(1, requiredPlayerLevel);
                this.maxAllocatedNode = Mathf.Max(0, maxAllocatedNode);
            }
        }
    }
}
