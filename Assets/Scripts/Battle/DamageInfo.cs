using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Battle
{
    [Serializable]
    public class DamageInfo
    {
        public Unit Owner { get; private set; }
        public ITarget Target { get; private set; }
        public BaseUnitModifiers BaseUnitModifiers { get; private set; }
        public AttackEffectPayload AttackEffectPayload { get; } = new AttackEffectPayload();
        public DamageInstance DamageInstance { get; } = new DamageInstance();
        [HideInInspector] public bool IsCritical { get; set; }
        [HideInInspector] public bool AllowsMultiCrit { get; set; }
        [HideInInspector] public int CriticalLayerCount { get; set; }
        [HideInInspector] public float HealthDamageTaken { get; private set; }
        [HideInInspector] public Unit AppliedChillTarget { get; private set; }
        [HideInInspector] public Func<BaseEffect> ChillAfterFreezeFactory { get; private set; }

        public DamageInfo(Unit owner, BaseUnitModifiers baseUnitModifiersSnapshot)
        {
            Reset(owner, baseUnitModifiersSnapshot);
        }

        public void Reset(Unit owner, BaseUnitModifiers baseUnitModifiersSnapshot)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Target = owner.attacker.Target;
            BaseUnitModifiers = baseUnitModifiersSnapshot ?? throw new ArgumentNullException(nameof(baseUnitModifiersSnapshot));
            AttackEffectPayload.Reset();
            DamageInstance.ResetValues();
            IsCritical = false;
            AllowsMultiCrit = false;
            CriticalLayerCount = 0;
            HealthDamageTaken = 0f;
            AppliedChillTarget = null;
            ChillAfterFreezeFactory = null;
        }

        public void SetHealthDamageTaken(float healthDamageTaken)
        {
            HealthDamageTaken = Mathf.Max(0f, healthDamageTaken);
        }

        public void RegisterAppliedChill(Unit target, Func<BaseEffect> chillAfterFreezeFactory)
        {
            AppliedChillTarget = target;
            ChillAfterFreezeFactory = chillAfterFreezeFactory;
        }

        public void ClearAppliedChill()
        {
            AppliedChillTarget = null;
            ChillAfterFreezeFactory = null;
        }
    }

    public class DamageInstance
    {
        public Dictionary<DamageType, float> Damage = new Dictionary<DamageType, float>();

        public DamageInstance()
        {
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                Damage[damageType] = 0f;
            }
        }

        public void ResetValues()
        {
            var keys = Damage.Keys.ToList();
            foreach (var key in keys)
            {
                Damage[key] = 0f;
            }
        }
    }
}
