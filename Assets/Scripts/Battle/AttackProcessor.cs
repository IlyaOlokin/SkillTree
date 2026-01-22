namespace Battle
{
    public static class AttackProcessor
    {
        public static void InitiateAttack(Unit attackerUnit, DamageInfo damageInfo, ITarget defender)
        {
            // Base Modifiers are applied on skill tree update
            
            if (attackerUnit is PlayerUnit unit) DamageCalculator.ApplySpecialModifiers(unit, damageInfo);
            
            // defence with specific mods Defence(DamageInfo damageInfo)
            
            DamageInstance damageInstance = DamageCalculator.CalculateAttackDamage(damageInfo);
            
            // defence with Defences Defence(DamageInstance damageInfo)
            
            defender.ReceiveDamage(damageInstance);
        }
    }
}
