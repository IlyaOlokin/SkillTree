using System.Collections;
using SkillTree;
using UnityEngine;

namespace Visual
{
    public class NodeAllocationEffect : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [SerializeField] private Renderer allocationEffect;
        [SerializeField, Min(0.01f)] private float duration = 1.25f;

        private Node _node;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _playRoutine;

        private void Awake()
        {
            _node = GetComponent<Node>();
            _propertyBlock = new MaterialPropertyBlock();
            SetProgress(0f);
            
            if (_node != null)
            {
                _node.OnAllocatedChanged += HandleAllocatedChanged;
            }
        }

        private void OnDestroy()
        {
            StopPlayback();

            if (_node != null)
            {
                _node.OnAllocatedChanged -= HandleAllocatedChanged;
            }
        }

        private void HandleAllocatedChanged(Node n)
        {
            if (n.IsApplyingSavedState)
            {
                SetProgress(1f);
                return;
            }

            if (n.IsAllocated)
            {
                Play();
            }
        }

        private void Play()
        {
            StopPlayback();
            _playRoutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            float time = 0f;
            SetProgress(0f);

            while (time < duration)
            {
                time += Time.deltaTime;
                SetProgress(Mathf.Clamp01(time / duration));
                yield return null;
            }

            SetProgress(1f);
            _playRoutine = null;
        }

        private void StopPlayback()
        {
            if (_playRoutine == null) return;

            StopCoroutine(_playRoutine);
            _playRoutine = null;
            SetProgress(1f);
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
