using System;
using Battle;
using UnityEngine;
using UnityEngine.UI;

namespace Visual
{
    public class AttackCooldownVisual : MonoBehaviour
    {
        [SerializeField] private Attacker attacker;
        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

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
            slider.value = attacker.AttackProgress;
        }
    }

}
