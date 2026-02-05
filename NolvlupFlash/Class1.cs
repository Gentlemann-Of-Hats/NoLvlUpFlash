using BepInEx; // <--- This line enables "BepInPlugin"
using BepInEx.Configuration;
using RoR2;
using UnityEngine;

namespace NolvlupFlash
{
    // Make sure this attribute is above the class
    [BepInPlugin("com.GentleMannOfHats.NolvlupFlash", "No Level Up Flash", "1.0.0")]
    public class NolvlupFlashPlugin : BaseUnityPlugin
    {
        public enum LevelUpMode
        {
            Both,   // Flash AND Ding
            Flash,  // Visuals only
            Ding,   // Sound only
            None    // Complete silence (Your Default)
        }

        private static ConfigEntry<LevelUpMode> MinionLevelUpBehavior;

        public void Awake()
        {
            // Set default to LevelUpMode.None as requested
            MinionLevelUpBehavior = Config.Bind(
                "General",
                "Minion Behavior",
                LevelUpMode.None,
                "What should happen when a minion (Drone/Lemurian) levels up?"
            );

            On.RoR2.CharacterBody.OnLevelUp += CharacterBody_OnLevelUp;
            Logger.LogInfo("NolvlupFlash loaded! Default behavior is NONE.");
        }

        private void CharacterBody_OnLevelUp(On.RoR2.CharacterBody.orig_OnLevelUp orig, CharacterBody self)
        {
            if (self.isPlayerControlled)
            {
                orig(self);
                return;
            }

            LevelUpMode mode = MinionLevelUpBehavior.Value;

            switch (mode)
            {
                case LevelUpMode.Both:
                    orig(self);
                    break;

                case LevelUpMode.None:
                    // Do nothing
                    break;

                case LevelUpMode.Ding:
                    Util.PlaySound("Play_UI_levelUp", self.gameObject);
                    break;

                case LevelUpMode.Flash:
                    // Use standard Unity Resources.Load. Simple and reliable.
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