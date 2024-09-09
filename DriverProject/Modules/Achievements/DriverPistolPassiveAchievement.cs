using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace RobDriver.Modules.Achievements
{
    internal class DriverPistolPassiveAchievement : ModdedUnlockable
    {
        public override string AchievementIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_REWARD_ID";
        public override string AchievementNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_ACHIEVEMENT_NAME";
        public override string PrerequisiteUnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_REWARD_ID";
        public override string UnlockableNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_UNLOCKABLE_NAME";
        public override string AchievementDescToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_ACHIEVEMENT_DESC";
        public override Sprite Sprite => Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texAltPassiveIcon");

        public override Func<string> GetHowToUnlock => () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);
        public override Func<string> GetUnlocked => () => Language.GetStringFormatted("UNLOCKED_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_PISTOLPASSIVE_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);

        public static bool weaponPickedUp;

        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RobDriverBody");

        public override void OnInstall()
        {
            base.OnInstall();

            TeleporterInteraction.onTeleporterBeginChargingGlobal += TeleporterInteraction_onTeleporterBeginChargingGlobal;
            TeleporterInteraction.onTeleporterFinishGlobal += TeleporterInteraction_onTeleporterFinishGlobal;
        }

        private void TeleporterInteraction_onTeleporterFinishGlobal(TeleporterInteraction obj)
        {
            if (base.meetsBodyRequirement && !weaponPickedUp)
            {
                base.Grant();
            }
        }

        private void TeleporterInteraction_onTeleporterBeginChargingGlobal(TeleporterInteraction obj) => weaponPickedUp = false;

        public override void OnUninstall()
        {
            base.OnUninstall();

            TeleporterInteraction.onTeleporterBeginChargingGlobal -= TeleporterInteraction_onTeleporterBeginChargingGlobal;
            TeleporterInteraction.onTeleporterFinishGlobal -= TeleporterInteraction_onTeleporterFinishGlobal;
        }
    }
}