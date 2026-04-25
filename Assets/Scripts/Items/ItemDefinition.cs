using System;
using System.Collections.Generic;
using LocalizationSupport;
using UnityEngine;

namespace Items
{
    public abstract class ItemDefinition : ScriptableObject
    {
        [SerializeField] [HideInInspector] private string saveDefinitionId;
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 6)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] [Min(1)] private int maxStack = 1;

        public string SaveDefinitionId => string.IsNullOrWhiteSpace(saveDefinitionId) ? $"name:{name}" : saveDefinitionId;
        public string ExplicitSaveDefinitionId => saveDefinitionId;
        public string DisplayName => GameLocalization.LocalizeValueOrKey(
            GameLocalization.ContentTable,
            string.IsNullOrWhiteSpace(displayName) ? name : displayName);
        public string Description => GameLocalization.LocalizeValueOrKey(GameLocalization.ContentTable, description);
        public Sprite Icon => icon;
        public int MaxStack => Mathf.Max(1, maxStack);
        public virtual bool CanBeUsed => false;
        public virtual bool ConsumeOnUse => false;

        public virtual IReadOnlyList<string> GetTooltipDescriptions()
        {
            if (string.IsNullOrWhiteSpace(Description))
                return Array.Empty<string>();

            return new[] { Description };
        }

        public virtual bool TryUse(ItemUseContext context)
        {
            return false;
        }

        public bool EnsureSaveDefinitionId()
        {
            if (!string.IsNullOrWhiteSpace(saveDefinitionId))
                return false;

            saveDefinitionId = Guid.NewGuid().ToString("N");
            return true;
        }

        public void RegenerateSaveDefinitionId()
        {
            saveDefinitionId = Guid.NewGuid().ToString("N");
        }

        protected virtual void OnValidate()
        {
            maxStack = Mathf.Max(1, maxStack);
            EnsureSaveDefinitionId();
        }
    }
}
