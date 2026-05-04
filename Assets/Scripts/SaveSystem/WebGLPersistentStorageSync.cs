using System.Runtime.InteropServices;
using UnityEngine;

namespace SaveSystem
{
    internal static class WebGLPersistentStorageSync
    {
        public static void Flush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                SkillTreeSyncPersistentDataPath();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Failed to sync WebGL persistent data: {exception.Message}");
            }
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SkillTreeSyncPersistentDataPath();
#endif
    }
}
