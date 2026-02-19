using SkillTree;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [SerializeField] protected NodeDescription NodeDescriptionPrefab;
    private NodeDescription NodeDescriptionInstance;

    public void DisplayNodeDescription(Node node)
    {
        if (NodeDescriptionInstance == null)
        {
            NodeDescriptionInstance = Instantiate(NodeDescriptionPrefab, transform, false);
            
        }
        
        NodeDescriptionInstance.transform.position = node.transform.position;
        NodeDescriptionInstance.gameObject.SetActive(true);
        NodeDescriptionInstance.SetText(node.GetDescription());
    }

    public void HideNodeDescription()
    {
        NodeDescriptionInstance.gameObject.SetActive(false);
    }
    
    
}
