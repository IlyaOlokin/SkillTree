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
        [SerializeField] private WeaponIcon[] icons = CreateWeaponIcons();

        private Sprite _fallbackIcon;

        private static WeaponIcon[] CreateWeaponIcons()
        {
            return new[]
            {
                new WeaponIcon { weaponType = WeaponType.Sword },
                new WeaponIcon { weaponType = WeaponType.Hammer },
                new WeaponIcon { weaponType = WeaponType.FireStaff },
                new WeaponIcon { weaponType = WeaponType.ColdStaff },
                new WeaponIcon { weaponType = WeaponType.LightningStaff }
            };
        }

        private void OnValidate()
        {
            WeaponIcon[] weaponIcons = CreateWeaponIcons();
            if (icons != null)
            {
                for (int i = 0; i < weaponIcons.Length; i++)
                {
                    for (int j = 0; j < icons.Length; j++)
                    {
                        if (icons[j].weaponType != weaponIcons[i].weaponType)
                            continue;

                        weaponIcons[i].icon = icons[j].icon;
                        break;
                    }
                }
            }

            icons = weaponIcons;
        }

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
