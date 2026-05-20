using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Battle
{
    public class LocationBattleBackgroundView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer backgroundSpriteRenderer;
        [SerializeField] private Sprite fallbackSprite;

        [Inject(Optional = true)] private EnemySpawner _enemySpawner;
        private Sprite _initialBackground;

        private void Awake()
        {
            if (backgroundSpriteRenderer == null)
                backgroundSpriteRenderer = GetComponent<SpriteRenderer>();

            _initialBackground = backgroundSpriteRenderer.sprite;

            if (_enemySpawner == null)
                _enemySpawner = FindAnyObjectByType<EnemySpawner>();
        }

        private void OnEnable()
        {
            if (_enemySpawner != null)
                _enemySpawner.OnLocationChanged += RefreshBackground;

            RefreshBackground();
        }

        private void OnDisable()
        {
            if (_enemySpawner != null)
                _enemySpawner.OnLocationChanged -= RefreshBackground;
        }

        private void RefreshBackground()
        {
            Sprite background = _enemySpawner != null && _enemySpawner.SelectedLocation != null
                ? _enemySpawner.SelectedLocation.BattleBackground
                : null;

            if (background == null)
                background = fallbackSprite != null ? fallbackSprite : _initialBackground;

            ApplyBackground(background);
        }

        private void ApplyBackground(Sprite background)
        {
            if (backgroundSpriteRenderer != null)
                backgroundSpriteRenderer.sprite = background;
        }
    }
}
