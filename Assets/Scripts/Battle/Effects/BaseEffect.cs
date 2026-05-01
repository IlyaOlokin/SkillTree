using System;
using System.Collections.Generic;
using LocalizationSupport;
using TooltipSystem;

namespace Battle
{
    public abstract class BaseEffect
    {
        public abstract bool IsStackable { get; set; }
        public virtual EffectVisualType VisualType => EffectVisualType.None;
        public float Duration = -1;

        public virtual void OnApply(Unit unit){}
        public virtual void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing){}
        public virtual void OnTick(Unit unit, float deltaTime){}
        public virtual bool IsReadyToBeRemoved(Unit unit)
        {
            return false;
        }
        public virtual void Consume(Unit unit){}
        public virtual void OnRemove(Unit unit){}

        public virtual TooltipDescriptionData GetDescription()
        {
            TooltipTermDatabase activeDatabase = TooltipTermDatabase.ActiveDatabase;
            if (activeDatabase == null)
            {
                return null;
            }

            activeDatabase.TryGetDescription(GetDescriptionId(), out TooltipDescriptionData description);
            return description;
        }

        public virtual IReadOnlyList<string> GetTooltipDescriptions()
        {
            TooltipDescriptionData description = GetDescription();
            if (description != null && description.Descriptions.Count > 0)
            {
                return description.Descriptions;
            }

            return new[] { GetDisplayName() };
        }

        protected virtual string GetDescriptionId()
        {
            return GetType().Name;
        }

        protected virtual string GetDisplayName()
        {
            string typeName = GetType().Name;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return string.Empty;
            }

            return GameLocalization.GetContent(
                $"effect.{GetDescriptionId()}.name",
                GameLocalization.HumanizeIdentifier(typeName));
        }
    }
}



