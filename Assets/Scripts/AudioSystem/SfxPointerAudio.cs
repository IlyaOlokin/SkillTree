using UnityEngine;
using UnityEngine.EventSystems;

namespace AudioSystem
{
    public sealed class SfxPointerAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private string hoverCueId;
        [SerializeField] private string clickCueId;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Play(hoverCueId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Play(clickCueId);
        }

        private static void Play(string cueId)
        {
            if (GameAudio.Instance == null || string.IsNullOrWhiteSpace(cueId))
                return;

            GameAudio.Instance.PlaySfx(cueId);
        }
    }
}
