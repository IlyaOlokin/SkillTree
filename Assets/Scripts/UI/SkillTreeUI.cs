using InventorySystem;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Windows")]
    [SerializeField] private PlayerStatsWindow playerStatsWindow;
    [SerializeField] private PlayerInventory playerInventoryWindow;

    public void TogglePlayerStatsWindow()
    {
        playerStatsWindow.gameObject.SetActive(!playerStatsWindow.gameObject.activeSelf);
    }
    
    public void TogglePlayerInventoryWindow()
    {
        playerInventoryWindow.gameObject.SetActive(!playerInventoryWindow.gameObject.activeSelf);
    }
}
