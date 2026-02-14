using System.Collections.Generic;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;

public class EnergyBarrierVisual : MonoBehaviour
{
    [SerializeField] private EnergyBarrier barrier;
    [SerializeField] private GSlider prefab;
    [SerializeField] private RectTransform spawnPos;
    [SerializeField] private float verticalSpacing = 25f;

    private readonly List<GSlider> _bars = new();

    private void Awake()
    {
        barrier.OnMaxBarrierChanged += Rebuild;
        barrier.OnBarrierCountChanged += Refresh;
    }

    private void OnDestroy()
    {
        barrier.OnMaxBarrierChanged -= Rebuild;
        barrier.OnBarrierCountChanged -= Refresh;
    }

    private void Update()
    {
        if (!barrier.HasBarrier || barrier.IsFull)
            return;

        int index = barrier.BarrierCount;

        if (index >= 0 && index < _bars.Count)
            _bars[index].UpdateBar(barrier.CooldownProgress);
    }

    private void Rebuild()
    {
        int needed = barrier.MaxBarrierCount;

        while (_bars.Count < needed)
            _bars.Add(Instantiate(prefab, transform));

        while (_bars.Count > needed)
        {
            Destroy(_bars[^1].gameObject);
            _bars.RemoveAt(_bars.Count - 1);
        }

        for (int i = 0; i < _bars.Count; i++)
        {
            var rect = _bars[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0, i * verticalSpacing);
        }

        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < _bars.Count; i++)
        {
            if (i < barrier.BarrierCount)
                _bars[i].UpdateBar(1f);
            else if (i == barrier.BarrierCount)
                _bars[i].UpdateBar(barrier.CooldownProgress);
            else
                _bars[i].UpdateBar(0f);
        }
    }
}
