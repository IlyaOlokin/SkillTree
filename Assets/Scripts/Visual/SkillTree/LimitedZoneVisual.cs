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

        private void Awake()
        {
            limitedZone.OnAllocatedCountChanged += UpdateText;
            UpdateText();
        }

        private void UpdateText()
        {
            text.text = limitedZone.CurrentAllocatedCount + "/" + limitedZone.MaxAllocatedNode;
        }
    }
}

