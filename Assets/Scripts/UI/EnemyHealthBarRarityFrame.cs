using Battle;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarRarityFrame : MonoBehaviour
{
    [SerializeField] private EnemyUnit unit;
    [SerializeField] private Image frameImage;

    [Header("Rarity Frames")]
    [SerializeField] private Sprite normalFrame;
    [SerializeField] private Sprite magicFrame;
    [SerializeField] private Sprite rareFrame;
    [SerializeField] private Sprite eliteFrame;
    [SerializeField] private Sprite bossFrame;

    private void Reset()
    {
        unit = GetComponentInParent<EnemyUnit>();
        frameImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (unit != null)
        {
            unit.OnInitialized += UpdateFrame;
        }
    }

    private void Start()
    {
        UpdateFrame();
    }

    private void OnDestroy()
    {
        if (unit != null)
        {
            unit.OnInitialized -= UpdateFrame;
        }
    }

    private void UpdateFrame()
    {
        if (unit == null || frameImage == null || unit.SpawnData == null)
        {
            return;
        }

        frameImage.sprite = GetFrameSprite(unit.SpawnData.Rarity);
    }

    private Sprite GetFrameSprite(EnemyRarity rarity)
    {
        return rarity switch
        {
            EnemyRarity.Magic => magicFrame,
            EnemyRarity.Rare => rareFrame,
            EnemyRarity.Elite => eliteFrame,
            EnemyRarity.Boss => bossFrame,
            _ => normalFrame
        };
    }
}
