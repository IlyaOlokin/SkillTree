using UnityEngine;

[CreateAssetMenu(menuName = "UI/Mystic Colors Config", fileName = "MysticColorsConfig")]
public class MysticColorsConfig : ScriptableObject
{
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private Color darknessColor = Color.black;

    public Color NeutralColor => neutralColor;
    public Color LightColor => lightColor;
    public Color DarknessColor => darknessColor;
}
