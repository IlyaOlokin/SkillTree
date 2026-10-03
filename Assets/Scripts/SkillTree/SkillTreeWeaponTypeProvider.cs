using System.Collections.Generic;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace SkillTree
{
    public class SkillTreeWeaponTypeProvider : MonoBehaviour
    {
        [Inject] private PlayerUnit _player;

        [SerializeField] private MainSkillTree skillTree;
        [SerializeField] private List<Node> hammerNodes = new();
        [SerializeField] private List<Node> swordNodes = new();
        [FormerlySerializedAs("staffNodes")]
        [SerializeField] private List<Node> fireStaffNodes = new();
        [SerializeField] private List<Node> coldStaffNodes = new();
        [SerializeField] private List<Node> lightningStaffNodes = new();

        public WeaponType CurrentWeaponType { get; private set; } = WeaponType.Unarmed;

        private void Awake()
        {
            if (skillTree == null)
                skillTree = GetComponent<MainSkillTree>();
        }

        private void OnEnable()
        {
            if (skillTree != null)
                skillTree.OnActiveModifiersChanged += UpdateWeaponType;

            UpdateWeaponType();
        }

        private void OnDisable()
        {
            if (skillTree != null)
                skillTree.OnActiveModifiersChanged -= UpdateWeaponType;
        }

        private void UpdateWeaponType()
        {
            bool hasHammer = HasActiveNode(hammerNodes);
            bool hasSword = HasActiveNode(swordNodes);
            bool hasFireStaff = HasActiveNode(fireStaffNodes);
            bool hasColdStaff = HasActiveNode(coldStaffNodes);
            bool hasLightningStaff = HasActiveNode(lightningStaffNodes);

            int activeWeaponCount = 0;
            if (hasHammer) activeWeaponCount++;
            if (hasSword) activeWeaponCount++;
            if (hasFireStaff) activeWeaponCount++;
            if (hasColdStaff) activeWeaponCount++;
            if (hasLightningStaff) activeWeaponCount++;

            if (activeWeaponCount > 1)
                Debug.LogWarning("Skill tree has conflicting weapon nodes. First matching weapon will be used.", this);

            WeaponType weaponType = WeaponType.Unarmed;
            if (hasHammer)
                weaponType = WeaponType.Hammer;
            else if (hasSword)
                weaponType = WeaponType.Sword;
            else if (hasFireStaff)
                weaponType = WeaponType.FireStaff;
            else if (hasColdStaff)
                weaponType = WeaponType.ColdStaff;
            else if (hasLightningStaff)
                weaponType = WeaponType.LightningStaff;

            CurrentWeaponType = weaponType;

            if (_player != null)
                _player.SetWeaponType(CurrentWeaponType);
        }

        private static bool HasActiveNode(List<Node> nodes)
        {
            if (nodes == null)
                return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].IsActive)
                    return true;
            }

            return false;
        }
    }
}
