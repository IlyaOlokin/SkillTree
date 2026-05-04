using System.Collections.Generic;
using Battle;
using UnityEngine;
using Zenject;

namespace SkillTree
{
    public class SkillTreeWeaponTypeProvider : MonoBehaviour
    {
        [Inject] private PlayerUnit _player;

        [SerializeField] private MainSkillTree skillTree;
        [SerializeField] private List<Node> hammerNodes = new();
        [SerializeField] private List<Node> swordNodes = new();
        [SerializeField] private List<Node> staffNodes = new();

        public WeaponType CurrentWeaponType { get; private set; } = WeaponType.Unarmed;

        private void Awake()
        {
            if (skillTree == null)
                skillTree = GetComponent<MainSkillTree>();
        }

        private void OnEnable()
        {
            if (skillTree != null)
                skillTree.OnSkillTreeChanged += UpdateWeaponType;

            UpdateWeaponType();
        }

        private void OnDisable()
        {
            if (skillTree != null)
                skillTree.OnSkillTreeChanged -= UpdateWeaponType;
        }

        private void UpdateWeaponType()
        {
            bool hasHammer = HasActiveNode(hammerNodes);
            bool hasSword = HasActiveNode(swordNodes);
            bool hasStaff = HasActiveNode(staffNodes);

            int activeWeaponCount = 0;
            if (hasHammer) activeWeaponCount++;
            if (hasSword) activeWeaponCount++;
            if (hasStaff) activeWeaponCount++;

            if (activeWeaponCount > 1)
                Debug.LogWarning("Skill tree has conflicting weapon nodes. First matching weapon will be used.", this);

            WeaponType weaponType = WeaponType.Unarmed;
            if (hasHammer)
                weaponType = WeaponType.Hammer;
            else if (hasSword)
                weaponType = WeaponType.Sword;
            else if (hasStaff)
                weaponType = WeaponType.Staff;

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
