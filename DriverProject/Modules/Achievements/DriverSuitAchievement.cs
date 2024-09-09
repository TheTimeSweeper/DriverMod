using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace RobDriver.Modules.Achievements
{
    internal class SuitAchievement : ModdedUnlockable
    {
        public override string AchievementIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_REWARD_ID";
        public override string AchievementNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_ACHIEVEMENT_NAME";
        public override string PrerequisiteUnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_REWARD_ID";
        public override string UnlockableNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_UNLOCKABLE_NAME";
        public override string AchievementDescToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_ACHIEVEMENT_DESC";
        public override Sprite Sprite => Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texSuitSkin");

        public override Func<string> GetHowToUnlock => () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);
        public override Func<string> GetUnlocked => () => Language.GetStringFormatted("UNLOCKED_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_SUIT_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);

        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RobDriverBody");

        public override void OnInstall()
        {
            base.OnInstall();

            RoR2.GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        }

        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport damageReport)
        {
            if (base.meetsBodyRequirement && damageReport.victimIsChampion && damageReport.attackerBody && damageReport.attackerBody.baseNameToken == Modules.Survivors.Driver.bodyNameToken)
            {
                var iDrive = damageReport.attackerBody.GetComponent<Modules.Components.DriverController>();
                if (iDrive && iDrive.weaponDef == DriverWeaponCatalog.Sniper)
                {
                    base.Grant();
                }
            }
        }

        public override void OnUninstall()
        {
            base.OnUninstall();

            RoR2.GlobalEventManager.onCharacterDeathGlobal -= GlobalEventManager_onCharacterDeathGlobal;
        }
    }
}