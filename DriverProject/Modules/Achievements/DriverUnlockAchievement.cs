using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace RobDriver.Modules.Achievements
{
    internal class DriverUnlockAchievement : ModdedUnlockable
    {
        public override string AchievementIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_REWARD_ID";
        public override string AchievementNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_ACHIEVEMENT_NAME";
        public override string PrerequisiteUnlockableIdentifier => "";
        public override string UnlockableNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_UNLOCKABLE_NAME";
        public override string AchievementDescToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_ACHIEVEMENT_DESC";
        public override Sprite Sprite => Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texDriverAchievement");

        public override Func<string> GetHowToUnlock => () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);
        public override Func<string> GetUnlocked => () => Language.GetStringFormatted("UNLOCKED_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);

        private void Check(CharacterBody characterBody)
        {
            if (Run.instance is null) return;

            if (Run.instance.stageClearCount >= 2 && Run.instance.time <= 900f)
            {
                base.Grant();
            }
        }

        public override void OnInstall()
        {
            base.OnInstall();

            CharacterBody.onBodyStartGlobal += Check;
        }

        public override void OnUninstall()
        {
            base.OnUninstall();

            CharacterBody.onBodyStartGlobal -= Check;
        }
    }
}