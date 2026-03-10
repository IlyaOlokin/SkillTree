using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class AttackResolver : MonoBehaviour, ITarget
    {
        private List<Unit> enemies;

        public Unit UnitObject
        {
            get
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null)
                        continue;
                    if (enemies[i].gameObject.activeSelf == false)
                        continue;
                    return enemies[i];
                }

                return null;
            }
            set{}
        }
        
        public DamageInstance ReceiveDamage(DamageInstance damageInstance)
        {
            return UnitObject.ReceiveDamage(damageInstance);
        }

        public void ReceiveDoT(DamageInstance damageInstance)
        {
            UnitObject.ReceiveDoT(damageInstance);
        }

        public void OnHitEvaded(DamageInstance damageInstance)
        {
            
        }

        public void OnHitBlock(DamageInstance damageInstance)
        {
            
        }

        public void SetNewEnemies(List<Unit> enemyUnits)
        {
            enemies = new List<Unit>(enemyUnits);
        }

        
    }
}