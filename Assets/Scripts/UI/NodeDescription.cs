using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class NodeDescription : MonoBehaviour
{
    [SerializeField] private TMP_Text description;

    public void SetText(string text)
    {
        description.text = text;
    }
}
