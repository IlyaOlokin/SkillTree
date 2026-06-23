namespace Battle.MiniGames
{
    public enum BattleMiniGameOutcome
    {
        Success,
        Fail,
        Cancelled
    }

    public readonly struct BattleMiniGameResult
    {
        public readonly BattleMiniGameOutcome Outcome;
        public readonly float Score01;

        public bool IsSuccess => Outcome == BattleMiniGameOutcome.Success;

        public BattleMiniGameResult(BattleMiniGameOutcome outcome, float score01 = 0f)
        {
            Outcome = outcome;
            Score01 = UnityEngine.Mathf.Clamp01(score01);
        }

        public static BattleMiniGameResult Success(float score01 = 1f)
        {
            return new BattleMiniGameResult(BattleMiniGameOutcome.Success, score01);
        }

        public static BattleMiniGameResult Fail(float score01 = 0f)
        {
            return new BattleMiniGameResult(BattleMiniGameOutcome.Fail, score01);
        }

        public static BattleMiniGameResult Cancelled()
        {
            return new BattleMiniGameResult(BattleMiniGameOutcome.Cancelled);
        }
    }
}
