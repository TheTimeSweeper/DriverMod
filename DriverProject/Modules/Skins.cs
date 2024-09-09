using R2API;
using RoR2;
using UnityEngine;

namespace RobDriver.Modules
{
    public static class Skins
    {
        public static SkinDef CreateSkinDef(string skinName, Sprite skinIcon, CharacterModel.RendererInfo[] rendererInfos, SkinnedMeshRenderer mainRenderer, GameObject root, UnlockableDef unlockableDef = null)
        {
            var skinDefInfo = new SkinDefInfo
            {
                BaseSkins = [],
                GameObjectActivations = [],
                Icon = skinIcon,
                MeshReplacements = [],
                MinionSkinReplacements = [],
                Name = skinName,
                NameToken = skinName,
                ProjectileGhostReplacements = [],
                RendererInfos = rendererInfos,
                RootObject = root,
                UnlockableDef = unlockableDef
            };

            return R2API.Skins.CreateNewSkinDef(skinDefInfo);
        }
    }
}