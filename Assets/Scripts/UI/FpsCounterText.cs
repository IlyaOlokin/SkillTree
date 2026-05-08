using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class FpsCounterText : MonoBehaviour
{
    [SerializeField] private float updateInterval = 0.25f;
    [SerializeField] private string prefix = "FPS: ";

    private TMP_Text text;
    private int frameCount;
    private float elapsedTime;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;

        if (elapsedTime < updateInterval)
            return;

        float fps = frameCount / elapsedTime;
        text.text = $"{prefix}{fps:0}";

        frameCount = 0;
        elapsedTime = 0f;
    }
}
