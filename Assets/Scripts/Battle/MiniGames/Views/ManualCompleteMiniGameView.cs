using System;
using UnityEngine;

namespace Battle.MiniGames
{
    public sealed class ManualCompleteMiniGameView : MonoBehaviour, IBattleMiniGameView
    {
        private BattleMiniGameRunContext _context;
        private float _timeLeft;

        public event Action<BattleMiniGameResult> Completed;

        public void StartGame(BattleMiniGameRunContext context)
        {
            _context = context;
            _timeLeft = context != null ? context.Duration : 0f;
        }

        private void Update()
        {
            if (_context == null)
            {
                return;
            }

            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f)
            {
                CompleteFail();
            }
        }

        public void CompleteSuccess()
        {
            Complete(BattleMiniGameResult.Success());
        }

        public void CompleteFail()
        {
            Complete(BattleMiniGameResult.Fail());
        }

        private void Complete(BattleMiniGameResult result)
        {
            if (_context == null)
            {
                return;
            }

            BattleMiniGameRunContext context = _context;
            _context = null;
            context.Complete(result);
            Completed?.Invoke(result);
        }
    }
}
