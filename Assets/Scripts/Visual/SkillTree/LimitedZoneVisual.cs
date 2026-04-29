using System;
using SkillTree;
using TMPro;
using UnityEngine;

namespace Visual
{
    public class LimitedZoneVisual : MonoBehaviour
    {
        [SerializeField] private LimitedZone limitedZone;
        [SerializeField] private TMP_Text text;
        [SerializeField] private TMP_Text nextLimitText;

        private void Awake()
        {
            if (limitedZone == null)
                return;

            limitedZone.OnAllocatedCountChanged += UpdateText;
            UpdateText();
        }

        private void OnDestroy()
        {
            if (limitedZone != null)
                limitedZone.OnAllocatedCountChanged -= UpdateText;
        }

        private void UpdateText()
        {
            if (limitedZone == null)
                return;

            if (text != null)
                text.text = limitedZone.CurrentAllocatedCount + "/" + limitedZone.MaxAllocatedNode;

            if (nextLimitText == null)
                return;

            nextLimitText.text = limitedZone.UsesPlayerLevelLimit &&
                                 limitedZone.TryGetNextPlayerLevelLimit(
                                     out int requiredPlayerLevel,
                                     out _)
                ? $"Lv. {requiredPlayerLevel}"
                : string.Empty;
        }
    }
}

