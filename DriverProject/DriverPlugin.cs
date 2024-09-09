using BepInEx;
using R2API.Utils;
using RoR2;
using System.Security;
using System.Security.Permissions;
using R2API.Networking;
using RobDriver.Modules.Survivors;
using System.Runtime.CompilerServices;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace RobDriver
{
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.TeamMoonstorm.Starstorm2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.ContactLight.LostInTransit", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.TeamMoonstorm.Starstorm2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.RiskySleeps.ClassicItemsReturns", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.GreenAlienHead", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bubbet.riskui", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rob.Ravager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class DriverPlugin : BaseUnityPlugin
    {
        public const string MODUID = "com.rob.Driver";
        public const string MODNAME = "Driver";
        public const string MODVERSION = "1.8.0";

        public const string developerPrefix = "ROB";
        public const string developerBodyPrefix = "ROB_DRIVER_BODY";

        public static DriverPlugin instance;

        public static bool starstormInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.TeamMoonstorm.Starstorm2");
        public static bool scepterInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.DestroyedClone.AncientScepter");
        public static bool rooInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool litInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.ContactLight.LostInTransit");
        public static bool classicItemsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.RiskySleeps.ClassicItemsReturns");
        public static bool riskUIInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("bubbet.riskui");
        public static bool extendedLoadoutInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ExtendedLoadout");
        public static bool greenAlienHeadInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.Borbo.GreenAlienHead");
        public static bool ravagerInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rob.Ravager");

        private void Awake()
        {
            instance = this;

            Modules.Config.myConfig = Config;

            Log.Init(Logger);
            Modules.Config.ReadConfig();
            Modules.Assets.PopulateAssets();
            Modules.CameraParams.InitializeParams();
            Modules.States.RegisterStates();
            Modules.DamageTypes.Init();
            Modules.DriverBulletCatalog.Init();
            Modules.Buffs.RegisterBuffs();
            Modules.Projectiles.RegisterProjectiles();
            Modules.Tokens.AddTokens();
            Modules.ItemDisplays.PopulateDisplays();
            Modules.NetMessages.RegisterNetworkMessages();

            new Driver().CreateCharacter();

            NetworkingAPI.RegisterMessageType<Modules.Components.SyncWeapon>();
            NetworkingAPI.RegisterMessageType<Modules.Components.SyncOverlay>();
            NetworkingAPI.RegisterMessageType<Modules.Components.SyncStoredWeapon>();
            NetworkingAPI.RegisterMessageType<Modules.Components.SyncDecapitation>();

            Hook();

            new Modules.ContentPacks().Initialize();

            RoR2.ContentManagement.ContentManager.onContentPacksAssigned += (_) =>
            {
                Driver.SetItemDisplays();
                Driver.LateSkinSetup();
            };

            CreateWeapons();

        }

        private void CreateWeapons()
        {
            new Modules.Weapons.ArmBFG().Init();
            new Modules.Weapons.CrabGun().Init();
            new Modules.Weapons.LunarGrenade().Init();
            new Modules.Weapons.ScavGun().Init();
            new Modules.Weapons.ArtiGauntlet().Init();
            new Modules.Weapons.BanditRevolver().Init();
            new Modules.Weapons.CommandoSMG().Init();
            new Modules.Weapons.Revolver().Init();
            new Modules.Weapons.SMG().Init();
            new Modules.Weapons.RavSword().Init();
            new Modules.Weapons.NemSword().Init();
        }

        private void Hook()
        {
            if (Modules.Config.dynamicCrosshairUniversal.Value)
                On.RoR2.UI.CrosshairController.Awake += CrosshairController_Awake;
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;

            // uncomment this if network testing
            // just download nuxlar's MultiplayerModTesting ffs, youre just gonna forget to comment it out again
            //On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };
        }

        private void CrosshairController_Awake(On.RoR2.UI.CrosshairController.orig_Awake orig, RoR2.UI.CrosshairController self)
        {
            orig(self);

            if (!self.name.Contains("SprintCrosshair") && !self.GetComponent<Modules.Components.DynamicCrosshair>())
            {
                self.gameObject.AddComponent<Modules.Components.DynamicCrosshair>();
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody self, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (self)
            {
                if (self.HasBuff(Modules.Buffs.woundDebuff))
                    args.armorAdd -= 40f;

                if (self.HasBuff(Modules.Buffs.syringeDamageBuff))
                    args.baseDamageAdd += self.level * 2f;

                if (self.HasBuff(Modules.Buffs.syringeAttackSpeedBuff))
                    args.baseAttackSpeedAdd += 0.5f;

                if (self.HasBuff(Modules.Buffs.syringeCritBuff))
                    args.critAdd += 30f;

                if (self.HasBuff(Modules.Buffs.syringeNewBuff))
                {
                    args.baseAttackSpeedAdd += 0.5f;
                    args.baseRegenAdd += 5f;
                }

                if (self.HasBuff(Modules.Buffs.syringeScepterBuff))
                {
                    args.baseDamageAdd += self.level * 2.5f;
                    args.baseAttackSpeedAdd += 0.75f;
                    args.critAdd += 40f;
                    args.baseRegenAdd += 10f;
                }
            }
        }

        public static float GetICBMDamageMult(CharacterBody body)
        {
            var mult = 1f;
            if (body && body.inventory)
            {
                var itemcount = body.inventory.GetItemCount(DLC1Content.Items.MoreMissile);
                var stack = itemcount - 1;
                if (stack > 0) mult += stack * 0.5f;
            }
            return mult;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool CheckIfBodyIsTerminal(CharacterBody body)
        {
            if (DriverPlugin.starstormInstalled)
                return _CheckIfBodyIsTerminal(body);
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool _CheckIfBodyIsTerminal(CharacterBody body) => body.HasBuff(Moonstorm.Starstorm2.SS2Content.Buffs.BuffTerminationReady);
    }
}