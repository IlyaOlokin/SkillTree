namespace Battle.MiniGames
{
    public sealed class MiniGameAttackSpeedBuffEffect : MiniGameMoreStatBuffEffect
    {
        public MiniGameAttackSpeedBuffEffect(float duration, float moreAttackSpeed)
            : base(duration, StatType.AttackSpeed, moreAttackSpeed)
        {
        }

        protected override string GetDescriptionId()
        {
            return "miniGameAttackSpeedBuff";
        }

        protected override string GetDescriptionFallback()
        {
            return "Grants [[0]]% more Attack Speed for [[1]] seconds.";
        }
    }
}
