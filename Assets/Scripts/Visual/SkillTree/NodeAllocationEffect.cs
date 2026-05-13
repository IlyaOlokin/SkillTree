using System;
using System.Collections;
using UnityEngine;

namespace Visual
{
    public class NodeAllocationEffect : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [SerializeField] private Renderer allocationEffect;

        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _playRoutine;
        private Action<NodeAllocationEffect> _completeCallback;

        private void Awake()
        {
            if (allocationEffect == null)
            {
                allocationEffect = GetComponentInChildren<Renderer>(true);
            }

            if (GetComponent<SkillTree.Node>() != null)
            {
                if (allocationEffect != null)
                {
                    allocationEffect.gameObject.SetActive(false);
                }

                enabled = false;
                return;
            }

            _propertyBlock = new MaterialPropertyBlock();
            SetProgress(1f);
        }

        private void OnDestroy()
        {
            StopPlayback();
        }

        public void Play(float duration, Action<NodeAllocationEffect> completeCallback)
        {
            StopPlayback();

            _completeCallback = completeCallback;
            gameObject.SetActive(true);
            _playRoutine = StartCoroutine(PlayRoutine(duration));
        }

        public void StopPlayback()
        {
            if (_playRoutine == null) return;

            StopCoroutine(_playRoutine);
            _playRoutine = null;
            _completeCallback = null;
            SetProgress(1f);
        }

        private IEnumerator PlayRoutine(float duration)
        {
            float time = 0f;
            duration = Mathf.Max(0.01f, duration);
            SetProgress(0f);

            while (time < duration)
            {
                time += Time.deltaTime;
                SetProgress(Mathf.Clamp01(time / duration));
                yield return null;
            }

            SetProgress(1f);
            _playRoutine = null;
            Action<NodeAllocationEffect> callback = _completeCallback;
            _completeCallback = null;
            callback?.Invoke(this);
        }

        private void SetProgress(float progress)
        {
            if (allocationEffect == null)
            {
                return;
            }

            allocationEffect.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(ProgressId, progress);
            allocationEffect.SetPropertyBlock(_propertyBlock);
        }
    }
}
