using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class AttackResolver : MonoBehaviour, ITarget
    {
        public static AttackResolver instance;
        
        private List<EnemyUnit> enemies;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }
        
        public void ReceiveAttack(Damage damage)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null)
                    continue;
                enemies[i].ReceiveAttack(damage);
                break;
            }
        }

        public void SetNewEnemies(List<EnemyUnit> enemyUnits)
        {
            enemies = new List<EnemyUnit>(enemyUnits);
        }
    }
}