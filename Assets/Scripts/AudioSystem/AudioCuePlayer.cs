using UnityEngine;

namespace AudioSystem
{
    public sealed class AudioCuePlayer : MonoBehaviour
    {
        [SerializeField] private string cueId;
        [SerializeField] private bool playAtTransformPosition;

        public void Play()
        {
            if (GameAudio.Instance == null)
                return;

            if (playAtTransformPosition)
                GameAudio.Instance.PlaySfxAt(cueId, transform.position);
            else
                GameAudio.Instance.PlaySfx(cueId);
        }
    }
}
