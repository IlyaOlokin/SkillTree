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
        public BaseUnitModifiers BaseUnitModifiers { get; private set; }
        public DamageInstance DamageInstance { get; } = new DamageInstance();
        [HideInInspector] public bool IsCritical { get; set; }

        public DamageInfo(Unit owner, BaseUnitModifiers baseUnitModifiersSnapshot)
        {
            Reset(owner, baseUnitModifiersSnapshot);
        }

        public void Reset(Unit owner, BaseUnitModifiers baseUnitModifiersSnapshot)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            BaseUnitModifiers = baseUnitModifiersSnapshot ?? throw new ArgumentNullException(nameof(baseUnitModifiersSnapshot));
            DamageInstance.ResetValues();
            IsCritical = false;
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

