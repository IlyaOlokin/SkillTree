using UnityEngine;
using System;

namespace Battle
{
    public class UnitLevel : MonoBehaviour, IUnitComponent
    { 
        private Unit _owner;
    
        [Header("Level")]
        [SerializeField] private int level = 1;
        [SerializeField] private double currentExp = 0d;
        [SerializeField] private double expToNextLevel = 100d;
        [SerializeField] private double baseExpToNextLevel = 100d;
        [SerializeField] private float expGrowthPerLevel = 1.12f;

        [Header("Skill Points")]
        [SerializeField] private int skillPoints = 1;
        
        public int Level => level;
        public double CurrentExp => currentExp;
        public double ExpToNextLevel => expToNextLevel;
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
                AddExperience(expToNextLevel);
            }
        }

        public void AddExperience(double amount)
        {
            if (amount <= 0d)
                return;

            if (expToNextLevel <= 0d)
            {
                Debug.LogError($"{nameof(UnitLevel)} has invalid {nameof(expToNextLevel)}={expToNextLevel}. Resetting to 100.", this);
                expToNextLevel = 100d;
            }

            currentExp += amount;

            int safetyCounter = 0;
            while (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                LevelUp();
                
                if (++safetyCounter > 1000)
                {
                    Debug.LogError($"{nameof(UnitLevel)} level-up loop exceeded safety threshold. Breaking out.", this);
                    break;
                }
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
            double clampedBaseExp = Math.Max(1d, baseExpToNextLevel);
            double growth = Math.Max(1.001d, expGrowthPerLevel);
            double scaled = clampedBaseExp * Math.Pow(growth, level - 1);
            expToNextLevel = Math.Max(1d, Math.Round(scaled, MidpointRounding.AwayFromZero));
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

