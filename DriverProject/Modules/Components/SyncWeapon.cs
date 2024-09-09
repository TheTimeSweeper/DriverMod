using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
using UnityEngine;
using RoR2;

namespace RobDriver.Modules.Components
{
    internal class SyncWeapon : INetMessage
    {
        private NetworkInstanceId netId;
        private ushort weaponIndex;
        private ushort bulletIndex;
        private bool cutAmmo;
        private bool isNewAmmoType;

        public SyncWeapon()
        {
        }

        public SyncWeapon(NetworkInstanceId netId, ushort augh, ushort ough, bool cutAmmo, bool isNewAmmoType)
        {
            this.netId = netId;
            this.weaponIndex = augh;
            this.bulletIndex = ough;
            this.cutAmmo = cutAmmo;
            this.isNewAmmoType = isNewAmmoType;
        }

        public void Deserialize(NetworkReader reader)
        {
            this.netId = reader.ReadNetworkId();
            this.weaponIndex = reader.ReadUInt16();
            this.bulletIndex = reader.ReadUInt16();
            this.cutAmmo = reader.ReadBoolean();
            this.isNewAmmoType = reader.ReadBoolean();
        }

        public void OnReceived()
        {
            var bodyObject = Util.FindNetworkObject(this.netId);
            if (!bodyObject) return;

            var iDrive = bodyObject.GetComponent<DriverController>();
            var weaponDef = DriverWeaponCatalog.GetWeaponFromIndex(this.weaponIndex);
            var bulletDef = DriverBulletCatalog.GetBulletDefFromIndex(this.bulletIndex);

            if (iDrive) iDrive.PickUpWeaponDrop(weaponDef, bulletDef, -1, this.cutAmmo, this.isNewAmmoType);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.netId);
            writer.Write(this.weaponIndex);
            writer.Write(this.bulletIndex);
            writer.Write(this.cutAmmo);
            writer.Write(this.isNewAmmoType);
        }
    }
}