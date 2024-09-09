﻿
using RobDriver.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RobDriver
{
    public static class DriverWeaponCatalog
    {
        public static Dictionary<string, DriverWeaponDef> weaponDrops = [];
        public static DriverWeaponDef[] weaponDefs = [];

        internal static DriverWeaponDef Pistol;
        internal static DriverWeaponDef PyriteGun;
        internal static DriverWeaponDef GoldenGun;
        internal static DriverWeaponDef RocketLauncher;
        internal static DriverWeaponDef PrototypeRocketLauncher;
        internal static DriverWeaponDef ArmCannon;
        internal static DriverWeaponDef PlasmaCannon;
        internal static DriverWeaponDef Behemoth;
        internal static DriverWeaponDef BeetleShield;
        internal static DriverWeaponDef LunarPistol;
        internal static DriverWeaponDef VoidPistol;
        internal static DriverWeaponDef Needler;
        internal static DriverWeaponDef GolemRifle;
        internal static DriverWeaponDef LunarRifle;
        internal static DriverWeaponDef LunarHammer;
        internal static DriverWeaponDef NemmandoGun;
        internal static DriverWeaponDef NemmercGun;
        internal static DriverWeaponDef RavSword;
        internal static DriverWeaponDef Shotgun;
        internal static DriverWeaponDef RiotShotgun;
        internal static DriverWeaponDef SlugShotgun;
        internal static DriverWeaponDef MachineGun;
        internal static DriverWeaponDef HeavyMachineGun;
        internal static DriverWeaponDef Sniper;
        internal static DriverWeaponDef Bazooka;
        internal static DriverWeaponDef GrenadeLauncher;
        internal static DriverWeaponDef BadassShotgun;

        public static void AddWeapon(DriverWeaponDef weaponDef)
        {
            Array.Resize(ref weaponDefs, weaponDefs.Length + 1);

            var index = weaponDefs.Length - 1;
            weaponDef.index = (ushort)index;

            weaponDefs[index] = weaponDef;
            weaponDef.index = (ushort)index;

            // heheheha
            weaponDef.pickupPrefab = Modules.Assets.CreatePickupObject(weaponDef);

            // set default icon
            if (!weaponDef.icon)
            {
                switch (weaponDef.tier)
                {
                    case DriverWeaponTier.Common:
                        weaponDef.icon = Modules.Assets.mainAssetBundle.LoadAsset<Texture>("texGenericWeaponGrey");
                        break;
                    case DriverWeaponTier.Uncommon:
                        weaponDef.icon = Modules.Assets.mainAssetBundle.LoadAsset<Texture>("texGenericWeaponGreen");
                        break;
                    case DriverWeaponTier.Legendary:
                        weaponDef.icon = Modules.Assets.mainAssetBundle.LoadAsset<Texture>("texGenericWeaponRed");
                        break;
                    case DriverWeaponTier.Unique:
                        weaponDef.icon = Modules.Assets.mainAssetBundle.LoadAsset<Texture>("texGenericWeaponYellow");
                        break;
                    case DriverWeaponTier.Lunar:
                        weaponDef.icon = Modules.Assets.mainAssetBundle.LoadAsset<Texture>("texGenericWeaponBlue");
                        break;
                    case DriverWeaponTier.Void:
                        weaponDef.icon = Modules.Assets.mainAssetBundle.LoadAsset<Texture>("texGenericWeaponPurple");
                        break;
                }
            }

            // add config
            Modules.Config.InitWeaponConfig(weaponDef);

            Debug.Log("Added " + weaponDef.nameToken + " to catalog with index: " + weaponDef.index);
        }

        public static void AddWeaponDrop(string bodyName, DriverWeaponDef weaponDef, bool autoComplete = true)
        {
            if (string.IsNullOrWhiteSpace(bodyName)) return;

            if (autoComplete)
            {
                if (!bodyName.Contains("Body")) bodyName += "Body";
                if (!bodyName.Contains("(Clone)")) bodyName += "(Clone)";
            }
            if (weaponDrops.ContainsKey(bodyName)) return;
            weaponDrops.Add(bodyName, weaponDef);
        }

        public static bool IsWeaponPistol(DriverWeaponDef weaponDef)
        {
            // These are all the pistol options that are forced upgrades with steadyaim
            // beetle shield doesnt count since it's dropped instead of reloaded
            return weaponDef && (weaponDef == Pistol ||
                weaponDef == LunarPistol ||
                weaponDef == VoidPistol ||
                weaponDef == Needler ||
                weaponDef == PyriteGun);
        }

        public static DriverWeaponDef GetWeaponFromIndex(int index) => weaponDefs.ElementAtOrDefault(index) ?? Pistol;

        public static DriverWeaponDef GetRandomWeapon()
        {
            var validWeapons = new List<DriverWeaponDef>();

            for (var i = 0; i < weaponDefs.Length; i++)
            {
                if (Modules.Config.GetWeaponConfigEnabled(weaponDefs[i]) && weaponDefs[i].shotCount > 0) validWeapons.Add(weaponDefs[i]);
            }

            if (validWeapons.Count <= 0) return Pistol; // pistol failsafe

            return validWeapons[UnityEngine.Random.Range(0, validWeapons.Count)];
        }

        public static DriverWeaponDef GetRandomWeaponFromTier(DriverWeaponTier tier)
        {
            var validWeapons = new List<DriverWeaponDef>();

            for (var i = 0; i < weaponDefs.Length; i++)
            {
                var weaponDef = weaponDefs[i];
                if (weaponDef)
                {
                    if (Config.uniqueDropsAreLegendary.Value && tier == DriverWeaponTier.Legendary)
                    {
                        if (weaponDef.tier >= tier && Modules.Config.GetWeaponConfigEnabled(weaponDef)) 
                            validWeapons.Add(weaponDef);
                    }
                    else
                    {
                        if (weaponDef.tier == tier && Modules.Config.GetWeaponConfigEnabled(weaponDef)) 
                            validWeapons.Add(weaponDef);
                    }
                }
            }

            if (validWeapons.Count <= 0) return Pistol; // pistol failsafe if you disabled rocket launcher like a fucking retard or something

            return validWeapons[UnityEngine.Random.Range(0, validWeapons.Count)];
        }
    }
}