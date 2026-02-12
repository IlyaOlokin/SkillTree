using System;
using Battle;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GSlider : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private bool needSecondaryFill;
    [SerializeField] private Image secondaryFill;
    [SerializeField] private bool needText;
    [SerializeField] private TMP_Text text;

    [SerializeField] private float fillMoveDuration = 0.1f;

    private void Start()
    {
        UpdateBar();
        UpdateText();
        secondaryFill.gameObject.SetActive(needSecondaryFill);
        text.gameObject.SetActive(needText);
    }


    public void UpdateBar(float fillAmount = 0)
    {
        if (needSecondaryFill)
        {
            secondaryFill.fillAmount = fillAmount;
        }
        
        fill.DOFillAmount(fillAmount, fillMoveDuration).SetLink(gameObject);;
    }

    public void UpdateText(String newText = "")
    {
        if (!needText)
            return;
        
        text.text = newText;
    }
}
