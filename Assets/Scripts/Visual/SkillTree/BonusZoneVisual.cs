using TMPro;
using UnityEngine;

namespace SkillTree
{
    public class BonusZoneVisual : MonoBehaviour
    {
        [SerializeField] private BonusZone bonusZone;
        [SerializeField] private TMP_Text text;

        private void Awake()
        {
            bonusZone.OnAllocatedCountChanged += UpdateText;
            UpdateText();
        }

        private void OnDestroy()
        {
            if (bonusZone != null)
                bonusZone.OnAllocatedCountChanged -= UpdateText;
        }

        private void UpdateText()
        {
            text.text = $"Gain {bonusZone.GetCurrentModifierDescription()} for every allocated node in this zone\nAllocated: {bonusZone.AllocatedNodesCount}";
        }
    }

}
