﻿using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RobDriver.Modules
{
    // module for creating body prefabs and whatnot
    // recommended to simply avoid touching this unless you REALLY need to

    internal static class Prefabs
    {
        // cache this just to give our ragdolls the same physic material as vanilla stuff
        private static PhysicMaterial ragdollMaterial;

        internal static List<SurvivorDef> survivorDefinitions = new();
        internal static List<GameObject> bodyPrefabs = new();
        internal static List<GameObject> masterPrefabs = new();
        internal static List<GameObject> projectilePrefabs = new();

        internal static void RegisterNewSurvivor(GameObject bodyPrefab, GameObject displayPrefab, string namePrefix, UnlockableDef unlockableDef)
        {
            var fullNameString = DriverPlugin.developerPrefix + "_" + namePrefix + "_BODY_NAME";
            var fullDescString = DriverPlugin.developerPrefix + "_" + namePrefix + "_BODY_DESCRIPTION";
            var fullOutroString = DriverPlugin.developerPrefix + "_" + namePrefix + "_BODY_OUTRO_FLAVOR";
            var fullFailureString = DriverPlugin.developerPrefix + "_" + namePrefix + "_BODY_OUTRO_FAILURE";

            var survivorDef = ScriptableObject.CreateInstance<SurvivorDef>();
            survivorDef.bodyPrefab = bodyPrefab;
            survivorDef.displayPrefab = displayPrefab;
            survivorDef.displayNameToken = fullNameString;
            survivorDef.descriptionToken = fullDescString;
            survivorDef.outroFlavorToken = fullOutroString;
            survivorDef.mainEndingEscapeFailureFlavorToken = fullFailureString;
            survivorDef.desiredSortPosition = 3.99f;
            survivorDef.unlockableDef = unlockableDef;
            survivorDef.cachedName = fullNameString;

            survivorDefinitions.Add(survivorDef);
        }

        internal static void RegisterNewSurvivor(GameObject bodyPrefab, GameObject displayPrefab, string namePrefix) => RegisterNewSurvivor(bodyPrefab, displayPrefab, namePrefix, null);

        internal static GameObject CreateDisplayPrefab(string modelName, GameObject prefab)
        {
            var newPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody"), modelName + "Prefab");

            var model = CreateModel(newPrefab, modelName);
            var modelBaseTransform = SetupModel(newPrefab, model.transform);

            model.AddComponent<CharacterModel>().baseRendererInfos = prefab.GetComponentInChildren<CharacterModel>().baseRendererInfos;
            model.AddComponent<Modules.Components.DriverCSS>();

            return model.gameObject;
        }

        internal static GameObject CreatePrefab(string bodyName, string modelName, BodyInfo bodyInfo)
        {
            var newPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody"), bodyName);

            var model = CreateModel(newPrefab, modelName);
            var modelBaseTransform = SetupModel(newPrefab, model.transform);

            #region CharacterBody
            var bodyComponent = newPrefab.GetComponent<CharacterBody>();

            bodyComponent.name = bodyInfo.bodyName;
            bodyComponent.baseNameToken = bodyInfo.bodyNameToken;
            bodyComponent.subtitleNameToken = bodyInfo.subtitleNameToken;
            bodyComponent.portraitIcon = bodyInfo.characterPortrait;
            bodyComponent._defaultCrosshairPrefab = bodyInfo.crosshair;
            bodyComponent.bodyColor = bodyInfo.bodyColor;

            bodyComponent.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            bodyComponent.rootMotionInMainState = false;

            bodyComponent.baseMaxHealth = bodyInfo.maxHealth;
            bodyComponent.levelMaxHealth = bodyInfo.healthGrowth;

            bodyComponent.baseRegen = bodyInfo.healthRegen;
            bodyComponent.levelRegen = bodyComponent.baseRegen * 0.2f;

            bodyComponent.baseMaxShield = bodyInfo.shield;
            bodyComponent.levelMaxShield = bodyInfo.shieldGrowth;

            bodyComponent.baseMoveSpeed = bodyInfo.moveSpeed;
            bodyComponent.levelMoveSpeed = bodyInfo.moveSpeedGrowth;

            bodyComponent.baseAcceleration = bodyInfo.acceleration;

            bodyComponent.baseJumpPower = bodyInfo.jumpPower;
            bodyComponent.levelJumpPower = bodyInfo.jumpPowerGrowth;

            bodyComponent.baseDamage = bodyInfo.damage;
            bodyComponent.levelDamage = bodyComponent.baseDamage * 0.2f;

            bodyComponent.baseAttackSpeed = bodyInfo.attackSpeed;
            bodyComponent.levelAttackSpeed = bodyInfo.attackSpeedGrowth;

            bodyComponent.baseArmor = bodyInfo.armor;
            bodyComponent.levelArmor = bodyInfo.armorGrowth;

            bodyComponent.baseCrit = bodyInfo.crit;
            bodyComponent.levelCrit = bodyInfo.critGrowth;

            bodyComponent.baseJumpCount = bodyInfo.jumpCount;

            bodyComponent.sprintingSpeedMultiplier = 1.45f;

            bodyComponent.hideCrosshair = false;
            bodyComponent.aimOriginTransform = modelBaseTransform.Find("AimOrigin");
            bodyComponent.hullClassification = HullClassification.Human;

            bodyComponent.preferredPodPrefab = bodyInfo.podPrefab;

            bodyComponent.isChampion = false;
            #endregion

            SetupCharacterDirection(newPrefab, modelBaseTransform, model.transform);
            SetupCameraTargetParams(newPrefab);
            SetupModelLocator(newPrefab, modelBaseTransform, model.transform);
            SetupRigidbody(newPrefab);
            SetupCapsuleCollider(newPrefab);
            SetupMainHurtbox(newPrefab, model);
            SetupFootstepController(model);
            SetupRagdoll(model);
            SetupAimAnimator(newPrefab, model);

            bodyPrefabs.Add(newPrefab);

            return newPrefab;
        }

        internal static void CreateGenericDoppelganger(GameObject bodyPrefab, string masterName, string masterToCopy)
        {
            var newMaster = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterMasters/" + masterToCopy + "MonsterMaster"), masterName, true);
            newMaster.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            masterPrefabs.Add(newMaster);
        }

        #region ModelSetup
        private static Transform SetupModel(GameObject prefab, Transform modelTransform)
        {
            var modelBase = new GameObject("ModelBase");
            modelBase.transform.parent = prefab.transform;
            modelBase.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            modelBase.transform.localRotation = Quaternion.identity;
            modelBase.transform.localScale = new Vector3(1f, 1f, 1f);

            var cameraPivot = new GameObject("CameraPivot");
            cameraPivot.transform.parent = modelBase.transform;
            cameraPivot.transform.localPosition = new Vector3(0f, 1.59f, 0f);
            cameraPivot.transform.localRotation = Quaternion.identity;
            cameraPivot.transform.localScale = Vector3.one;

            var aimOrigin = new GameObject("AimOrigin");
            aimOrigin.transform.parent = modelBase.transform;
            aimOrigin.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            aimOrigin.transform.localRotation = Quaternion.identity;
            aimOrigin.transform.localScale = Vector3.one;
            prefab.GetComponent<CharacterBody>().aimOriginTransform = aimOrigin.transform;

            modelTransform.parent = modelBase.transform;
            modelTransform.localPosition = Vector3.zero;
            modelTransform.localRotation = Quaternion.identity;
            modelTransform.localScale = Vector3.one;//new Vector3(0.55f, 0.55f, 0.55f);

            return modelBase.transform;
        }

        private static GameObject CreateModel(GameObject main, string modelName)
        {
            DriverPlugin.DestroyImmediate(main.transform.Find("ModelBase").gameObject);
            DriverPlugin.DestroyImmediate(main.transform.Find("CameraPivot").gameObject);
            DriverPlugin.DestroyImmediate(main.transform.Find("AimOrigin").gameObject);

            if (Modules.Assets.mainAssetBundle.LoadAsset<GameObject>(modelName) == null)
            {
                Log.Error("Trying to load a null model- check to see if the name in your code matches the name of the object in Unity");
                return null;
            }

            return GameObject.Instantiate(Modules.Assets.mainAssetBundle.LoadAsset<GameObject>(modelName));
        }

        internal static void SetupCharacterModel(GameObject prefab, CustomRendererInfo[] rendererInfo, int mainRendererIndex)
        {
            var characterModel = prefab.GetComponent<ModelLocator>().modelTransform.gameObject.AddComponent<CharacterModel>();
            var childLocator = characterModel.GetComponent<ChildLocator>();
            characterModel.body = prefab.GetComponent<CharacterBody>();

            var rendererInfos = new List<CharacterModel.RendererInfo>();

            for (var i = 0; i < rendererInfo.Length; i++)
            {
                rendererInfos.Add(new CharacterModel.RendererInfo
                {
                    renderer = childLocator.FindChild(rendererInfo[i].childName).GetComponent<Renderer>(),
                    defaultMaterial = rendererInfo[i].material,
                    ignoreOverlays = rendererInfo[i].ignoreOverlays,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                });
            }

            characterModel.baseRendererInfos = [.. rendererInfos];

            characterModel.autoPopulateLightInfos = true;
            characterModel.invisibilityCount = 0;
            characterModel.temporaryOverlays = [];

            characterModel.mainSkinnedMeshRenderer = characterModel.baseRendererInfos[mainRendererIndex].renderer.GetComponent<SkinnedMeshRenderer>();
        }
        #endregion

        #region ComponentSetup
        private static void SetupCharacterDirection(GameObject prefab, Transform modelBaseTransform, Transform modelTransform)
        {
            var characterDirection = prefab.GetComponent<CharacterDirection>();
            characterDirection.targetTransform = modelBaseTransform;
            characterDirection.overrideAnimatorForwardTransform = null;
            characterDirection.rootMotionAccumulator = null;
            characterDirection.modelAnimator = modelTransform.GetComponent<Animator>();
            characterDirection.driveFromRootRotation = false;
            characterDirection.turnSpeed = 720f;
        }

        private static void SetupCameraTargetParams(GameObject prefab)
        {
            var cameraTargetParams = prefab.GetComponent<CameraTargetParams>();
            cameraTargetParams.cameraParams = Resources.Load<GameObject>("Prefabs/CharacterBodies/MercBody").GetComponent<CameraTargetParams>().cameraParams;
            cameraTargetParams.cameraPivotTransform = prefab.transform.Find("ModelBase").Find("CameraPivot");
        }

        private static void SetupModelLocator(GameObject prefab, Transform modelBaseTransform, Transform modelTransform)
        {
            var modelLocator = prefab.GetComponent<ModelLocator>();
            modelLocator.modelTransform = modelTransform;
            modelLocator.modelBaseTransform = modelBaseTransform;
        }

        private static void SetupRigidbody(GameObject prefab)
        {
            var rigidbody = prefab.GetComponent<Rigidbody>();
            rigidbody.mass = 100f;
        }

        private static void SetupCapsuleCollider(GameObject prefab)
        {
            var capsuleCollider = prefab.GetComponent<CapsuleCollider>();
            capsuleCollider.center = new Vector3(0f, 0f, 0f);
            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.direction = 1;
        }

        private static void SetupMainHurtbox(GameObject prefab, GameObject model)
        {
            var hurtBoxGroup = model.AddComponent<HurtBoxGroup>();
            var childLocator = model.GetComponent<ChildLocator>();

            if (!childLocator.FindChild("MainHurtbox"))
            {
                Log.Error("Could not set up main hurtbox: make sure you have a transform pair in your prefab's ChildLocator component called 'MainHurtbox'");
                return;
            }

            var mainHurtbox = childLocator.FindChild("MainHurtbox").gameObject.AddComponent<HurtBox>();
            mainHurtbox.gameObject.layer = LayerIndex.entityPrecise.intVal;
            mainHurtbox.healthComponent = prefab.GetComponent<HealthComponent>();
            mainHurtbox.isBullseye = true;
            mainHurtbox.damageModifier = HurtBox.DamageModifier.Normal;
            mainHurtbox.hurtBoxGroup = hurtBoxGroup;
            mainHurtbox.indexInGroup = 0;
            mainHurtbox.isSniperTarget = true;

            hurtBoxGroup.hurtBoxes = new HurtBox[]
            {
                mainHurtbox
            };

            hurtBoxGroup.mainHurtBox = mainHurtbox;
            hurtBoxGroup.bullseyeCount = 1;
        }

        private static void SetupFootstepController(GameObject model)
        {
            var footstepHandler = model.AddComponent<FootstepHandler>();
            footstepHandler.baseFootstepString = "Play_player_footstep";
            footstepHandler.sprintFootstepOverrideString = "";
            footstepHandler.enableFootstepDust = true;
            footstepHandler.footstepDustPrefab = Resources.Load<GameObject>("Prefabs/GenericFootstepDust");
        }

        private static void SetupRagdoll(GameObject model)
        {
            var ragdollController = model.GetComponent<RagdollController>();

            if (!ragdollController) return;

            if (ragdollMaterial == null) ragdollMaterial = Resources.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponentInChildren<RagdollController>().bones[1].GetComponent<Collider>().material;

            foreach (var i in ragdollController.bones)
            {
                if (i)
                {
                    i.gameObject.layer = LayerIndex.ragdoll.intVal;
                    var j = i.GetComponent<Collider>();
                    if (j)
                    {
                        j.material = ragdollMaterial;
                        j.sharedMaterial = ragdollMaterial;
                    }
                }
            }
        }

        private static void SetupAimAnimator(GameObject prefab, GameObject model)
        {
            var aimAnimator = model.AddComponent<AimAnimator>();
            aimAnimator.directionComponent = prefab.GetComponent<CharacterDirection>();
            aimAnimator.pitchRangeMax = 45f;
            aimAnimator.pitchRangeMin = -45f;
            aimAnimator.yawRangeMin = -60f;
            aimAnimator.yawRangeMax = 60f;
            aimAnimator.pitchGiveupRange = 30f;
            aimAnimator.yawGiveupRange = 10f;
            aimAnimator.giveupDuration = 3f;
            aimAnimator.inputBank = prefab.GetComponent<InputBankTest>();
        }

        internal static void SetupHitbox(GameObject prefab, Transform[] hitboxTransforms, string hitboxName)
        {
            var hitBoxGroup = prefab.AddComponent<HitBoxGroup>();

            var hitboxes = new List<HitBox>();

            foreach (var i in hitboxTransforms)
            {
                var hitBox = i.gameObject.AddComponent<HitBox>();
                i.gameObject.layer = LayerIndex.projectile.intVal;
                hitboxes.Add(hitBox);
            }

            hitBoxGroup.hitBoxes = [.. hitboxes];

            hitBoxGroup.groupName = hitboxName;
        }
        #endregion
    }
}

// for simplifying characterbody creation
internal class BodyInfo
{
    internal string bodyName = "";
    internal string bodyNameToken = "";
    internal string subtitleNameToken = "";
    internal Color bodyColor = Color.white;

    internal Texture characterPortrait = null;

    internal GameObject crosshair = null;
    internal GameObject podPrefab = null;

    internal float maxHealth = 100f;
    internal float healthGrowth = 2f;

    internal float healthRegen = 0f;

    internal float shield = 0f;// base shield is a thing apparently. neat
    internal float shieldGrowth = 0f;

    internal float moveSpeed = 7f;
    internal float moveSpeedGrowth = 0f;

    internal float acceleration = 80f;

    internal float jumpPower = 15f;
    internal float jumpPowerGrowth = 0f;// jump power per level exists for some reason

    internal float damage = 12f;

    internal float attackSpeed = 1f;
    internal float attackSpeedGrowth = 0f;

    internal float armor = 0f;
    internal float armorGrowth = 0f;

    internal float crit = 1f;
    internal float critGrowth = 0f;

    internal int jumpCount = 1;
}

// for simplifying rendererinfo creation
internal class CustomRendererInfo
{
    internal string childName;
    internal Material material;
    internal bool ignoreOverlays;
}