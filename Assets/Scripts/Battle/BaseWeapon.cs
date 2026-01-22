using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Battle
{
    [CreateAssetMenu(menuName = "Base Weapon", fileName = "New BaseWeapon")]
    public class BaseWeapon : ScriptableObject
    {
        public WeaponType weaponType;
        [FormerlySerializedAs("damage")] public DamageInfo damageInfo; // change
        
        public float attacksSpeed;
    }
}

