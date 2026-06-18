using UnityEngine;
using SnakeRescue.Systems;
using SnakeRescue.Data;

namespace SnakeRescue.Characters.Princess
{
    /// <summary>
    /// Applies cosmetic skins to the Princess character.
    ///
    /// Attach this to the Princess Prefab.
    /// It listens to CosmeticSystem for equip events
    /// and updates SpriteRenderer materials/colors.
    ///
    /// Supports:
    /// - Main Sprite change
    /// - Color tint override
    /// - Particle color change
    /// </summary>
    public class PrincessSkin : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────
        [Header("Components")]
        [SerializeField] private SpriteRenderer _mainSprite;
        [SerializeField] private SpriteRenderer _dressSprite;
        [SerializeField] private ParticleSystem  _particles;

        // ─── Default Assets ───────────────────────────────────
        [Header("Default Assets")]
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Color  _defaultTint = Color.white;

        // ─── Runtime ──────────────────────────────────────────
        private string _currentSkinID = "Default";

        // ─── Unity Lifecycle ──────────────────────────────────

        private void OnEnable()
        {
            CosmeticSystem.OnSkinEquipped += OnSkinEquipped;
            ApplyCurrentSkin();
        }

        private void OnDisable()
        {
            CosmeticSystem.OnSkinEquipped -= OnSkinEquipped;
        }

        private void Start()
        {
            ApplyCurrentSkin();
        }

        // ─── Skin Application ─────────────────────────────────

        private void OnSkinEquipped(string skinID)
        {
            _currentSkinID = skinID;
            ApplyCurrentSkin();
        }

        public void ApplyCurrentSkin()
        {
            SkinDefinition skin = CosmeticSystem.Instance?.GetSkinDefinition(_currentSkinID);

            if (skin == null)
            {
                ApplyDefault();
                return;
            }

            // Apply sprite if defined in config
            if (skin.PreviewSprite != null && _mainSprite != null)
            {
                _mainSprite.sprite = skin.PreviewSprite;
            }

            // Apply tint (using preview color as proxy for now)
            // In full implementation, SkinDefinition would have Color field
            if (_mainSprite != null)
            {
                _mainSprite.color = _defaultTint;
            }

            Debug.Log($"[PrincessSkin] Applied skin: {skin.DisplayName}");
        }

        private void ApplyDefault()
        {
            if (_mainSprite != null)
            {
                _mainSprite.sprite = _defaultSprite;
                _mainSprite.color  = _defaultTint;
            }

            _currentSkinID = "Default";
        }

        // ─── Particle Color ───────────────────────────────────

        public void SetParticleColor(Color color)
        {
            if (_particles == null) return;

            var main = _particles.main;
            main.startColor = color;
        }

        // ─── Debug ────────────────────────────────────────────

        [ContextMenu("Apply Default Skin")]
        private void DebugApplyDefault()
        {
            ApplyDefault();
        }

        [ContextMenu("Apply Random Unlockable Skin")]
        private void DebugApplyRandom()
        {
            SkinDefinition[] skins = CosmeticSystem.Instance?.GetAllSkins();
            if (skins == null || skins.Length == 0) return;

            int index = Random.Range(0, skins.Length);
            CosmeticSystem.Instance?.EquipSkin(skins[index].SkinID);
        }
    }
}
