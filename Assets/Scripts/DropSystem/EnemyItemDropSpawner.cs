using System;
using Battle;
using CurrencySystem;
using DG.Tweening;
using InventorySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Visual;
using Zenject;
using UnityEngine.Scripting.APIUpdating;

namespace DropSystem
{
    [MovedFrom(true, "DropSystem", null, "EnemyGemDropSpawner")]
    public class EnemyItemDropSpawner : MonoBehaviour
    {
        private const string BattleCameraTag = "BattleCamera";
        private const string RuntimeRootName = "LootFlyouts";

        [Header("Scene references")]
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform flyoutRoot;
        [SerializeField] private RectTransform inventoryFlyTarget;
        [SerializeField] private RectTransform goldFlyTarget;
        [SerializeField] private Camera battleCamera;

        [Header("View")]
        [SerializeField] private LootFlyoutIcon flyoutIconPrefab;
        [SerializeField] private Sprite goldIconSprite;
        [SerializeField] private Color fallbackGoldIconColor = new(1f, 0.74f, 0.16f, 1f);
        [SerializeField] private UnitNotificationEffect goldNotificationPrefab;
        [SerializeField] private Vector2 runtimeIconSize = new(48f, 48f);
        [SerializeField] private Vector2 inventoryTargetOffset = new(48f, -48f);
        [SerializeField] private Vector2 goldTargetOffset = new(-48f, -48f);

        [Header("Motion")]
        [SerializeField] private float autoPickupDelay = 0.45f;
        [SerializeField] private float dropBurstDuration = 0.32f;
        [SerializeField] private float flyDuration = 0.65f;
        [SerializeField] private float arcHeight = 90f;
        [SerializeField] private float spawnSpread = 42f;
        [SerializeField] private Ease dropBurstEase = Ease.OutCubic;
        [SerializeField] [Range(0f, 1f)] private float endScale = 0.1f;
        [SerializeField] private Ease flyEase = Ease.InOutCubic;
        [SerializeField] private bool useUnscaledTime = true;

        [Inject] private DiContainer container;
        [Inject] private PlayerWallet playerWallet;

        private int _activeFlyouts;
        private Sprite _runtimeGoldIconSprite;

        public int ActiveDropCount => _activeFlyouts;
        public float AutoPickupDelay => Mathf.Max(0f, autoPickupDelay);

        private void Awake()
        {
            if (enemySpawner == null)
                enemySpawner = GetComponent<EnemySpawner>();

            if (battleCamera == null)
                battleCamera = FindBattleCamera();

            ResolveCanvasRoot();
        }

        private void OnEnable()
        {
            if (enemySpawner != null)
            {
                enemySpawner.OnEnemyGoldResolved += HandleEnemyGoldResolved;
                enemySpawner.OnBattleActivityChanged += HandleBattleActivityChanged;
            }
        }

        private void OnDisable()
        {
            if (enemySpawner != null)
            {
                enemySpawner.OnEnemyGoldResolved -= HandleEnemyGoldResolved;
                enemySpawner.OnBattleActivityChanged -= HandleBattleActivityChanged;
            }
        }

        private void HandleEnemyGoldResolved(EnemyUnit enemy, GoldDropResult goldDrop)
        {
            if (enemy == null || !goldDrop.HasGold)
                return;

            SpawnAutoPickupGold(goldDrop.Amount, enemy.transform.position);
        }

        private void SpawnAutoPickupGold(int amount, Vector3 worldPosition)
        {
            if (amount <= 0)
                return;

            Vector2 center = WorldToCanvasPosition(worldPosition);
            Vector2 settledPosition = center + GetDropBurstOffset(0, 1);
            FlyDroppedGoldToWallet(amount, center, settledPosition, AutoPickupDelay);
        }

        public void FlyItemFromRectToInventory(
            InventoryItem item,
            RectTransform source,
            float delay,
            Action onComplete = null)
        {
            if (source == null)
            {
                FlyItemToInventory(item, GetInventoryTargetPosition(), delay, onComplete);
                return;
            }

            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(GetCanvasEventCamera(), source.position);
            Vector2 startPosition = ScreenToCanvasPosition(screenPosition);
            FlyItemToInventory(item, startPosition, delay, onComplete);
        }

