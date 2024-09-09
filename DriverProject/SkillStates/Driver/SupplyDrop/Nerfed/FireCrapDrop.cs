using RobDriver.Modules;
using RobDriver.Modules.Components;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RobDriver.SkillStates.Driver.SupplyDrop.Nerfed
{
    public class FireCrapDrop : FireSupplyDrop
    {
        protected override void SpawnWeapon()
        {
            if (NetworkServer.active)
            {
                DriverWeaponDef _weaponDef;
                DriverBulletDef _bulletDef;

                if (Modules.Config.randomSupplyDrop.Value)
                {
                    _weaponDef = DriverWeaponCatalog.GetRandomWeapon();
                    _bulletDef = DriverBulletCatalog.GetWeightedRandomBullet(DriverWeaponTier.Legendary);

                }
                else
                {
                    _weaponDef = DriverWeaponCatalog.GetRandomWeaponFromTier(DriverWeaponTier.Uncommon);
                    _bulletDef = DriverBulletCatalog.GetWeightedRandomBullet(DriverWeaponTier.Uncommon);
                }

                var weaponPickup = UnityEngine.Object.Instantiate<GameObject>(_weaponDef.pickupPrefab, this.dropPosition, UnityEngine.Random.rotation);

                var weaponComponent = weaponPickup.GetComponent<SyncPickup>();
                weaponComponent.bulletDef = _bulletDef;
                weaponComponent.cutAmmo = true;

                var teamFilter = weaponPickup.GetComponent<TeamFilter>();
                if (teamFilter) teamFilter.teamIndex = this.teamComponent.teamIndex;

                NetworkServer.Spawn(weaponPickup);
            }
        }
    }
}