using System.Collections.Generic;
using Battle;
using UnityEngine;

public class BarrierVisual : MonoBehaviour
{
    [System.Serializable]
    private struct BarrierBarStyle
    {
        public Sprite Border;
        public Sprite Fill;
    }

    [SerializeField] private Barrier barrier;
    [SerializeField] private GSlider prefab;
    [SerializeField] private RectTransform spawnPos;
    [SerializeField] private float horizontalGap = 4f;
    [SerializeField] private BarrierBarStyle edgeStyle;
    [SerializeField] private BarrierBarStyle middleStyle;
    [SerializeField] private BarrierBarStyle singleStyle;

    private readonly List<GSlider> _bars = new();
    private RectTransform _transform;

    private void Awake()
    {
        barrier.OnMaxBarrierChanged += Rebuild;
        barrier.OnBarrierCountChanged += Refresh;
        _transform = GetComponent<RectTransform>();
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

        RefreshAnimated();
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

        LayoutBars();
        ApplyBarStyles();

        Refresh();
    }

    private void LayoutBars()
    {
        if (_bars.Count == 0)
            return;

        float totalWidth = _transform.rect.width;
        
        float gap = _bars.Count > 1 ? horizontalGap : 0f;
        float availableWidth = Mathf.Max(0f, totalWidth - gap * (_bars.Count - 1));
        float barWidth = availableWidth / _bars.Count;
        float startX = -totalWidth * 0.5f + barWidth * 0.5f;

        for (int i = 0; i < _bars.Count; i++)
        {
            var rect = _bars[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + i * (barWidth + gap), 0f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barWidth);
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < _bars.Count; i++)
        {
            if (i < barrier.BarrierCount)
                _bars[i].SetBar(1f);
            else if (i == barrier.BarrierCount)
                _bars[i].SetBar(barrier.CooldownProgress);
            else
                _bars[i].SetBar(0f);
        }
    }

    private void RefreshAnimated()
    {
        for (int i = 0; i < _bars.Count; i++)
        {
            if (i < barrier.BarrierCount)
            {
                _bars[i].SetBar(1f);
            }
            else if (i == barrier.BarrierCount)
            {
                _bars[i].UpdateBar(barrier.CooldownProgress);
            }
            else
            {
                _bars[i].SetBar(0f);
            }
        }
    }

    private void ApplyBarStyles()
    {
        int barCount = _bars.Count;

        for (int i = 0; i < barCount; i++)
        {
            bool isSingle = barCount == 1;
            bool isFirst = i == 0;
            bool isLast = i == barCount - 1;

            BarrierBarStyle style = GetStyle(isSingle, isFirst, isLast);
            _bars[i].SetSprites(style.Border, style.Fill);
            _bars[i].SetMirrored(!isSingle && isLast);
        }
    }

    private BarrierBarStyle GetStyle(bool isSingle, bool isFirst, bool isLast)
    {
        if (isSingle)
            return singleStyle;

        if (isFirst || isLast)
            return edgeStyle;

        return middleStyle;
    }
}
