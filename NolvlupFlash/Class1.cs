#nullable disable
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine;

namespace NolvlupFlash
{
    [BepInPlugin("com.FortressForce.NolvlupFlash", "No Level Up Flash", "2.0.0")]
    public class NolvlupFlashPlugin : BaseUnityPlugin
    {
        public enum LevelUpMode
        {
            Both,   // Flash AND Ding (Vanilla)
            Flash,  // Visuals only
            Ding,   // Sound only
            None    // Complete silence
        }

        // Configuration Entries
        private static ConfigEntry<LevelUpMode> ConfigPlayer;
        private static ConfigEntry<LevelUpMode> ConfigEnemies;
        private static ConfigEntry<LevelUpMode> ConfigDrones;
        private static ConfigEntry<LevelUpMode> ConfigDevotion;

        // Cache indices to avoid constant string lookups
        private ItemIndex devotionItemIndex = ItemIndex.None;
        private ArtifactDef devotionArtifactDef = null;

        public void Awake()
        {
            // 1. SETUP CONFIGURATION
            ConfigPlayer = Config.Bind("Categories", "Player", LevelUpMode.Both, "Behavior when YOU level up.");
            ConfigEnemies = Config.Bind("Categories", "Enemies", LevelUpMode.None, "Behavior when Monsters/Bosses level up.");
            ConfigDrones = Config.Bind("Categories", "Drones", LevelUpMode.None, "Behavior when mechanical allies (Drones, Turrets) level up.");
            ConfigDevotion = Config.Bind("Categories", "Devotion Minions", LevelUpMode.None, "Behavior when Devotion-bound Lemurians level up.");

            // 2. SETUP HOOKS
            On.RoR2.CharacterBody.OnLevelUp += CharacterBody_OnLevelUp;

            // 3. FIND DEVOTION CONTENT (Safely)
            // We wait for the catalogs to load
            RoR2Application.onLoad += () =>
            {
                devotionItemIndex = ItemCatalog.FindItemIndex("DevotionItem");
                devotionArtifactDef = ArtifactCatalog.FindArtifactDef("Devotion");
            };

            Logger.LogInfo("NolvlupFlash 2.0 loaded! Ready to categorize.");
        }

        private void CharacterBody_OnLevelUp(On.RoR2.CharacterBody.orig_OnLevelUp orig, CharacterBody self)
        {
            // --- DECISION TREE: WHO IS LEVELING UP? ---
            LevelUpMode selectedMode = LevelUpMode.Both; // Default to Vanilla

            if (self.isPlayerControlled)
            {
                selectedMode = ConfigPlayer.Value;
            }
            else if (self.teamComponent.teamIndex != TeamIndex.Player)
            {
                // If they aren't on our team, they are enemies (Monsters/Void/Lunar)
                selectedMode = ConfigEnemies.Value;
            }
            else
            {
                // If we are here, it is a Non-Player Ally (Minion)

                // CHECK 1: Is it a Devotion Minion?
                // Logic: 
                // A) Has the Devotion Item (Standard check)
                // B) OR The Devotion Artifact is enabled AND it is a Lemurian (Fallback/User Preference)
                bool isDevotion = false;

                // Check A: Item Presence
                if (devotionItemIndex != ItemIndex.None && self.inventory)
                {
#pragma warning disable CS0618 // Disable obsolete warning for GetItemCount
                    if (self.inventory.GetItemCount(devotionItemIndex) > 0)
                    {
                        isDevotion = true;
                    }
#pragma warning restore CS0618
                }

                // Check B: Artifact + Body Type (If Item check didn't catch it)
                if (!isDevotion && devotionArtifactDef != null)
                {
                    if (RunArtifactManager.instance.IsArtifactEnabled(devotionArtifactDef))
                    {
                        string bodyName = self.baseNameToken;
                        // LemurianBody or LemurianBruiserBody usually have "LEMURIAN" in their name token
                        if (bodyName.Contains("LEMURIAN"))
                        {
                            isDevotion = true;
                        }
                    }
                }

                // CHECK 2: Is it a Drone?
                // We check the "Mechanical" flag (Drones, Turrets, Squid Polyps usually)
                bool isDrone = self.bodyFlags.HasFlag(CharacterBody.BodyFlags.Mechanical);

                if (isDevotion)
                {
                    selectedMode = ConfigDevotion.Value;
                }
                else if (isDrone)
                {
                    selectedMode = ConfigDrones.Value;
                }
                else
                {
                    // Fallback for other minions (Beetle Guards, Ghosts, Aurelionite)
                    // We treat them as Drones for now to keep config simple
                    selectedMode = ConfigDrones.Value;
                }
            }

            // --- EXECUTE MODE ---
            switch (selectedMode)
            {
                case LevelUpMode.Both:
                    orig(self); // Run vanilla code
                    break;

                case LevelUpMode.None:
                    // Do nothing
                    break;

                case LevelUpMode.Ding:
                    Util.PlaySound("Play_UI_levelUp", self.gameObject);
                    break;

                case LevelUpMode.Flash:
                    // Use standard Unity Resources.Load to get the effect
                    GameObject vfxPrefab = Resources.Load<GameObject>("Prefabs/Effects/LevelUpEffect");
                    if (vfxPrefab)
                    {
                        GameObject effect = Object.Instantiate(vfxPrefab, self.transform.position, self.transform.rotation);
                        effect.transform.parent = self.transform;
                    }
                    break;
            }
        }
    }
}