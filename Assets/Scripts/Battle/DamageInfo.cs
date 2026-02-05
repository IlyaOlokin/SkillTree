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
        public Unit owner;
        
        [SerializeField] public BaseUnitModifiers BaseUnitModifiers;
        
        public DamageInstance DamageInstance = new DamageInstance();
        [HideInInspector] public bool isCritical = false;
    }

    public class DamageInstance
    {
        public Dictionary<DamageType, float> Damage = new Dictionary<DamageType, float>();
    }
}
