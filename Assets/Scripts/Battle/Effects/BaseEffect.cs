using System;
using System.Collections.Generic;
using System.Globalization;
using LocalizationSupport;
using TooltipSystem;

namespace Battle
{
    public abstract class BaseEffect
    {
        private List<SkillTree.Modifier> _runtimeModifiers;

        protected T CreateRuntimeModifier<T>() where T : SkillTree.Modifier
        {
            T modifier = UnityEngine.ScriptableObject.CreateInstance<T>();
            OwnRuntimeModifier(modifier);
            return modifier;
        }

        protected void OwnRuntimeModifier(SkillTree.Modifier modifier)
        {
            if (modifier == null) return;
            _runtimeModifiers ??= new List<SkillTree.Modifier>();
            if (!_runtimeModifiers.Contains(modifier)) _runtimeModifiers.Add(modifier);
        }

        protected void ReleaseRuntimeModifier(SkillTree.Modifier modifier)
        {
            if (_runtimeModifiers == null || !_runtimeModifiers.Remove(modifier)) return;
            if (modifier != null) UnityEngine.Object.Destroy(modifier);
        }

        // Called by the controller after detaching an effect, or discarding a stack candidate.
        internal void ReleaseRuntimeModifiers()
        {
            if (_runtimeModifiers == null) return;
            foreach (var modifier in _runtimeModifiers)
            {
                if (modifier != null) UnityEngine.Object.Destroy(modifier);
            }
            _runtimeModifiers.Clear();
        }

        public abstract bool IsStackable { get; set; }
        public virtual EffectVisualType VisualType => EffectVisualType.None;
        public virtual bool CanDisplayMultipleIcons => false;
        public float Duration = -1;

        public virtual void OnApply(Unit unit){}
        public virtual void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing){}
        public virtual void OnTick(Unit unit, float deltaTime){}
        public virtual bool IsReadyToBeRemoved(Unit unit)
        {
            return false;
        }
        public virtual void Consume(Unit unit){}
        public virtual void Consume(Unit unit, ActiveEffect activeEffect)
        {
            Consume(unit);
        }
        public virtual void OnRemove(Unit unit){}

        public virtual string GetIconDisplayKey()
        {
            return GetType().FullName;
        }

        public virtual string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (activeEffects == null || activeEffects.Count == 0)
            {
                return string.Empty;
            }

            if (activeEffects.Count > 1)
            {
                return activeEffects.Count.ToString();
            }

            ActiveEffect activeEffect = activeEffects[0];
            if (activeEffect?.Effect != null && activeEffect.Effect.Duration > 0f)
            {
                float timeLeft = Math.Max(0f, activeEffect.TimeLeft);
                return timeLeft.ToString("0.0", CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }
        
        public virtual float GetIconTimerProgress(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (activeEffects == null || activeEffects.Count == 0)
            {
                return 1f;
            }

            float closestProgress = 1f;
            bool hasTimedEffect = false;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                ActiveEffect activeEffect = activeEffects[i];
                if (activeEffect?.Effect == null || activeEffect.Effect.Duration <= 0f)
                {
                    continue;
                }

                float progress = activeEffect.TimeLeft / activeEffect.Effect.Duration;
                if (!hasTimedEffect || progress < closestProgress)
                {
                    closestProgress = progress;
                    hasTimedEffect = true;
                }
            }

            return hasTimedEffect ? closestProgress : 1f;
        }

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
                return description.GetDescriptions(GetDescriptionArguments());
            }

            string fallbackDescription = GetDescriptionFallback();
            if (!string.IsNullOrWhiteSpace(fallbackDescription))
            {
                return new[]
                {
                    GameLocalization.FormatContent(
                        GetDescriptionLocalizationKey(),
                        fallbackDescription,
                        GetDescriptionArguments())
                };
            }

            return new[] { GetDisplayName() };
        }

        protected virtual string GetDescriptionId()
        {
            return GetType().Name;
        }

        protected virtual string GetDescriptionLocalizationKey()
        {
            return $"effect.{GetDescriptionId()}.description";
        }

        protected virtual string GetDescriptionFallback()
        {
            return null;
        }

        protected virtual object[] GetDescriptionArguments()
        {
            return Array.Empty<object>();
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
