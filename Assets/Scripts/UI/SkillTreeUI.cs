using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Windows")]
    [SerializeField] private PlayerStatsWindow playerStatsWindow;

    public void TogglePlayerStatsWindow()
    {
        playerStatsWindow.gameObject.SetActive(!playerStatsWindow.gameObject.activeSelf);
    }
}
