using UnityEngine;

[CreateAssetMenu(fileName = "rsd", menuName = "ScriptableObjects/DriverWeaponSkinDef", order = 3)]
public class DriverWeaponSkinDef : ScriptableObject
{
    [Header("General")]
    public string nameToken = "";
    public string mainSkinName = "";
    public ushort weaponDefIndex;
    public Mesh weaponSkinMesh;
    public Material weaponSkinMaterial;

    public static DriverWeaponSkinDef CreateWeaponSkinDefFromInfo(DriverWeaponSkinDefInfo skinDefInfo)
    {
        var weaponSkinDef = (DriverWeaponSkinDef)ScriptableObject.CreateInstance(typeof(DriverWeaponSkinDef));
        weaponSkinDef.nameToken = skinDefInfo.nameToken;
        weaponSkinDef.mainSkinName = skinDefInfo.mainSkinName;
        weaponSkinDef.weaponDefIndex = skinDefInfo.weaponDefIndex;
        weaponSkinDef.weaponSkinMesh = skinDefInfo.weaponSkinMesh;
        weaponSkinDef.weaponSkinMaterial = skinDefInfo.weaponSkinMaterial;
        return weaponSkinDef;
    }
    [System.Serializable]
    public struct DriverWeaponSkinDefInfo
    {
        public string nameToken;
        public string mainSkinName;
        public ushort weaponDefIndex;
        public Mesh weaponSkinMesh;
        public Material weaponSkinMaterial;
    }
}