using System;
using System.Collections.Generic;
using AudioSystem;
using SaveSystem;
using UnityEngine;
using Zenject;

namespace MenuTree
{
    public class MenuVolumeZone : MonoBehaviour
    {
        private const int FullVolumeNodeCount = 10;
        private const float VolumePerNode = 0.1f;

        public event Action OnAllocatedCountChanged;

        [SerializeField] private MenuVolumeTarget volumeTarget;
        [SerializeField] private List<MenuNode> nodes = new();
        [SerializeField] private bool syncNodesFromSavedVolume = true;
        [SerializeField] private bool saveOnAllocatedCountChanged = true;

        [Inject(Optional = true)] private LocalSettingsService localSettingsService;

        private LocalSettingsService fallbackLocalSettingsService;
        private bool fallbackLocalSettingsLoaded;
        private bool suppressNodeChangeHandling;

        public MenuVolumeTarget VolumeTarget => volumeTarget;
        public int AllocatedNodesCount { get; private set; }
        public float CurrentVolume => CountToVolume(AllocatedNodesCount);
        public static float VolumePerAllocatedNode => VolumePerNode;

        private void Awake()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node == null)
                    continue;

                node.OnAllocatedChanged += HandleNodeAllocationChanged;
            }

            RecalculateAllocatedNodesCount();
        }

        private void Start()
        {
            if (syncNodesFromSavedVolume)
                SyncNodesToVolume(GetSavedVolume());

            RecalculateAllocatedNodesCount();
            ApplyVolume(CurrentVolume, false);
            OnAllocatedCountChanged?.Invoke();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node != null)
                    node.OnAllocatedChanged -= HandleNodeAllocationChanged;
            }
        }

        private void HandleNodeAllocationChanged(MenuNode _)
        {
            if (suppressNodeChangeHandling)
                return;

            RecalculateAllocatedNodesCount();
            ApplyVolume(CurrentVolume, saveOnAllocatedCountChanged);
            OnAllocatedCountChanged?.Invoke();
        }

        private void SyncNodesToVolume(float volume)
        {
            int targetCount = VolumeToCount(volume);
            suppressNodeChangeHandling = true;

            try
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    MenuNode node = nodes[i];
                    if (node == null || !node.IsAllocated)
                        continue;

                    if (CountAllocatedNodes() <= targetCount)
                        break;

                    TrySetNodeAllocated(node, false);
                }

                for (int i = 0; i < nodes.Count; i++)
                {
                    MenuNode node = nodes[i];
                    if (node == null || node.IsAllocated)
                        continue;

                    if (CountAllocatedNodes() >= targetCount)
                        break;

                    TrySetNodeAllocated(node, true);
                }
            }
            finally
            {
                suppressNodeChangeHandling = false;
            }
        }

        private bool TrySetNodeAllocated(MenuNode node, bool allocated)
        {
            if (node == null)
                return false;

            if (node.TreeController != null)
            {
                return allocated
                    ? node.TreeController.TryAllocateNode(node)
                    : node.TreeController.TryDeallocateNode(node);
            }

            return allocated ? node.Allocate() : node.Deallocate();
        }

        private void RecalculateAllocatedNodesCount()
        {
            AllocatedNodesCount = CountAllocatedNodes();
        }

        private int CountAllocatedNodes()
        {
            int allocatedNodes = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                MenuNode node = nodes[i];
                if (node != null && node.IsAllocated)
                    allocatedNodes++;
            }

            return allocatedNodes;
        }

        private float GetSavedVolume()
        {
            LocalSettingsSaveData settings = ResolveLocalSettingsService()?.Current;
            if (settings == null)
                return 1f;

            return volumeTarget switch
            {
                MenuVolumeTarget.Master => settings.masterVolume,
                MenuVolumeTarget.Sfx => settings.sfxVolume,
                MenuVolumeTarget.Music => settings.musicVolume,
                _ => 1f
            };
        }

        private void ApplyVolume(float volume, bool save)
        {
            float clampedVolume = Mathf.Clamp01(volume);
            ApplyToGameAudio(clampedVolume);

            LocalSettingsService settingsService = ResolveLocalSettingsService();
            if (settingsService?.Current == null)
                return;

            switch (volumeTarget)
            {
                case MenuVolumeTarget.Master:
                    settingsService.Current.masterVolume = clampedVolume;
                    break;
                case MenuVolumeTarget.Sfx:
                    settingsService.Current.sfxVolume = clampedVolume;
                    break;
                case MenuVolumeTarget.Music:
                    settingsService.Current.musicVolume = clampedVolume;
                    break;
            }

            if (save)
                settingsService.Save();
        }

        private void ApplyToGameAudio(float volume)
        {
            if (GameAudio.Instance == null)
                return;

            switch (volumeTarget)
            {
                case MenuVolumeTarget.Master:
                    GameAudio.Instance.SetMasterVolume(volume);
                    break;
                case MenuVolumeTarget.Sfx:
                    GameAudio.Instance.SetSfxVolume(volume);
                    break;
                case MenuVolumeTarget.Music:
                    GameAudio.Instance.SetMusicVolume(volume);
                    break;
            }
        }

        private LocalSettingsService ResolveLocalSettingsService()
        {
            if (localSettingsService != null)
                return localSettingsService;

            if (fallbackLocalSettingsService == null)
            {
                fallbackLocalSettingsService = new LocalSettingsService(new SaveFileStorage(new SaveFileCodec()));
                fallbackLocalSettingsLoaded = false;
            }

            if (!fallbackLocalSettingsLoaded)
            {
                fallbackLocalSettingsService.Load();
                fallbackLocalSettingsLoaded = true;
            }

            return fallbackLocalSettingsService;
        }

        private static float CountToVolume(int allocatedNodesCount)
        {
            return Mathf.Clamp01(allocatedNodesCount * VolumePerNode);
        }

        private static int VolumeToCount(float volume)
        {
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(volume) / VolumePerNode), 0, FullVolumeNodeCount);
        }
    }

    public enum MenuVolumeTarget
    {
        Master,
        Sfx,
        Music
    }
}
