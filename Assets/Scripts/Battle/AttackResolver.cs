using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class AttackResolver : MonoBehaviour, ITarget
    {
        public static AttackResolver instance;
        
        private List<EnemyUnit> enemies;

        public Unit UnitObject
        {
            get
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null)
                        continue;
                    return enemies[i];
                }

                return null;
            }
            set{}
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }
        
        public DamageInstance ReceiveDamage(DamageInstance damageInstance)
        {
            return UnitObject.ReceiveDamage(damageInstance);
        }

        public void ReceiveDoT(DamageInstance damageInstance)
        {
            UnitObject.ReceiveDoT(damageInstance);
        }

        public void OnEvaded(DamageInstance damageInstance)
        {
            
        }

        public void SetNewEnemies(List<EnemyUnit> enemyUnits)
        {
            enemies = new List<EnemyUnit>(enemyUnits);
        }

        
    }
}