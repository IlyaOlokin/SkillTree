namespace Battle.MiniGames
{
    public sealed class MiniGameDamageBuffEffect : MiniGameMoreStatBuffEffect
    {
        public MiniGameDamageBuffEffect(float duration, float moreDamage)
            : base(duration, StatType.Damage, moreDamage)
        {
        }

        protected override string GetDescriptionId()
        {
            return "miniGameDamageBuff";
        }

        protected override string GetDescriptionFallback()
        {
            return "Grants [[0]]% more {damage|Damage} for [[1]] seconds.";
        }
    }
}
