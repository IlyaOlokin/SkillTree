using System;
using Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Visual
{
    public class AttackCooldownVisual : MonoBehaviour
    {
        [SerializeField] private Attacker attacker;
        [SerializeField] private GSlider slider;
        
        private void Start()
        {
            UpdateSlider();
        }

        private void Update()
        {
            UpdateSlider();
        }

        private void UpdateSlider()
        {
            slider.UpdateBar(attacker.AttackProgress);
        }
    }

}