        public void FlyItemToInventory(
            InventoryItem item,
            Vector2 startPosition,
            float delay,
            Action onComplete = null)
        {
            if (item == null || item.IsEmpty)
            {
                onComplete?.Invoke();
                return;
            }

            ResolveCanvasRoot();
            if (flyoutRoot == null)
            {
                playerInventory?.TryAddItem(item, out _);
                onComplete?.Invoke();
                return;
            }

            InventoryItem itemCopy = item.CreateCopy() ?? item;
            LootFlyoutIcon flyoutIcon = CreateFlyoutIcon();
            flyoutIcon.EnsureVisibleSize(runtimeIconSize);
            flyoutIcon.Initialize(itemCopy);
            flyoutIcon.RectTransform.SetParent(flyoutRoot, false);
            flyoutIcon.RectTransform.SetAsLastSibling();

            _activeFlyouts++;
            flyoutIcon.Play(
                startPosition,
                GetInventoryTargetPosition(),
                Mathf.Max(0f, delay),
                Mathf.Max(0.01f, flyDuration),
                arcHeight,
                endScale,
                flyEase,
                useUnscaledTime,
                () =>
                {
                    playerInventory?.TryAddItem(itemCopy, out _);
                    _activeFlyouts = Mathf.Max(0, _activeFlyouts - 1);

                    if (flyoutIcon != null)
                        Destroy(flyoutIcon.gameObject);

                    onComplete?.Invoke();
                });
        }

        public void FlyGoldToWallet(
            int amount,
            Vector2 startPosition,
            float delay,
            Action onComplete = null)
        {
            if (amount <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            ResolveCanvasRoot();
            if (flyoutRoot == null)
            {
                playerWallet?.AddGold(amount);
                onComplete?.Invoke();
                return;
            }

            LootFlyoutIcon flyoutIcon = CreateFlyoutIcon();
            flyoutIcon.EnsureVisibleSize(runtimeIconSize);
            flyoutIcon.Initialize(GetGoldIconSprite(), amount.ToString());
            flyoutIcon.RectTransform.SetParent(flyoutRoot, false);
            flyoutIcon.RectTransform.SetAsLastSibling();

            _activeFlyouts++;
            flyoutIcon.Play(
                startPosition,
                GetGoldTargetPosition(),
                Mathf.Max(0f, delay),
                Mathf.Max(0.01f, flyDuration),
                arcHeight,
                endScale,
                flyEase,
                useUnscaledTime,
                () =>
                {
                    playerWallet?.AddGold(amount);
                    _activeFlyouts = Mathf.Max(0, _activeFlyouts - 1);

                    if (flyoutIcon != null)
                        Destroy(flyoutIcon.gameObject);

                    onComplete?.Invoke();
                },
                () => ShowGoldNotification(flyoutIcon, amount));
        }

        private void FlyDroppedItemToInventory(
            InventoryItem item,
            Vector2 spawnPosition,
            Vector2 settledPosition,
            float delay,
            Action onComplete = null)
        {
            if (item == null || item.IsEmpty)
            {
                onComplete?.Invoke();
                return;
            }

            ResolveCanvasRoot();
            if (flyoutRoot == null)
            {
                playerInventory?.TryAddItem(item, out _);
                onComplete?.Invoke();
                return;
            }

            InventoryItem itemCopy = item.CreateCopy() ?? item;
            LootFlyoutIcon flyoutIcon = CreateFlyoutIcon();
            flyoutIcon.EnsureVisibleSize(runtimeIconSize);
            flyoutIcon.Initialize(itemCopy);
            flyoutIcon.RectTransform.SetParent(flyoutRoot, false);
            flyoutIcon.RectTransform.SetAsLastSibling();

            _activeFlyouts++;
            flyoutIcon.PlayDropThenFly(
                spawnPosition,
                settledPosition,
                GetInventoryTargetPosition(),
                Mathf.Max(0f, dropBurstDuration),
                dropBurstEase,
                Mathf.Max(0f, delay),
                Mathf.Max(0.01f, flyDuration),
                arcHeight,
                endScale,
                flyEase,
                useUnscaledTime,
                () =>
                {
                    playerInventory?.TryAddItem(itemCopy, out _);
                    _activeFlyouts = Mathf.Max(0, _activeFlyouts - 1);

                    if (flyoutIcon != null)
                        Destroy(flyoutIcon.gameObject);

                    onComplete?.Invoke();
                });
        }

        private void FlyDroppedGoldToWallet(
            int amount,
            Vector2 spawnPosition,
            Vector2 settledPosition,
            float delay,
            Action onComplete = null)
        {
            if (amount <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            ResolveCanvasRoot();
            if (flyoutRoot == null)
            {
                playerWallet?.AddGold(amount);
                onComplete?.Invoke();
                return;
            }

            LootFlyoutIcon flyoutIcon = CreateFlyoutIcon();
            flyoutIcon.EnsureVisibleSize(runtimeIconSize);
            flyoutIcon.Initialize(GetGoldIconSprite(), amount.ToString());
            flyoutIcon.RectTransform.SetParent(flyoutRoot, false);
            flyoutIcon.RectTransform.SetAsLastSibling();

            _activeFlyouts++;
            flyoutIcon.PlayDropThenFly(
                spawnPosition,
                settledPosition,
                GetGoldTargetPosition(),
                Mathf.Max(0f, dropBurstDuration),
                dropBurstEase,
                Mathf.Max(0f, delay),
                Mathf.Max(0.01f, flyDuration),
                arcHeight,
                endScale,
                flyEase,
                useUnscaledTime,
                () =>
                {
                    playerWallet?.AddGold(amount);
                    _activeFlyouts = Mathf.Max(0, _activeFlyouts - 1);

                    if (flyoutIcon != null)
                        Destroy(flyoutIcon.gameObject);

                    onComplete?.Invoke();
                },
                () => ShowGoldNotification(flyoutIcon, amount));
        }

        public Vector2 GetInventoryTargetPosition()
        {
            ResolveCanvasRoot();

            if (inventoryFlyTarget != null)
            {
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(GetCanvasEventCamera(), inventoryFlyTarget.position);
                return ScreenToCanvasPosition(screenPosition);
            }

            RectTransform root = flyoutRoot;
            if (root == null)
                return Vector2.zero;

            Rect rect = root.rect;
            return new Vector2(rect.xMin + inventoryTargetOffset.x, rect.yMax + inventoryTargetOffset.y);
        }

        public Vector2 GetGoldTargetPosition()
        {
            ResolveCanvasRoot();

            if (goldFlyTarget != null)
            {
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(GetCanvasEventCamera(), goldFlyTarget.position);
                return ScreenToCanvasPosition(screenPosition);
            }

            RectTransform root = flyoutRoot;
            if (root == null)
                return Vector2.zero;

            Rect rect = root.rect;
            return new Vector2(rect.xMax + goldTargetOffset.x, rect.yMax + goldTargetOffset.y);
        }

        public void SetGoldFlyTarget(RectTransform target)
        {
            ResolveCanvasRoot();

            Canvas targetOwnerCanvas = target != null ? target.GetComponentInParent<Canvas>() : null;
            if (target != null &&
                targetCanvas != null &&
                targetOwnerCanvas != null &&
                targetOwnerCanvas != targetCanvas)
            {
                Debug.LogWarning(
                    $"{nameof(EnemyItemDropSpawner)} ignored gold fly target '{target.name}' because it belongs to canvas '{targetOwnerCanvas.name}', but loot flyouts use canvas '{targetCanvas.name}'. Use a target under the same canvas.",
                    this);
                return;
            }

            goldFlyTarget = target;
        }

        private Vector2 WorldToCanvasPosition(Vector3 worldPosition)
        {
            Camera camera = battleCamera != null ? battleCamera : FindBattleCamera();
            Vector2 screenPosition = camera != null
                ? RectTransformUtility.WorldToScreenPoint(camera, worldPosition)
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            return ScreenToCanvasPosition(screenPosition);
        }

        private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            ResolveCanvasRoot();
            if (flyoutRoot == null)
                return Vector2.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                flyoutRoot,
                screenPosition,
                GetCanvasEventCamera(),
                out Vector2 localPosition);

            return localPosition;
        }

