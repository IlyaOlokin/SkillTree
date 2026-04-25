using System.Collections.Generic;
using Battle;
using InventorySystem;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;
using UnityEngine.Scripting.APIUpdating;

namespace DropSystem
{
    [MovedFrom(true, "DropSystem", null, "EnemyGemDropSpawner")]
    public class EnemyItemDropSpawner : MonoBehaviour
    {
        private const string BattleCameraTag = "BattleCamera";

        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private ItemDropPickup itemDropPrefab;
        [SerializeField] private Transform dropRoot;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private float spawnRadius = 0.5f;
        [SerializeField] [Min(1)] private int defaultCapacity = 8;
        [SerializeField] [Min(1)] private int maxPoolSize = 64;

        [Inject] private DiContainer container;

        private ObjectPool<ItemDropPickup> pickupPool;
        private readonly List<ItemDropPickup> _activePickups = new();

        private void Awake()
        {
            if (enemySpawner == null)
                enemySpawner = GetComponent<EnemySpawner>();

            if (battleCamera == null)
                battleCamera = FindBattleCamera();

            pickupPool = new ObjectPool<ItemDropPickup>(
                CreatePickup,
                OnTakePickupFromPool,
                OnReturnPickupToPool,
                OnDestroyPickup,
                false,
                defaultCapacity,
                maxPoolSize);
        }

        private void OnEnable()
        {
            if (enemySpawner != null)
            {
                enemySpawner.OnEnemyDropsResolved += HandleEnemyDropsResolved;
                enemySpawner.OnLocationRewardsResolved += HandleLocationRewardsResolved;
                enemySpawner.OnBattleActivityChanged += HandleBattleActivityChanged;
            }
        }

        private void OnDisable()
        {
            if (enemySpawner != null)
            {
                enemySpawner.OnEnemyDropsResolved -= HandleEnemyDropsResolved;
                enemySpawner.OnLocationRewardsResolved -= HandleLocationRewardsResolved;
                enemySpawner.OnBattleActivityChanged -= HandleBattleActivityChanged;
            }
        }

        private void HandleEnemyDropsResolved(EnemyUnit enemy, IReadOnlyList<InventoryItem> drops)
        {
            SpawnDrops(drops, enemy.transform.position);
        }

        private void HandleLocationRewardsResolved(IReadOnlyList<InventoryItem> rewards, Vector3 spawnCenter)
        {
            SpawnDrops(rewards, spawnCenter);
        }

        private void SpawnDrops(IReadOnlyList<InventoryItem> drops, Vector3 spawnCenter)
        {
            if (drops == null)
                return;

            for (int i = 0; i < drops.Count; i++)
            {
                InventoryItem dropItem = drops[i];
                if (dropItem == null || dropItem.IsEmpty)
                    continue;

                Vector3 spawnPosition = spawnCenter + GetSpawnOffset(i, drops.Count);
                ItemDropPickup pickupInstance = pickupPool.Get();
                pickupInstance.transform.SetParent(dropRoot != null ? dropRoot : transform);
                pickupInstance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                pickupInstance.SetCamera(battleCamera);
                pickupInstance.Initialize(dropItem, playerInventory, ReleasePickup);
                RegisterActivePickup(pickupInstance);
            }
        }

        private Vector3 GetSpawnOffset(int dropIndex, int totalDrops)
        {
            if (totalDrops <= 1 || spawnRadius <= 0f)
                return Vector3.zero;

            float angle = 360f * dropIndex / totalDrops;
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            return new Vector3(direction.x, direction.y, 0f) * spawnRadius;
        }

        private ItemDropPickup CreatePickup()
        {
            Transform parent = dropRoot != null ? dropRoot : transform;
            ItemDropPickup pickupInstance = container.InstantiatePrefabForComponent<ItemDropPickup>(itemDropPrefab, parent);
            pickupInstance.SetInventory(playerInventory);
            pickupInstance.SetCamera(battleCamera);
            pickupInstance.gameObject.SetActive(false);
            return pickupInstance;
        }

        private static void OnTakePickupFromPool(ItemDropPickup pickup)
        {
            pickup.gameObject.SetActive(true);
        }

        private void OnReturnPickupToPool(ItemDropPickup pickup)
        {
            _activePickups.Remove(pickup);
            pickup.transform.SetParent(dropRoot != null ? dropRoot : transform);
            pickup.gameObject.SetActive(false);
        }

        private static void OnDestroyPickup(ItemDropPickup pickup)
        {
            if (pickup != null)
                Destroy(pickup.gameObject);
        }

        private void ReleasePickup(ItemDropPickup pickup)
        {
            if (pickup == null || pickupPool == null)
                return;

            pickupPool.Release(pickup);
        }

        public void ClearActiveDrops()
        {
            if (pickupPool == null || _activePickups.Count == 0)
                return;

            // Release from the end because releasing removes entries from the active list.
            for (int i = _activePickups.Count - 1; i >= 0; i--)
            {
                ItemDropPickup pickup = _activePickups[i];
                if (pickup == null)
                    continue;

                pickup.Release();
            }

            _activePickups.Clear();
        }

        private void OnDestroy()
        {
            pickupPool?.Dispose();
        }

        private void HandleBattleActivityChanged(bool isBattleActive)
        {
            if (!isBattleActive)
                ClearActiveDrops();
        }

        private Camera FindBattleCamera()
        {
            GameObject battleCameraObject = GameObject.FindWithTag(BattleCameraTag);
            if (battleCameraObject == null)
                return null;

            Camera cameraComponent = battleCameraObject.GetComponent<Camera>();
            if (cameraComponent != null)
                return cameraComponent;

            return battleCameraObject.GetComponentInChildren<Camera>();
        }

        private void LateUpdate()
        {
            // Track active pooled pickups centrally so they can be cleared on location exit.
            for (int i = 0; i < _activePickups.Count; i++)
            {
                if (_activePickups[i] != null)
                    continue;

                _activePickups.RemoveAt(i);
                i--;
            }
        }

        private void RegisterActivePickup(ItemDropPickup pickup)
        {
            if (pickup == null || _activePickups.Contains(pickup))
                return;

            _activePickups.Add(pickup);
        }
    }
}
