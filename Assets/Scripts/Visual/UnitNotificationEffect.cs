using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Visual
{
    public class UnitNotificationEffect : MonoBehaviour
{
    [SerializeField] private Transform criticalEffect;
    [SerializeField] private Transform objectToMove;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float lifeTime;
    [SerializeField] private float effectDuration;
    [SerializeField] private float scaleMultiplier;
    [SerializeField] private float moveDistance;
    [SerializeField] private float lerpSpeed;
    [SerializeField] private float posSpreading;
    
    [Header("Colors")]
    [SerializeField] private Color dmgColor;
    [SerializeField] private Color dmgEnergyShieldColor;
    [SerializeField] private Color healColor;
    [SerializeField] private Color messageColor;
    private float timer;
    private Vector3 targetScale;
    private Vector3 startScale;
    private Vector3 startPos;
    private Vector3 targetPos;

    void Start()
    {
        objectToMove.position += new Vector3(Random.Range(-posSpreading, posSpreading),
            Random.Range(-posSpreading, posSpreading));
        GetComponent<Canvas>().worldCamera = Camera.main;
        Destroy(gameObject, lifeTime);
        startScale = objectToMove.localScale;
        targetScale = startScale * scaleMultiplier;
        startPos = objectToMove.position;
        targetPos = objectToMove.position + Vector3.up * moveDistance;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer <= effectDuration)
        {
            objectToMove.localScale = Vector2.Lerp(startScale, targetScale, timer / effectDuration);
            objectToMove.position = Vector2.Lerp(objectToMove.position, targetPos, lerpSpeed);
        }
        else
        {
            objectToMove.localScale = Vector2.Lerp(targetScale, startScale,
                (timer - effectDuration) / (lifeTime - effectDuration));
        }
    }

    public void WriteDamage(float dmg, bool energyShield = false)
    {
        if (energyShield) text.color = dmgEnergyShieldColor;
        else text.color = dmgColor;
        text.text = Math.Round(dmg).ToString();
    }

    public void WriteHeal(float heal)
    {
        text.color = healColor;
        text.text = Math.Round(heal).ToString();
    }
    
    public void WriteMessage(string message)
    {
        text.color = messageColor;
        text.text = message;
    }

    public void InitTargetPos(Vector3 damagerPos, bool isCritical)
    {
        if ((damagerPos - objectToMove.position).magnitude < posSpreading)
        {
            targetPos = objectToMove.position + new Vector3(0, moveDistance);
            return;
        }
        var dir = (objectToMove.position - damagerPos).normalized;
        targetPos = objectToMove.position + dir * moveDistance;

        if (!isCritical) return;
        criticalEffect.gameObject.SetActive(true);
        float angle = Mathf.Atan2(dir.y, dir.x) * 180 / Mathf.PI;
        criticalEffect.eulerAngles = new Vector3(0, 0, angle - 90);
    }
}

[Serializable]
public class InnerColor
{
    public int bottomBorder;
    public Color color;
}
}
