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
    [SerializeField] private Color dmgColor = Color.white;
    [SerializeField] private Color dmgEnergyShieldColor = Color.cyan;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color messageColor = new(1f, 0.82f, 0.18f, 1f);
    private Camera _worldCamera;
    private float timer;
    private Vector3 targetScale;
    private Vector3 startScale;
    private Vector3 startPos;
    private Vector3 targetPos;

    void Start()
    {
        EnsureRuntimeReferences();
        objectToMove.position += new Vector3(Random.Range(-posSpreading, posSpreading),
            Random.Range(-posSpreading, posSpreading));
        AssignCanvasCamera();

        Destroy(gameObject, Mathf.Max(0.01f, lifeTime));
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
                (timer - effectDuration) / Mathf.Max(0.01f, lifeTime - effectDuration));
        }
    }

    public void WriteDamage(float dmg, bool energyShield = false)
    {
        EnsureRuntimeReferences();
        if (energyShield) text.color = dmgEnergyShieldColor;
        else text.color = dmgColor;
        text.text = Math.Round(dmg).ToString();
    }

    public void WriteHeal(float heal)
    {
        EnsureRuntimeReferences();
        text.color = healColor;
        text.text = Math.Round(heal).ToString();
    }
    
    public void WriteMessage(string message)
    {
        EnsureRuntimeReferences();
        text.color = messageColor;
        text.text = message;
    }

    public void SetWorldCamera(Camera worldCamera)
    {
        _worldCamera = worldCamera;
        AssignCanvasCamera();
    }

    public void InitTargetPos(Vector3 damagerPos, bool isCritical)
    {
        EnsureRuntimeReferences();
        if ((damagerPos - objectToMove.position).magnitude < posSpreading)
        {
            targetPos = objectToMove.position + new Vector3(0, moveDistance);
            return;
        }
        var dir = (objectToMove.position - damagerPos).normalized;
        targetPos = objectToMove.position + dir * moveDistance;

        if (!isCritical || criticalEffect == null) return;
        criticalEffect.gameObject.SetActive(true);
        float angle = Mathf.Atan2(dir.y, dir.x) * 180 / Mathf.PI;
        criticalEffect.eulerAngles = new Vector3(0, 0, angle - 90);
    }

    private void EnsureRuntimeReferences()
    {
        if (objectToMove == null)
            objectToMove = transform;

        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>(true);

        if (text == null)
            text = CreateFallbackText();

        if (criticalEffect == null)
        {
            Transform critical = transform.Find("CriticalEffect");
            if (critical != null)
                criticalEffect = critical;
        }

        if (lifeTime <= 0f)
            lifeTime = 0.85f;

        if (effectDuration <= 0f)
            effectDuration = 0.32f;

        if (scaleMultiplier <= 0f)
            scaleMultiplier = 1.25f;

        if (lerpSpeed <= 0f)
            lerpSpeed = 0.08f;

        if (moveDistance <= 0f)
            moveDistance = transform is RectTransform ? 45f : 0.45f;

        if (posSpreading <= 0f)
            posSpreading = transform is RectTransform ? 12f : 0.12f;
    }

    private TextMeshProUGUI CreateFallbackText()
    {
        GameObject textObject = new("Text", typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rectTransform = textObject.transform as RectTransform;
        rectTransform.SetParent(transform, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(160f, 42f);

        TextMeshProUGUI createdText = textObject.AddComponent<TextMeshProUGUI>();
        createdText.alignment = TextAlignmentOptions.Center;
        createdText.fontSize = 24f;
        createdText.raycastTarget = false;
        return createdText;
    }

    private void AssignCanvasCamera()
    {
        Canvas canvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return;

        Camera camera = _worldCamera != null ? _worldCamera : ResolveParentCanvasCamera();
        if (camera == null)
            camera = FindBattleCamera();
        if (camera == null)
            camera = Camera.main;

        if (camera != null)
            canvas.worldCamera = camera;
    }

    private Camera ResolveParentCanvasCamera()
    {
        Canvas parentCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>()
            : null;

        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return parentCanvas.worldCamera;
    }

    private static Camera FindBattleCamera()
    {
        GameObject battleCameraObject = GameObject.FindWithTag("BattleCamera");
        if (battleCameraObject == null)
            return null;

        Camera camera = battleCameraObject.GetComponent<Camera>();
        if (camera != null)
            return camera;

        return battleCameraObject.GetComponentInChildren<Camera>();
    }
}

[Serializable]
public class InnerColor
{
    public int bottomBorder;
    public Color color;
}
}
