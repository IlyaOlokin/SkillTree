using System;

namespace Battle.MiniGames
{
    public interface IBattleMiniGameView
    {
        event Action<BattleMiniGameResult> Completed;
        void StartGame(BattleMiniGameRunContext context);
    }
}
