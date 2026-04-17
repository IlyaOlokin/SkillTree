using UnityEngine;
using UnityEngine.SceneManagement;

namespace MenuTree
{
    public class MenuSceneNodeAction : MenuNodeAction
    {
        [SerializeField] private string sceneName;
        [SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        protected override void OnAllocated(MenuNode node)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning($"Menu scene node '{name}' has no scene name configured.", this);
                return;
            }

            SceneManager.LoadScene(sceneName, loadSceneMode);
        }
    }
}
