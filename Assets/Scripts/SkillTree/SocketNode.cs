using System;
using System.Collections.Generic;
using Gems;
using SkillTree;
using UnityEngine;

namespace SkillTree
{
    public class SocketNode : Node
    {
        [SerializeField] private GemInstance socketedGem;

        private readonly List<Modifier> _runtimeGemModifiers = new();

        public event Action<SocketNode> OnSocketedGemChanged;

        public GemInstance SocketedGem => socketedGem;
        public bool HasGem => IsValidGem(socketedGem);
        public bool IsGemActive => IsAllocated && HasGem;

        private void Awake()
        {
            RebuildRuntimeGemModifiers();
        }

        public bool CanAcceptGem(GemInstance gemInstance)
        {
            return IsAllocated && IsValidGem(gemInstance) && !HasGem;
        }

        public bool TryInsertGem(GemInstance gemInstance)
        {
            if (!CanAcceptGem(gemInstance))
                return false;

            socketedGem = gemInstance;
            RebuildRuntimeGemModifiers();
            NotifySocketChanged();
            return true;
        }

        public bool TryRemoveGem(out GemInstance removedGem)
        {
            removedGem = socketedGem;
            if (!HasGem)
            {
                removedGem = null;
                return false;
            }

            socketedGem = null;
            ClearRuntimeGemModifiers();
            NotifySocketChanged();
            return true;
        }

        public IReadOnlyList<Modifier> GetActiveModifiers()
        {
            if (!IsGemActive || socketedGem.Kind != GemKind.LocalModifiers)
                return Array.Empty<Modifier>();

            return _runtimeGemModifiers;
        }

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            List<string> descriptions = new(base.GetTooltipDescriptions());

            if (!HasGem)
            {
                descriptions.Add("Empty Socket");

                if (!IsAllocated)
                    descriptions.Add("Allocate this node before socketing a {gem|Gem}.");

                return descriptions;
            }

            descriptions.Add($"Socketed Gem: {socketedGem.DisplayName}");

            if (!IsAllocated)
                descriptions.Add("Socketed {gem} is inactive until this node is allocated.");

            IReadOnlyList<string> gemDescriptions = socketedGem.GetTooltipDescriptions();
            for (int i = 0; i < gemDescriptions.Count; i++)
            {
                descriptions.Add(gemDescriptions[i]);
            }

            return descriptions;
        }

        private void RebuildRuntimeGemModifiers()
        {
            ClearRuntimeGemModifiers();
            if (!HasGem || socketedGem.Kind != GemKind.LocalModifiers)
                return;

            _runtimeGemModifiers.AddRange(socketedGem.CreateRuntimeModifiers());
        }

        private void ClearRuntimeGemModifiers()
        {
            for (int i = _runtimeGemModifiers.Count - 1; i >= 0; i--)
            {
                ModifierRollUtility.DestroyModifier(_runtimeGemModifiers[i]);
            }

            _runtimeGemModifiers.Clear();
        }

        private void NotifySocketChanged()
        {
            OnSocketedGemChanged?.Invoke(this);
            RaiseNodeChanged();
        }

        private static bool IsValidGem(GemInstance gemInstance)
        {
            return gemInstance != null && gemInstance.Definition != null;
        }
    }
}
