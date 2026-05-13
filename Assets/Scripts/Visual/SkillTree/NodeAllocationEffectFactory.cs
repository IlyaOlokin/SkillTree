using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Visual
{
    [DisallowMultipleComponent]
    public class NodeAllocationEffectFactory : MonoBehaviour
    {
        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField, Min(0)] private int preloadCount = 8;
        [SerializeField, Min(0)] private int maxPoolSize = 32;
        [SerializeField, Min(0.01f)] private float duration = 1.25f;

        private readonly Queue<NodeAllocationEffect> _availableEffects = new();
        private readonly HashSet<NodeAllocationEffect> _activeEffects = new();
        private readonly Dictionary<Node, bool> _knownAllocationStates = new();

        private void Awake()
        {
            if (poolRoot == null)
            {
                poolRoot = transform;
            }

            Preload();
        }

        private void OnEnable()
        {
            Node.OnAnyNodeAllocatedChanged += HandleAnyNodeAllocatedChanged;
        }

        private void OnDisable()
        {
            Node.OnAnyNodeAllocatedChanged -= HandleAnyNodeAllocatedChanged;
        }

        private void OnDestroy()
        {
            foreach (NodeAllocationEffect effect in _activeEffects)
            {
                if (effect != null)
                {
                    effect.StopPlayback();
                }
            }

            _activeEffects.Clear();
            _availableEffects.Clear();
            _knownAllocationStates.Clear();
        }

        private void HandleAnyNodeAllocatedChanged(Node node)
        {
            if (node == null)
            {
                return;
            }

            bool isAllocated = node.IsAllocated;
            if (!_knownAllocationStates.TryGetValue(node, out bool wasAllocated))
            {
                wasAllocated = !isAllocated;
            }

            _knownAllocationStates[node] = isAllocated;

            if (!isAllocated || wasAllocated || node.IsApplyingSavedState)
            {
                return;
            }

            Play(node);
        }

        private void Play(Node node)
        {
            NodeAllocationEffect effect = GetEffect();
            if (effect == null)
            {
                return;
            }

            Transform effectTransform = effect.transform;
            Transform prefabTransform = effectPrefab.transform;
            effectTransform.SetParent(node.transform, false);
            effectTransform.localPosition = prefabTransform.localPosition;
            effectTransform.localRotation = prefabTransform.localRotation;
            effectTransform.localScale = prefabTransform.localScale;

            _activeEffects.Add(effect);
            effect.Play(duration, Release);
        }

        private void Preload()
        {
            if (effectPrefab == null)
            {
                Debug.LogError($"{nameof(NodeAllocationEffectFactory)} has no effect prefab assigned.", this);
                return;
            }

            int count = maxPoolSize > 0 ? Mathf.Min(preloadCount, maxPoolSize) : preloadCount;
            for (int i = 0; i < count; i++)
            {
                NodeAllocationEffect effect = CreateEffect();
                if (effect != null)
                {
                    _availableEffects.Enqueue(effect);
                }
            }
        }

        private NodeAllocationEffect GetEffect()
        {
            while (_availableEffects.Count > 0)
            {
                NodeAllocationEffect effect = _availableEffects.Dequeue();
                if (effect != null)
                {
                    return effect;
                }
            }

            if (effectPrefab == null)
            {
                return null;
            }

            if (maxPoolSize > 0 && _activeEffects.Count >= maxPoolSize)
            {
                return null;
            }

            return CreateEffect();
        }

        private NodeAllocationEffect CreateEffect()
        {
            GameObject instance = Instantiate(effectPrefab, poolRoot);
            instance.SetActive(false);

            NodeAllocationEffect effect = instance.GetComponent<NodeAllocationEffect>();
            if (effect == null)
            {
                effect = instance.AddComponent<NodeAllocationEffect>();
            }

            return effect;
        }

        private void Release(NodeAllocationEffect effect)
        {
            if (effect == null || !_activeEffects.Remove(effect))
            {
                return;
            }

            effect.transform.SetParent(poolRoot, false);
            effect.gameObject.SetActive(false);
            _availableEffects.Enqueue(effect);
        }
    }
}
