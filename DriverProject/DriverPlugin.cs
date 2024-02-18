﻿using System;
using BepInEx;
using R2API.Utils;
using RoR2;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using R2API.Networking;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace RobDriver
{
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.TeamMoonstorm.Starstorm2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.ContactLight.LostInTransit", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.TeamMoonstorm.Starstorm2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.RiskySleeps.ClassicItemsReturns", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("bubbet.riskui", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    [R2APISubmoduleDependency(new string[]
    {
        "PrefabAPI",
        "LanguageAPI",
        "SoundAPI",
        "DirectorAPI",
        "LoadoutAPI",
        "UnlockableAPI",
        "NetworkingAPI",
        "RecalculateStatsAPI",
    })]

    public class DriverPlugin : BaseUnityPlugin
    {
        public const string MODUID = "com.rob.Driver";
        public const string MODNAME = "Driver";
        public const string MODVERSION = "1.4.0";

        public const string developerPrefix = "ROB";

        public static DriverPlugin instance;

        public static bool starstormInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.TeamMoonstorm.Starstorm2");
        public static bool scepterInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.DestroyedClone.AncientScepter");
        public static bool rooInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        public static bool litInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.ContactLight.LostInTransit");
        public static bool classicItemsInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.RiskySleeps.ClassicItemsReturns");
        public static bool riskUIInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("bubbet.riskui");
        public static bool extendedLoadoutInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.KingEnderBrine.ExtendedLoadout");

        private void Awake()
        {
            instance = this;

            Modules.Config.myConfig = Config;

            Log.Init(Logger);
            Modules.Config.ReadConfig();
            Modules.Assets.PopulateAssets();
            Modules.CameraParams.InitializeParams();
            Modules.States.RegisterStates();
            Modules.Buffs.RegisterBuffs();
            Modules.Projectiles.RegisterProjectiles();
            Modules.Tokens.AddTokens();
            Modules.ItemDisplays.PopulateDisplays();
            Modules.NetMessages.RegisterNetworkMessages();

            new Modules.Survivors.Driver().CreateCharacter();

            NetworkingAPI.RegisterMessageType<Modules.Components.SyncWeapon>();
            //NetworkingAPI.RegisterMessageType<Modules.Components.SyncWeaponPickup>();
            // kill me
            NetworkingAPI.RegisterMessageType<Modules.Components.SyncOverlay>();

            Hook();

            new Modules.ContentPacks().Initialize();

            RoR2.ContentManagement.ContentManager.onContentPacksAssigned += LateSetup;

            CreateWeapons();
        }

        private void LateSetup(global::HG.ReadOnlyArray<RoR2.ContentManagement.ReadOnlyContentPack> obj)
        {
            Modules.Survivors.Driver.SetItemDisplays();
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
        }

        private void Hook()
        {
            if (Modules.Config.dynamicCrosshairUniversal.Value) On.RoR2.UI.CrosshairController.Awake += CrosshairController_Awake;
            //R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;

            // uncomment this if network testing
            //On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (self && self.HasBuff(Modules.Buffs.woundDebuff))
            {
                self.armor -= 20f;
            }

            if (self && self.HasBuff(Modules.Buffs.syringeDamageBuff))
            {
                self.damage += self.level * 1.5f;
            }

            if (self && self.HasBuff(Modules.Buffs.syringeAttackSpeedBuff))
            {
                self.attackSpeed += 0.3f;
            }

            if (self && self.HasBuff(Modules.Buffs.syringeCritBuff))
            {
                self.crit += 25f;
            }
        }

        private void CrosshairController_Awake(On.RoR2.UI.CrosshairController.orig_Awake orig, RoR2.UI.CrosshairController self)
        {
            orig(self);

            if (!self.name.Contains("SprintCrosshair"))
            {
                if (!self.GetComponent<Modules.Components.DynamicCrosshair>())
                {
                    self.gameObject.AddComponent<Modules.Components.DynamicCrosshair>();
                }
            }
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args) {

            /*if (sender.HasBuff(Modules.Buffs.armorBuff)) {

                args.armorAdd += 500f;
            }

            if (sender.HasBuff(Modules.Buffs.slowStartBuff)) {

                args.armorAdd += 20f;
                args.moveSpeedReductionMultAdd += 1f; //movespeed *= 0.5f // 1 + 1 = divide by 2?
                args.attackSpeedMultAdd -= 0.5f; //attackSpeed *= 0.5f;
                args.damageMultAdd -= 0.5f; //damage *= 0.5f;
            }*/
        }

        public static float GetICBMDamageMult(CharacterBody body)
        {
            float mult = 1f;
            if (body && body.inventory)
            {
                int itemcount = body.inventory.GetItemCount(DLC1Content.Items.MoreMissile);
                int stack = itemcount - 1;
                if (stack > 0) mult += stack * 0.5f;
            }
            return mult;
        }

        public static bool CheckIfBodyIsTerminal(CharacterBody body)
        {
            if (DriverPlugin.starstormInstalled) return _CheckIfBodyIsTerminal(body);
            return false;
        }

        public static bool _CheckIfBodyIsTerminal(CharacterBody body)
        {
            return body.HasBuff(Moonstorm.Starstorm2.SS2Content.Buffs.BuffTerminationReady);
        }
    }
}