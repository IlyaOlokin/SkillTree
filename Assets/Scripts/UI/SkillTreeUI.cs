using SkillTree;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    public static SkillTreeUI instance;
    [SerializeField] protected NodeDescription NodeDescriptionPrefab;
    private NodeDescription NodeDescriptionInstance;
    protected void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

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
