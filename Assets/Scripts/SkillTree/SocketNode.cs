using System;
using System.Collections.Generic;
using Gems;
using LocalizationSupport;
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
        private GemInstance _pendingGem;
        [SerializeField, HideInInspector] private SocketNode _bridgePartner;
        public bool HasPendingBridge => _pendingGem != null;
        public SocketNode BridgePartner => _bridgePartner != null
            && _bridgePartner._bridgePartner == this
            && socketedGem != null && socketedGem.Kind == GemKind.Bridge
            && !string.IsNullOrEmpty(socketedGem.InstanceId)
            && socketedGem.InstanceId == _bridgePartner.socketedGem?.InstanceId ? _bridgePartner : null;
        public GemInstance SavedSocketedGem => socketedGem;

        public event Action<SocketNode> OnSocketedGemChanged;

        public GemInstance SocketedGem => _pendingGem ?? socketedGem;
        public GemInstance DefaultSocketedGem => defaultSocketedGem;
        public bool HasGem => IsValidGem(SocketedGem);
        public bool IsGemActive => IsActive && IsValidGem(socketedGem) && !HasPendingBridge;
        public override bool CanChangePower => false;

        private void Awake()
        {
            RebuildRuntimeGemModifiers();
            if (!TryGetComponent<Visual.BridgeConnectionVisual>(out _))
                gameObject.AddComponent<Visual.BridgeConnectionVisual>();
        }

        public bool CanAcceptGem(GemInstance gemInstance)
        {
            return IsValidGem(gemInstance) && gemInstance.Kind != GemKind.Bridge && !HasGem;
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
            if (HasPendingBridge || socketedGem?.Kind == GemKind.Bridge)
            {
                removedGem = null;
                return false;
            }
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
            if (HasGem)
                return GetSocketedGemDescriptions();

            List<string> descriptions = GetModifierTooltipDescriptions();
            AppendSocketNodeDescription(descriptions);

            descriptions.Add(GameLocalization.Get("node.socket.empty", "Empty Socket"));

            AppendInactiveNoEffectDescription(descriptions);
            return descriptions;
        }

        public override IReadOnlyList<TooltipDescriptionLine> GetTooltipDescriptionLines()
        {
            return GetRequiredTooltipDescriptionLines(GetTooltipDescriptions());
        }

        private List<string> GetSocketedGemDescriptions()
        {
            List<string> descriptions = new(SocketedGem.GetTooltipDescriptions(ModifierPowerContext.FromNode(this)));
            if (HasPendingBridge)
            {
                descriptions.Add(GameLocalization.Get("node.socket.bridge.pending", "Choose the second socket. Right-click to cancel."));
                return descriptions;
            }
            if (BridgePartner != null)
            {
                descriptions.Add(GameLocalization.Format("node.socket.bridge.partner", "Connected to: [[0]]", BridgePartner.name));
                return descriptions;
            }
            if (!IsActive)
            {
                descriptions.Add(GameLocalization.Get(
                    "node.inactiveNoEffect",
                    "This node is inactive and grants no effects"));
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

        private static IReadOnlyList<TooltipDescriptionLine> GetRequiredTooltipDescriptionLines(
            IReadOnlyList<string> descriptions)
        {
            if (descriptions == null || descriptions.Count == 0)
                return Array.Empty<TooltipDescriptionLine>();

            List<TooltipDescriptionLine> lines = new(descriptions.Count);
            for (int i = 0; i < descriptions.Count; i++)
            {
                lines.Add(TooltipDescriptionLine.Required(descriptions[i]));
            }

            return lines;
        }

        private void RebuildRuntimeGemModifiers()
        {
            ClearRuntimeGemModifiers();
            if (!IsValidGem(socketedGem) || socketedGem.Kind != GemKind.LocalModifiers)
                return;

            _runtimeGemModifiers.AddRange(socketedGem.CreateRuntimeModifiers());
        }

        private void ClearRuntimeGemModifiers()
        {
            for (int i = _runtimeGemModifiers.Count - 1; i >= 0; i--)
            {
                GemModifierUtility.DestroyRuntimeModifier(_runtimeGemModifiers[i]);
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
            SetGemState(gemInstance);
            NotifySocketChanged();
        }

        internal void SetGemState(GemInstance gem, SocketNode partner = null)
        {
            _pendingGem = null;
            socketedGem = gem;
            _bridgePartner = partner;
            RebuildRuntimeGemModifiers();
        }

        internal void SetPendingBridge(GemInstance gem)
        {
            _pendingGem = gem;
            NotifySocketChanged();
        }

        internal void PublishGemChange() => NotifySocketChanged();

        protected override void OnValidate()
        {
            base.OnValidate();
            defaultSocketedGem = socketedGem;
        }
    }
}
