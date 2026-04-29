using System;
using System.Collections.Generic;
using Gems;
using LocalizationSupport;
using SkillTree;
using TooltipSystem;
using UnityEngine;

namespace SkillTree
{
    public class SocketNode : Node
    {
        private const string TooltipDescriptionId = "socketnode";
        private const string TooltipTitleLocalizationKey = "node.title.socket";

        [SerializeField] private GemInstance socketedGem;
        [SerializeField] [HideInInspector] private GemInstance defaultSocketedGem;

        private readonly List<Modifier> _runtimeGemModifiers = new();

        public event Action<SocketNode> OnSocketedGemChanged;

        public GemInstance SocketedGem => socketedGem;
        public GemInstance DefaultSocketedGem => defaultSocketedGem;
        public bool HasGem => IsValidGem(socketedGem);
        public bool IsGemActive => IsActive && HasGem;

        private void Awake()
        {
            RebuildRuntimeGemModifiers();
        }

        public bool CanAcceptGem(GemInstance gemInstance)
        {
            return IsActive && IsValidGem(gemInstance) && !HasGem;
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
            AppendSocketNodeDescription(descriptions);

            if (!HasGem)
            {
                descriptions.Add(GameLocalization.Get("node.socket.empty", "Empty Socket"));

                if (!IsActive)
                {
                    descriptions.Add(GameLocalization.Get(
                        "node.socket.allocateBeforeSocketing",
                        "Allocate this node before socketing a {gem|Gem}."));
                }

                return descriptions;
            }

            descriptions.Add(GameLocalization.Format(
                "node.socket.socketedGem",
                "Socketed Gem: [[0]]",
                socketedGem.DisplayName));

            if (!IsActive)
            {
                descriptions.Add(GameLocalization.Get(
                    "node.socket.inactiveGem",
                    "Socketed {gem|Gem} is inactive until this node is allocated."));
            }

            IReadOnlyList<string> gemDescriptions = socketedGem.GetTooltipDescriptions();
            for (int i = 0; i < gemDescriptions.Count; i++)
            {
                descriptions.Add(gemDescriptions[i]);
            }

            return descriptions;
        }

        public override string GetTooltipTitle()
        {
            return GameLocalization.GetModifier(TooltipTitleLocalizationKey, "Socket Node");
        }

        private static void AppendSocketNodeDescription(List<string> descriptions)
        {
            TooltipTermDatabase activeDatabase = TooltipTermDatabase.ActiveDatabase;
            if (activeDatabase == null
                || !activeDatabase.TryGetDescription(TooltipDescriptionId, out TooltipDescriptionData description))
            {
                return;
            }

            IReadOnlyList<string> socketDescriptions = description.Descriptions;
            for (int i = 0; i < socketDescriptions.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(socketDescriptions[i]))
                    descriptions.Add(socketDescriptions[i]);
            }
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

        public void SetSocketedGemFromSave(GemInstance gemInstance)
        {
            socketedGem = gemInstance;
            RebuildRuntimeGemModifiers();
            NotifySocketChanged();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            defaultSocketedGem = socketedGem;
        }
    }
}
