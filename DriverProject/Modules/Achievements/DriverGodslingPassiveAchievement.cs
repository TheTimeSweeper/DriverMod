using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace RobDriver.Modules.Achievements
{
    internal class DriverGodslingPassiveAchievement : ModdedUnlockable
    {
        public override string AchievementIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_REWARD_ID";
        public override string AchievementNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_ACHIEVEMENT_NAME";
        public override string PrerequisiteUnlockableIdentifier => DriverPlugin.developerPrefix + "_DRIVER_BODY_UNLOCKABLE_REWARD_ID";
        public override string UnlockableNameToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_UNLOCKABLE_NAME";
        public override string AchievementDescToken => DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_ACHIEVEMENT_DESC";
        public override Sprite Sprite => Modules.Assets.mainAssetBundle.LoadAsset<Sprite>("texAltPassiveIcon");

        public override Func<string> GetHowToUnlock => () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);
        public override Func<string> GetUnlocked => () => Language.GetStringFormatted("UNLOCKED_FORMAT",
                            [
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_ACHIEVEMENT_NAME"),
                                Language.GetString(DriverPlugin.developerPrefix + "_DRIVER_BODY_GODSLING_UNLOCKABLE_ACHIEVEMENT_DESC")
                            ]);

        public static bool weaponPickedUpHard;

        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("RobDriverBody");

        public override void OnInstall()
        {
            base.OnInstall();

            Run.onClientGameOverGlobal += this.ClearCheck;
            Run.onRunStartGlobal += this.Reset;
        }

        private void Reset(Run run) => weaponPickedUpHard = false;

        public void ClearCheck(Run run, RunReport runReport)
        {
            if (weaponPickedUpHard || !base.meetsBodyRequirement || !run || !runReport?.gameEnding || runReport.ruleBook is null)
                return;

            if (runReport.gameEnding.isWin)
            {
                var difficultyDef = DifficultyCatalog.GetDifficultyDef(runReport.ruleBook.FindDifficulty());

                if (difficultyDef?.countsAsHardMode == true)
                {
                    base.Grant();
                }
            }
        }

        public override void OnUninstall()
        {
            base.OnUninstall();

            Run.onClientGameOverGlobal -= this.ClearCheck;
            Run.onRunStartGlobal -= this.Reset;
        }
    }
}