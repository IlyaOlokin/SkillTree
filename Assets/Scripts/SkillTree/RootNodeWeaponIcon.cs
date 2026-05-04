using System;
using Battle;
using UnityEngine;
using Visual;
using Zenject;

namespace SkillTree
{
    public class RootNodeWeaponIcon : MonoBehaviour
    {
        [Serializable]
        private struct WeaponIcon
        {
            public WeaponType weaponType;
            public Sprite icon;
        }

        [Inject] private PlayerUnit _player;

        [SerializeField] private NodeVisual nodeVisual;
        [SerializeField] private WeaponIcon[] icons;

        private Sprite _fallbackIcon;

        private void Awake()
        {
            if (nodeVisual == null)
                nodeVisual = GetComponentInChildren<NodeVisual>(true);

            _fallbackIcon = nodeVisual != null ? nodeVisual.NodeIcon : null;
        }

        private void OnEnable()
        {
            if (_player != null)
                _player.OnWeaponTypeChanged += UpdateIcon;

            UpdateIcon(_player != null ? _player.WeaponType : WeaponType.Unarmed);
        }

        private void OnDisable()
        {
            if (_player != null)
                _player.OnWeaponTypeChanged -= UpdateIcon;
        }

        private void UpdateIcon(WeaponType weaponType)
        {
            if (nodeVisual == null)
                return;

            nodeVisual.SetDefaultNodeIcon(GetIcon(weaponType) ?? _fallbackIcon);
        }

        private Sprite GetIcon(WeaponType weaponType)
        {
            if (icons == null)
                return null;

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i].weaponType == weaponType)
                    return icons[i].icon;
            }

            return null;
        }
    }
}
