using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RobDriver.Modules
{
    internal static class ItemDisplays
    {
        private static Dictionary<string, GameObject> itemDisplayPrefabs = new();

        internal static void PopulateDisplays()
        {
            PopulateFromBody("Commando");
            PopulateFromBody("Croco");

            // i forgot this cursed code was lying here
            // waht the fuck dude?
            var fuckYou = Assets.mainAssetBundle.LoadAsset<GameObject>("DriverStunGrenadeGhost").InstantiateClone("DriverStunGrenadeGhost", true);//ItemDisplays.LoadDisplay("DisplayStunGrenade").InstantiateClone("DriverStunGrenadeGhost", true);
            fuckYou.AddComponent<RoR2.Projectile.ProjectileGhostController>();
            fuckYou.AddComponent<NetworkIdentity>();

            var model = GameObject.Instantiate(ItemDisplays.LoadDisplay("DisplayStunGrenade"));
            model.transform.parent = fuckYou.transform;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * 3f;

            Assets.stunGrenadeModelPrefab = fuckYou;
            Modules.Projectiles.stunGrenadeProjectilePrefab.GetComponent<RoR2.Projectile.ProjectileController>().ghostPrefab = Assets.stunGrenadeModelPrefab;
        }

        private static void PopulateFromBody(string bodyName)
        {
            var itemDisplayRuleSet = Resources.Load<GameObject>("Prefabs/CharacterBodies/" + bodyName + "Body").GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>().itemDisplayRuleSet;

            var item = itemDisplayRuleSet.keyAssetRuleGroups;

            for (var i = 0; i < item.Length; i++)
            {
                var rules = item[i].displayRuleGroup.rules;

                for (var j = 0; j < rules.Length; j++)
                {
                    var followerPrefab = rules[j].followerPrefab;
                    if (followerPrefab)
                    {
                        var name = followerPrefab.name;
                        var key = (name != null) ? name.ToLower() : null;
                        if (!itemDisplayPrefabs.ContainsKey(key))
                        {
                            itemDisplayPrefabs[key] = followerPrefab;
                        }
                    }
                }
            }
        }

        internal static GameObject LoadDisplay(string name)
        {
            if (itemDisplayPrefabs.ContainsKey(name.ToLower()))
            {
                if (itemDisplayPrefabs[name.ToLower()]) return itemDisplayPrefabs[name.ToLower()];
            }

            Debug.LogError("Could not find display prefab " + name);

            return null;
        }
    }
}