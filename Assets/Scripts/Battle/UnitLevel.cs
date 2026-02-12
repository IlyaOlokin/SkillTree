using UnityEngine;
using System;

namespace Battle
{
    public class UnitLevel : MonoBehaviour, IUnitComponent
    { 
        private Unit _owner;
    
        [Header("Level")]
        [SerializeField] private int level = 1;
        [SerializeField] private float currentExp = 0;
        [SerializeField] private float expToNextLevel = 100;

        [Header("Skill Points")]
        [SerializeField] private int skillPoints = 1;

        public int Level => level;
        public float CurrentExp => currentExp;
        public float ExpToNextLevel => expToNextLevel;
        public int SkillPoints => skillPoints;

        public event Action OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnSkillPointsChanged;

        public void Init(Unit owner)
        {
            _owner = owner;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                AddExperience(100);
            }
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
                return;

            currentExp += amount;
            
            while (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                LevelUp();
            }
            OnExpChanged?.Invoke();
        }

        private void LevelUp()
        {
            level++;
            skillPoints += 1;

            RecalculateExpToNextLevel();

            OnLevelUp?.Invoke(level);
            OnSkillPointsChanged?.Invoke(skillPoints);
        }

        private void RecalculateExpToNextLevel()
        {
            expToNextLevel = Mathf.RoundToInt(100 * Mathf.Pow(1.25f, level - 1));
        }
        
        public bool TrySpendSkillPoints(int cost)
        {
            if (cost <= 0)
                return false;

            if (skillPoints < cost)
                return false;

            skillPoints -= cost;
            OnSkillPointsChanged?.Invoke(skillPoints);
            return true;
        }
        
        public void RefundSkillPoints(int amount)
        {
            if (amount <= 0)
                return;

            skillPoints += amount;
            OnSkillPointsChanged?.Invoke(skillPoints);
        }
    }
}

