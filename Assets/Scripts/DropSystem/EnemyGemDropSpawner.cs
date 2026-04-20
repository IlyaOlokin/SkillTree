using System.Collections.Generic;
using Battle;
using InventorySystem;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace DropSystem
{
    public class EnemyGemDropSpawner : MonoBehaviour
    {
        private const string BattleCameraTag = "BattleCamera";

        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private GemDropPickup gemDropPrefab;
        [SerializeField] private Transform dropRoot;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private float spawnRadius = 0.5f;
        [SerializeField] [Min(1)] private int defaultCapacity = 8;
        [SerializeField] [Min(1)] private int maxPoolSize = 64;

        [Inject] private DiContainer container;

        private ObjectPool<GemDropPickup> pickupPool;
        private readonly List<GemDropPickup> _activePickups = new();

        private void Awake()
        {
            if (enemySpawner == null)
                enemySpawner = GetComponent<EnemySpawner>();

            if (battleCamera == null)
                battleCamera = FindBattleCamera();

            pickupPool = new ObjectPool<GemDropPickup>(
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
                enemySpawner.OnBattleActivityChanged += HandleBattleActivityChanged;
            }
        }

        private void OnDisable()
        {
            if (enemySpawner != null)
            {
                enemySpawner.OnEnemyDropsResolved -= HandleEnemyDropsResolved;
                enemySpawner.OnBattleActivityChanged -= HandleBattleActivityChanged;
            }
        }

        private void HandleEnemyDropsResolved(EnemyUnit enemy, IReadOnlyList<InventoryItem> drops)
        {
            if (enemy == null || drops == null || gemDropPrefab == null)
                return;

            Vector3 spawnCenter = enemy.transform.position;
            for (int i = 0; i < drops.Count; i++)
            {
                InventoryItem dropItem = drops[i];
                if (dropItem?.Gem == null)
                    continue;

                Vector3 spawnPosition = spawnCenter + GetSpawnOffset(i, drops.Count);
                GemDropPickup pickupInstance = pickupPool.Get();
                pickupInstance.transform.SetParent(dropRoot != null ? dropRoot : transform);
                pickupInstance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                pickupInstance.SetCamera(battleCamera);
                pickupInstance.Initialize(dropItem.Gem, playerInventory, ReleasePickup);
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

        private GemDropPickup CreatePickup()
        {
            Transform parent = dropRoot != null ? dropRoot : transform;
            GemDropPickup pickupInstance = container.InstantiatePrefabForComponent<GemDropPickup>(gemDropPrefab, parent);
            pickupInstance.SetInventory(playerInventory);
            pickupInstance.SetCamera(battleCamera);
            pickupInstance.gameObject.SetActive(false);
            return pickupInstance;
        }

        private static void OnTakePickupFromPool(GemDropPickup pickup)
        {
            pickup.gameObject.SetActive(true);
        }

        private void OnReturnPickupToPool(GemDropPickup pickup)
        {
            _activePickups.Remove(pickup);
            pickup.transform.SetParent(dropRoot != null ? dropRoot : transform);
            pickup.gameObject.SetActive(false);
        }

        private static void OnDestroyPickup(GemDropPickup pickup)
        {
            if (pickup != null)
                Destroy(pickup.gameObject);
        }

        private void ReleasePickup(GemDropPickup pickup)
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
                GemDropPickup pickup = _activePickups[i];
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

        private void RegisterActivePickup(GemDropPickup pickup)
        {
            if (pickup == null || _activePickups.Contains(pickup))
                return;

            _activePickups.Add(pickup);
        }
    }
}
