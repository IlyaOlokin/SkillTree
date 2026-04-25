using UnityEngine;
using System;
using SaveSystem;

namespace Battle
{
    public class UnitLevel : MonoBehaviour, IUnitComponent
    { 
        private Unit _owner;
        private bool _defaultsCaptured;
        private int _defaultLevel;
        private double _defaultCurrentExp;
        private double _defaultExpToNextLevel;
        private int _defaultSkillPoints;
    
        [Header("Level")]
        [SerializeField] private int level = 1;
        [SerializeField] private double currentExp = 0d;
        [SerializeField] private double expToNextLevel = 100d;
        [SerializeField] private double baseExpToNextLevel = 100d;
        [SerializeField] private float expGrowthPerLevel = 1.08f;

        [Header("Skill Points")]
        [SerializeField] private int skillPoints = 1;
        
        public int Level => level;
        public double CurrentExp => currentExp;
        public double ExpToNextLevel => expToNextLevel;
        public int SkillPoints => skillPoints;

        public event Action OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnSkillPointsChanged;

        private void Awake()
        {
            CaptureDefaultsIfNeeded();
        }

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

        public PlayerSaveData CaptureSaveData()
        {
            return new PlayerSaveData
            {
                level = level,
                currentExp = currentExp,
                skillPoints = skillPoints
            };
        }

        public void ApplySaveData(PlayerSaveData saveData)
        {
            if (saveData == null)
                return;

            level = Math.Max(1, saveData.level);
            currentExp = Math.Max(0d, saveData.currentExp);
            skillPoints = Math.Max(0, saveData.skillPoints);
            RecalculateExpToNextLevel();
            currentExp = Math.Min(currentExp, expToNextLevel);

            OnExpChanged?.Invoke();
            OnSkillPointsChanged?.Invoke(skillPoints);
        }

        public void ResetToDefaults()
        {
            CaptureDefaultsIfNeeded();

            level = _defaultLevel;
            currentExp = _defaultCurrentExp;
            expToNextLevel = _defaultExpToNextLevel;
            skillPoints = _defaultSkillPoints;

            OnExpChanged?.Invoke();
            OnSkillPointsChanged?.Invoke(skillPoints);
        }

        private void CaptureDefaultsIfNeeded()
        {
            if (_defaultsCaptured)
                return;

            _defaultLevel = Math.Max(1, level);
            _defaultCurrentExp = Math.Max(0d, currentExp);
            _defaultExpToNextLevel = Math.Max(1d, expToNextLevel);
            _defaultSkillPoints = Math.Max(0, skillPoints);
            _defaultsCaptured = true;
        }
    }
}

