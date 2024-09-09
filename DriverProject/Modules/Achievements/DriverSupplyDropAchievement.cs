using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace RobDriver.Modules.Achievements
{
    internal class SupplyDropAchievement : ModdedUnlockable
    {
        public override string AchievementIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_REWARD_ID";
        public override string AchievementNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_ACHIEVEMENT_NAME";
        public override string PrerequisiteUnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_REWARD_ID";
        public override string UnlockableNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_UNLOCKABLE_NAME";
        public override string AchievementDescToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_ACHIEVEMENT_DESC";
        public override Sprite Sprite => Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texSupplyDropIcon");

        public override Func<string> GetHowToUnlock => () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);
        public override Func<string> GetUnlocked => () => Language.GetStringFormatted("UNLOCKED_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUPPLY_DROP_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);

        public static bool weaponHasDespawned;

        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RobDriverBody");

        public override void OnInstall()
        {
            base.OnInstall();

            TeleporterInteraction.onTeleporterBeginChargingGlobal += TeleporterInteraction_onTeleporterBeginChargingGlobal;
            TeleporterInteraction.onTeleporterFinishGlobal += TeleporterInteraction_onTeleporterFinishGlobal;
        }

        private void TeleporterInteraction_onTeleporterFinishGlobal(TeleporterInteraction obj)
        {
            if (base.meetsBodyRequirement && !weaponHasDespawned)
            {
                base.Grant();
            }
        }

        private void TeleporterInteraction_onTeleporterBeginChargingGlobal(TeleporterInteraction obj) => weaponHasDespawned = false;

        public override void OnUninstall()
        {
            base.OnUninstall();

            TeleporterInteraction.onTeleporterBeginChargingGlobal -= TeleporterInteraction_onTeleporterBeginChargingGlobal;
            TeleporterInteraction.onTeleporterFinishGlobal -= TeleporterInteraction_onTeleporterFinishGlobal;
        }
    }
}