        private Vector2 GetDropBurstOffset(int dropIndex, int totalDrops)
        {
            if (totalDrops <= 1 || spawnSpread <= 0f)
                return GetRandomDirection(dropIndex) * Mathf.Max(0f, spawnSpread);

            Vector2 direction = GetRandomDirection(dropIndex);
            float distance = UnityEngine.Random.Range(spawnSpread * 0.65f, spawnSpread);
            return direction * distance;
        }

        private Vector2 GetRandomDirection(int fallbackIndex)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle;
            if (direction.sqrMagnitude > 0.001f)
                return direction.normalized;

            float angle = 137.5f * fallbackIndex;
            return Quaternion.Euler(0f, 0f, angle) * Vector2.right;
        }

        private LootFlyoutIcon CreateFlyoutIcon()
        {
            if (flyoutIconPrefab != null)
            {
                return container != null
                    ? container.InstantiatePrefabForComponent<LootFlyoutIcon>(flyoutIconPrefab, flyoutRoot)
                    : Instantiate(flyoutIconPrefab, flyoutRoot);
            }

            GameObject iconObject = new("LootFlyoutIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
            RectTransform rectTransform = iconObject.transform as RectTransform;
            rectTransform.SetParent(flyoutRoot, false);
            rectTransform.sizeDelta = runtimeIconSize;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = iconObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            GameObject amountObject = new("Amount", typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform amountTransform = amountObject.transform as RectTransform;
            amountTransform.SetParent(rectTransform, false);
            amountTransform.anchorMin = new Vector2(1f, 0f);
            amountTransform.anchorMax = new Vector2(1f, 0f);
            amountTransform.pivot = new Vector2(1f, 0f);
            amountTransform.anchoredPosition = Vector2.zero;
            amountTransform.sizeDelta = new Vector2(42f, 22f);

            TMP_Text amountText = amountObject.AddComponent<TextMeshProUGUI>();
            amountText.alignment = TextAlignmentOptions.BottomRight;
            amountText.fontSize = 16f;
            amountText.raycastTarget = false;

            LootFlyoutIcon icon = iconObject.AddComponent<LootFlyoutIcon>();
            icon.Configure(image, amountText, iconObject.GetComponent<CanvasGroup>());
            return icon;
        }

        private Sprite GetGoldIconSprite()
        {
            if (goldIconSprite != null)
                return goldIconSprite;

            if (_runtimeGoldIconSprite != null)
                return _runtimeGoldIconSprite;

            const int size = 32;
            const float radius = size * 0.42f;
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color transparent = new(0f, 0f, 0f, 0f);
            Color highlight = Color.Lerp(Color.white, fallbackGoldIconColor, 0.35f);
            Color shadow = Color.Lerp(Color.black, fallbackGoldIconColor, 0.65f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, transparent);
                        continue;
                    }

                    float vertical = Mathf.InverseLerp(size - 1, 0f, y);
                    Color color = Color.Lerp(highlight, shadow, vertical);
                    if (distance > radius * 0.82f)
                        color = Color.Lerp(color, Color.black, 0.22f);

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            _runtimeGoldIconSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _runtimeGoldIconSprite.name = "RuntimeGoldIcon";
            return _runtimeGoldIconSprite;
        }

        private void ShowGoldNotification(LootFlyoutIcon flyoutIcon, int amount)
        {
            if (amount <= 0 || flyoutIcon == null || flyoutRoot == null)
                return;

            UnitNotificationEffect notification = goldNotificationPrefab != null
                ? Instantiate(goldNotificationPrefab, flyoutIcon.RectTransform.position, Quaternion.identity)
                : CreateFallbackGoldNotification(flyoutIcon.RectTransform.position);

            if (notification == null)
                return;

            notification.SetWorldCamera(GetCanvasEventCamera() ?? battleCamera ?? FindBattleCamera());
            notification.WriteMessage($"+{amount}");
        }

        private UnitNotificationEffect CreateFallbackGoldNotification(Vector3 worldPosition)
        {
            GameObject notificationObject = new("GoldNotification", typeof(RectTransform), typeof(UnitNotificationEffect));
            RectTransform rectTransform = notificationObject.transform as RectTransform;
            rectTransform.SetParent(flyoutRoot, false);
            rectTransform.position = worldPosition;
            rectTransform.sizeDelta = new Vector2(160f, 42f);
            return notificationObject.GetComponent<UnitNotificationEffect>();
        }

        private void ResolveCanvasRoot()
        {
            if (flyoutRoot != null)
            {
                Canvas flyoutCanvas = flyoutRoot.GetComponentInParent<Canvas>();
                if (flyoutCanvas != null)
                    targetCanvas = flyoutCanvas;

                return;
            }

            if (targetCanvas == null)
                targetCanvas = GetComponentInParent<Canvas>();

            if (targetCanvas == null)
                targetCanvas = FindCanvasForBattleCamera();

            if (targetCanvas == null)
                return;

            RectTransform canvasTransform = targetCanvas.transform as RectTransform;
            if (canvasTransform == null)
                return;

            Transform existingRoot = canvasTransform.Find(RuntimeRootName);
            if (existingRoot is RectTransform existingRect)
            {
                flyoutRoot = existingRect;
                return;
            }

            GameObject rootObject = new(RuntimeRootName, typeof(RectTransform));
            RectTransform rootRect = rootObject.transform as RectTransform;
            rootRect.SetParent(canvasTransform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            flyoutRoot = rootRect;
        }

        private Canvas FindCanvasForBattleCamera()
        {
            Camera camera = battleCamera != null ? battleCamera : FindBattleCamera();
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (camera != null)
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas canvas = canvases[i];
                    if (canvas != null && canvas.worldCamera == camera)
                        return canvas;
                }
            }

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas != null && string.Equals(canvas.name, "BattleCanvas", StringComparison.Ordinal))
                    return canvas;
            }

            return null;
        }

        private Camera GetCanvasEventCamera()
        {
            if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return targetCanvas.worldCamera;
        }

        private void HandleBattleActivityChanged(bool isBattleActive)
        {
            if (!isBattleActive)
                _activeFlyouts = 0;
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
    }
}
