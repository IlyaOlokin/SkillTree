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

        public int MaxAllocatedNode => limitMode == LimitMode.PlayerLevel
            ? GetMaxAllocatedNodeForPlayerLevel()
            : maxAllocatedNode;

        public bool UsesPlayerLevelLimit => limitMode == LimitMode.PlayerLevel;
        public int CurrentAllocatedCount { get; private set; }
        
        public event Action OnAllocatedCountChanged;


        private void Awake()
        {
            foreach (var node in nodes)
            {
                if (node == null)
                    continue;

                node.OnAllocatedChanged += OnNodeChanged;
                node.AdditionalAllocatedCondition = LimitCondition;
                node.AdditionalActivationCondition = ActivationCondition;
            }

            if (_unitLevel != null)
                _unitLevel.OnExpChanged += OnPlayerProgressChanged;

            RebuildAllocationOrderFromNodes();
            _lastMaxAllocatedNode = MaxAllocatedNode;
            RebuildActiveNodes();
        }

        private void OnDestroy()
        {
            if (_unitLevel != null)
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
            return CurrentAllocatedCount < MaxAllocatedNode;
        }

        private bool ActivationCondition()
        {
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

            RebuildActiveNodes();
            OnAllocatedCountChanged?.Invoke();
        }

        private void RebuildActiveNodes()
        {
            int activeSlots = MaxAllocatedNode;
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
                int currentMaxAllocatedNode = MaxAllocatedNode;
                if (currentMaxAllocatedNode != _lastMaxAllocatedNode)
                {
                    _lastMaxAllocatedNode = currentMaxAllocatedNode;
                    RebuildAllocationOrderFromNodes();
                    RebuildActiveNodes();
                }

                OnAllocatedCountChanged?.Invoke();
            }
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
