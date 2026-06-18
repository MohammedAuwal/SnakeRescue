using UnityEngine;
using SnakeRescue.Data;
using SnakeRescue.Systems;
using SnakeRescue.Core;

namespace SnakeRescue.Systems
{
    /// <summary>
    /// Manages all cosmetic unlocks and equipment.
    ///
    /// Responsibilities:
    /// - Check if skins are unlocked based on stars
    /// - Equip/Unequip skins
    /// - Notify UI when cosmetics change
    /// - Save cosmetic state to PlayerData
    ///
    /// This system does NOT handle visuals directly.
    /// It tells PrincessSkinApplier what to show.
    /// </summary>
    public class CosmeticSystem : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────
        public static CosmeticSystem Instance { get; private set; }

        // ─── Events ───────────────────────────────────────────
        public static event System.Action<string> OnSkinEquipped;
        public static event System.Action<string> OnSkinUnlocked;

        // ─── Runtime ──────────────────────────────────────────
        public string CurrentSkinID { get; private set; } = "Default";

        // ─── Unity Lifecycle ──────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCurrentSkin();
        }

        private void OnEnable()
        {
            ProgressManager.Instance?.CheckAndGrantCosmeticUnlocks();
        }

        // ─── Load/Save ────────────────────────────────────────

        private void LoadCurrentSkin()
        {
            if (SaveSystem.Instance?.CurrentData != null)
            {
                CurrentSkinID = SaveSystem.Instance.CurrentData.EquippedPrincessSkin;
            }
            else
            {
                CurrentSkinID = "Default";
            }
        }

        // ─── Public API ───────────────────────────────────────

        /// <summary>
        /// Check if a skin is unlocked for the player.
        /// </summary>
        public bool IsSkinUnlocked(string skinID)
        {
            if (SaveSystem.Instance?.CurrentData == null) return false;
            return SaveSystem.Instance.CurrentData.UnlockedSkins.Contains(skinID);
        }

        /// <summary>
        /// Equip a skin. Returns false if not unlocked.
        /// </summary>
        public bool EquipSkin(string skinID)
        {
            if (!IsSkinUnlocked(skinID))
            {
                Debug.LogWarning($"[Cosmetic] Skin {skinID} is locked.");
                return false;
            }

            CurrentSkinID = skinID;

            // Save to player data
            if (SaveSystem.Instance?.CurrentData != null)
            {
                SaveSystem.Instance.CurrentData.EquipSkin(skinID);
                SaveSystem.Instance.SaveData();
            }

            OnSkinEquipped?.Invoke(skinID);
            Debug.Log($"[Cosmetic] Equipped skin: {skinID}");

            return true;
        }

        /// <summary>
        /// Unlock a skin permanently.
        /// </summary>
        public void UnlockSkin(string skinID)
        {
            if (SaveSystem.Instance?.CurrentData == null) return;

            bool newlyUnlocked = SaveSystem.Instance.CurrentData.UnlockSkin(skinID);

            if (newlyUnlocked)
            {
                OnSkinUnlocked?.Invoke(skinID);
                SaveSystem.Instance.SaveData();
                Debug.Log($"[Cosmetic] Unlocked skin: {skinID}");
            }
        }

        /// <summary>
        /// Get skin definition from GameConfig.
        /// </summary>
        public SkinDefinition GetSkinDefinition(string skinID)
        {
            if (GameConfig.Instance?.AvailableSkins == null) return null;

            foreach (SkinDefinition skin in GameConfig.Instance.AvailableSkins)
            {
                if (skin.SkinID == skinID)
                    return skin;
            }

            return null;
        }

        /// <summary>
        /// Get all available skins for UI display.
        /// </summary>
        public SkinDefinition[] GetAllSkins()
        {
            if (GameConfig.Instance?.AvailableSkins == null)
                return new SkinDefinition[0];

            return GameConfig.Instance.AvailableSkins.ToArray();
        }
    }
}
