﻿using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RobDriver.Modules.Survivors;
using RobDriver.SkillStates.Driver;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace RobDriver.Modules.Components
{
    public class DriverController : MonoBehaviour
    {
        // properties for when access control matters
        // if you try to circumvent/change these youre probably doing a hack
        public DriverWeaponDef weaponDef { get; private set; }
        public DriverBulletDef currentBulletDef { get; private set; } =  BulletTypes.Default;
        public DriverWeaponDef defaultWeaponDef { get; private set; }
        public bool HasSpecialBullets => this.currentBulletDef.index != BulletTypes.Default.index;
        public DamageType DamageType => this.currentBulletDef.bulletType;
        public DamageAPI.ModdedDamageType ModdedDamageType => this.currentBulletDef.moddedBulletType;

        public float chargeValue;
        //private bool timerStarted;
        private float jamTimer;
        //private EntityStateMachine weaponStateMachine;
        private CharacterBody characterBody;
        private ChildLocator childLocator;
        private CharacterModel characterModel;
        private Animator animator;
        private SkillLocator skillLocator;

        private readonly int maxShellCount = 12;
        private readonly int basePistolAmmo = 20;
        private int currentShell;
        private int currentSlug;
        private GameObject[] shellObjects;
        private GameObject[] slugObjects;

        public Action<DriverController> onWeaponUpdate;

        public float maxWeaponTimer;
        public float weaponTimer;
        public DriverPassive passive;
        private float comboDecay = 1f;
        private SkinnedMeshRenderer weaponRenderer;

        public readonly float upForce = 9f;
        public readonly float backForce = 2.4f;

        // ooooAAAAAUGHHHHHGAHEM,67TKM
        private SkillDef[] primarySkillOverrides;
        private SkillDef[] secondarySkillOverrides;

        public GameObject crosshairPrefab;

        private int availableSupplyDrops;
        private int lysateCellCount = 0;

        private GameObject muzzleTrail;

        public ParticleSystem machineGunVFX;

        private bool hasPickedUpHammer;
        private GameObject hammerEffectInstance;
        private GameObject hammerEffectInstance2;

        private DriverWeaponDef lastWeaponDef;
        private WeaponNotificationQueue notificationQueue;
        private bool needReload = false;

        private DriverWeaponTracker weaponTracker
        {
            get
            {
                if (this.characterBody && this.characterBody.master)
                {
                    DriverWeaponTracker i = this.characterBody.master.GetComponent<DriverWeaponTracker>();
                    if (!i) i = this.characterBody.master.gameObject.AddComponent<DriverWeaponTracker>();
                    return i;
                }
                return null;
            }
        }

        private void Awake()
        {
            // this was originally used for gun jamming
            /*foreach (EntityStateMachine i in this.GetComponents<EntityStateMachine>())
            {
                if (i && i.customName == "Weapon") this.weaponStateMachine = i;
            }*/
            // probably won't be used but who knows

            this.passive = this.GetComponent<DriverPassive>();
            this.characterBody = this.GetComponent<CharacterBody>();
            ModelLocator modelLocator = this.GetComponent<ModelLocator>();
            this.childLocator = modelLocator.modelBaseTransform.GetComponentInChildren<ChildLocator>();
            this.animator = modelLocator.modelBaseTransform.GetComponentInChildren<Animator>();
            this.characterModel = modelLocator.modelBaseTransform.GetComponentInChildren<CharacterModel>();
            this.skillLocator = this.GetComponent<SkillLocator>();
            this.machineGunVFX = this.childLocator.FindChild("MachineGunVFX").gameObject.GetComponent<ParticleSystem>();

            // really gotta cache this instead of calling a getcomponent on every single weapon pickup
            this.weaponRenderer = this.childLocator.FindChild("PistolModel").GetComponent<SkinnedMeshRenderer>();

            this.GetSkillOverrides();

            this.defaultWeaponDef = DriverWeaponCatalog.Pistol;
            this.currentBulletDef = BulletTypes.Default;
            this.PickUpWeapon(this.defaultWeaponDef);

            this.availableSupplyDrops = 1;

            this.CreateHammerEffect();

            this.Invoke("SetInventoryHook", 0.5f);
            this.Invoke("CheckForUpgrade", 2.5f);
        }

        private void GetSkillOverrides()
        {
            // get each skilldef from each weapondef in the catalog...... i hate you
            List<SkillDef> primary = new List<SkillDef>();
            List<SkillDef> secondary = new List<SkillDef>();

            for (int i = 0; i < DriverWeaponCatalog.weaponDefs.Length; i++)
            {
                if (DriverWeaponCatalog.weaponDefs[i])
                {
                    if (DriverWeaponCatalog.weaponDefs[i].primarySkillDef) primary.Add(DriverWeaponCatalog.weaponDefs[i].primarySkillDef);
                    if (DriverWeaponCatalog.weaponDefs[i].secondarySkillDef) secondary.Add(DriverWeaponCatalog.weaponDefs[i].secondarySkillDef);
                }
            }

            this.primarySkillOverrides = primary.ToArray();
            this.secondarySkillOverrides = secondary.ToArray();
        }

        private void Start()
        {
            this.InitShells();
            this.SetBulletAmmo();
        }

        private void SetInventoryHook()
        {
            // swag
            if (this.skillLocator.utility.skillDef == Driver.skateboardSkillDef) this.childLocator.FindChild("SkateboardBackModel").gameObject.SetActive(true);

            if (this.characterBody && this.characterBody.master && this.characterBody.master.inventory)
            {
                this.characterBody.master.inventory.onItemAddedClient += this.Inventory_onItemAddedClient;
                this.characterBody.master.inventory.onInventoryChanged += this.Inventory_onInventoryChanged;
            }

            this.defaultWeaponDef = DriverWeaponCatalog.GetWeaponFromIndex(Config.defaultWeaponIndex.Value);

            PickUpWeapon(defaultWeaponDef);

            // upgrade shit
            this.CheckForLysateCell();

            this.CheckForNeedler();

            this.CheckForStoredWeapon();
        }

        private void CheckForStoredWeapon()
        {
            if (NetworkServer.active)
            {
                DriverWeaponTracker weaponTracker = this.weaponTracker;
                if (weaponTracker?.isStoringWeapon == true)
                {
                    DriverWeaponTracker.StoredWeapon storedWeapon = weaponTracker.RetrieveWeapon();
                    this.ServerGetStoredWeapon(storedWeapon.weaponDef, storedWeapon.ammo, storedWeapon.ammoIndex);
                }
            }
        }

        private void StoreWeapon()
        {
            this.weaponTracker?.StoreWeapon(this.weaponDef, this.weaponTimer, (short)this.currentBulletDef.index);
        }

        private void CheckForLysateCell()
        {
            if (this.characterBody && this.characterBody.master && this.characterBody.master.inventory)
            {
                int count = this.characterBody.master.inventory.GetItemCount(DLC1Content.Items.EquipmentMagazineVoid);
                if (count > this.lysateCellCount)
                {
                    int diff = count - this.lysateCellCount;
                    this.availableSupplyDrops += diff;
                    this.lysateCellCount = count;
                }
            }
        }

        private void CheckForNeedler()
        {
            if (this.hasPickedUpHammer) return;

            if (this.characterBody && this.characterBody.master && this.characterBody.master.inventory)
            {
                DriverWeaponDef desiredWeapon = this.defaultWeaponDef;

                if (this.characterBody.master.inventory.GetItemCount(RoR2Content.Items.TitanGoldDuringTP) > 0 &&
                    this.defaultWeaponDef == DriverWeaponCatalog.Pistol)
                {
                    desiredWeapon = DriverWeaponCatalog.PyriteGun;
                }

                if (this.characterBody.master.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement) > 0)
                {
                    desiredWeapon = DriverWeaponCatalog.Needler;
                }

                // set new default
                this.defaultWeaponDef = desiredWeapon;
                // give new weapon if you arent holding a stored weapon
                if (this.weaponDef != this.defaultWeaponDef && desiredWeapon != this.defaultWeaponDef)
                {
                    this.PickUpWeapon(this.defaultWeaponDef);
                }
            }
        }

        private void Inventory_onInventoryChanged()
        {
            this.CheckForNeedler();
            this.CheckForLysateCell();
        }

        private void CheckForUpgrade()
        {
            if (!Modules.Config.enablePistolUpgrade.Value) return;

            if (this.hasPickedUpHammer) return;

            // upgrade your pistol for run-ending bosses; this is more interesting than just injecting weapon drops imo
            Scene currentScene = SceneManager.GetActiveScene();

            if (currentScene.name == "moon" || currentScene.name == "moon2")
            {
                this.UpgradeToLunar();
            }

            if (currentScene.name == "voidraid")
            {
                this.UpgradeToVoid();
            }

            if (currentScene.name == "limbo")
            {
                this.UpgradeToLunar();
            }
        }

        private bool TryUpgradeWeapon(DriverWeaponDef newWeaponDef)
        {
            if (!DriverWeaponCatalog.IsWeaponPistol(defaultWeaponDef)) return false;
            if (this.characterBody && this.characterBody.inventory && this.characterBody.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement) > 0) return false;

            this.defaultWeaponDef = newWeaponDef;

            return true;
        }

        private void UpgradeToLunar()
        {
            bool success = this.TryUpgradeWeapon(DriverWeaponCatalog.LunarPistol);
            if (!success) return;

            if (weaponDef == defaultWeaponDef)
            {
                this.PickUpWeapon(this.defaultWeaponDef);
                this.TryPickupNotification(true);
            }

            EffectData effectData = new EffectData
            {
                origin = this.childLocator.FindChild("PistolMuzzle").position,
                rotation = Quaternion.identity
            };
            EffectManager.SpawnEffect(Modules.Assets.upgradeEffectPrefab, effectData, false);

            EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarGolem/LunarGolemTwinShotExplosion.prefab").WaitForCompletion(),
                new EffectData
                {
                    origin = this.childLocator.FindChild("Pistol").position,
                    rotation = Quaternion.identity,
                    scale = 1f
                }, false);
        }

        private void UpgradeToVoid()
        {
            bool success = this.TryUpgradeWeapon(DriverWeaponCatalog.VoidPistol);
            if (!success) return;

            if (weaponDef == defaultWeaponDef)
            {
                this.PickUpWeapon(this.defaultWeaponDef);
                this.TryPickupNotification(true);
            }

            EffectData effectData = new EffectData
            {
                origin = this.childLocator.FindChild("PistolMuzzle").position,
                rotation = Quaternion.identity
            };
            EffectManager.SpawnEffect(Modules.Assets.upgradeEffectPrefab, effectData, false);

            EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidSurvivor/VoidSurvivorMegaBlasterExplosion.prefab").WaitForCompletion(),
                new EffectData
                {
                    origin = this.childLocator.FindChild("Pistol").position,
                    rotation = Quaternion.identity,
                    scale = 1f
                }, false);
        }

        private void Inventory_onItemAddedClient(ItemIndex itemIndex)
        {
            // quit resetting my shit
            if (this.passive.isBullets || this.passive.isPistolOnly) return;

            if (DriverPlugin.litInstalled && this.IsItemGoldenGun(itemIndex)) // funny compat :-)
            {
                this.ServerPickUpWeapon(DriverWeaponCatalog.GoldenGun);
            }

            if (DriverPlugin.classicItemsInstalled && this.IsItemGoldenGun2(itemIndex)) // not funny anymore
            {
                this.ServerPickUpWeapon(DriverWeaponCatalog.GoldenGun);
            }

            if (itemIndex == RoR2Content.Items.Behemoth.itemIndex)
            {
                this.ServerPickUpWeapon(DriverWeaponCatalog.Behemoth);
            }
        }

        private bool IsItemGoldenGun(ItemIndex itemIndex)
        {
            var goldenGun = LostInTransit.LITContent.Items.GoldenGun;

            if (goldenGun is null) return false;
            return goldenGun.itemIndex == itemIndex;
        }

        // electric boogaloo
        private bool IsItemGoldenGun2(ItemIndex itemIndex)
        {
            var goldenGun = ClassicItemsReturns.Items.GoldenGun.Instance;

            if (goldenGun?.ItemDef is null) return false;
            return goldenGun.ItemDef.itemIndex == itemIndex;
        }

        private void CreateHammerEffect()
        {
            #region clone mithrix effect
            this.hammerEffectInstance = GameObject.Instantiate(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/BrotherBody").GetComponentInChildren<ChildLocator>().FindChild("Phase3HammerFX").gameObject);
            this.hammerEffectInstance.transform.parent = this.childLocator.FindChild("GunR");
            this.hammerEffectInstance.transform.localScale = Vector3.one * 0.0002f;
            this.hammerEffectInstance.transform.rotation = Quaternion.Euler(new Vector3(0f, 90f, 90f));
            this.hammerEffectInstance.transform.localPosition = new Vector3(0f, 1.6f, 0.05f);
            this.hammerEffectInstance.gameObject.SetActive(true);

            this.hammerEffectInstance.transform.Find("Amb_Fire_Ps, Left").localScale = Vector3.one * 0.6f;
            this.hammerEffectInstance.transform.Find("Amb_Fire_Ps, Right").localScale = Vector3.one * 0.6f;
            this.hammerEffectInstance.transform.Find("Core, Light").localScale = Vector3.one * 0.1f;
            this.hammerEffectInstance.transform.Find("Blocks, Spinny").localScale = Vector3.one * 0.4f;
            this.hammerEffectInstance.transform.Find("Sparks").localScale = Vector3.one * 0.4f;
            #endregion

            this.hammerEffectInstance2 = GameObject.Instantiate(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/LunarWispBody").GetComponentInChildren<CharacterModel>().transform.Find("Amb_Fire_Ps").gameObject);
            this.hammerEffectInstance2.transform.parent = this.childLocator.FindChild("HandL");
            this.hammerEffectInstance2.transform.localPosition = Vector3.zero;
            this.hammerEffectInstance2.transform.localRotation = Quaternion.identity;
            this.hammerEffectInstance2.transform.localScale *= 0.25f;

            this.hammerEffectInstance.SetActive(false);
            this.hammerEffectInstance2.SetActive(false);
        }

        public void ConsumeAmmo(float multiplier = 1f, bool scaleWithAttackSpeed = true)
        {
            if (this.characterBody && this.characterBody.HasBuff(RoR2Content.Buffs.NoCooldowns)) return;

            if (this.characterBody && this.characterBody.inventory && scaleWithAttackSpeed)
            {
                int alienHeadCount = this.characterBody.inventory.GetItemCount(RoR2Content.Items.AlienHead);
                if (alienHeadCount > 0)
                {
                    for (int i = 0; i < alienHeadCount; i++)
                    {
                        if (DriverPlugin.greenAlienHeadInstalled)
                        {
                            multiplier *= 0.85f;
                        }
                        else
                        {
                            multiplier *= 0.75f;
                        }
                    }
                }
            }

            if (scaleWithAttackSpeed) this.weaponTimer -= multiplier / this.characterBody.attackSpeed;
            else this.weaponTimer -= multiplier;

            // notify Hud
            this.onWeaponUpdate?.Invoke(this);
        }

        private void FixedUpdate()
        {
            this.jamTimer = Mathf.Clamp(this.jamTimer - (2f * Time.fixedDeltaTime), 0f, Mathf.Infinity);

            if (this.weaponTimer <= 0f && this.maxWeaponTimer > 0f)
            {
                if (this.passive.isBullets || this.passive.isRyan)
                {
                    if (this.HasSpecialBullets)
                    {
                        currentBulletDef = BulletTypes.Default;

                        UnityEngine.Object.Destroy(muzzleTrail.gameObject);
                        this.muzzleTrail = null;
                    }
                }

                if (weaponDef == defaultWeaponDef)
                {
                    if (!needReload)
                    {
                        this.needReload = true;
                        this.skillLocator.primary.SetSkillOverride(this, Driver.pistolReloadSkillDef, GenericSkill.SkillOverridePriority.Contextual);
                    }
                }
                else
                {
                    this.ReturnToDefaultWeapon();
                }
            }
            this.CheckSupplyDrop();
        }

        private void CheckSupplyDrop()
        {
            if (this.skillLocator)
            {
                if (this.skillLocator.special.baseSkill.skillNameToken == DriverPlugin.developerPrefix + "_DRIVER_BODY_SPECIAL_SUPPLY_DROP_LEGACY_NAME")
                {
                    if (this.characterBody && this.characterBody.master && this.characterBody.master.inventory)
                    {
                        if (this.characterBody.master.inventory.GetItemCount(RoR2Content.Items.LunarSpecialReplacement) > 0)
                        {
                            return;
                        }
                    }

                    this.skillLocator.special.stock = this.availableSupplyDrops;
                }
            }
        }

        public void ConsumeSupplyDrop()
        {
            this.availableSupplyDrops--;
        }

        public void ServerResetTimer()
        {
            // just pick up the same weapon again cuz i don't feel like writing even more netcode to sync this
            this.ServerPickUpWeapon(this.weaponDef);
        }

        /// <summary>
        /// Dont call this, gets stored weapon from DriverWeaponController
        /// </summary>
        public void ServerGetStoredWeapon(DriverWeaponDef newWeapon, float ammo, int ammoIndex)
        {
            NetworkIdentity identity = this.gameObject.GetComponent<NetworkIdentity>();
            if (!identity) return;

            new SyncStoredWeapon(identity.netId, newWeapon.index, ammo, (short)ammoIndex).Send(NetworkDestination.Clients);
        }

        /// <summary>
        /// Server telling you to pick up a new weapon
        /// </summary>
        public void ServerPickUpWeapon(DriverWeaponDef newWeapon)
        {
            ServerPickUpWeapon(this, newWeapon, false, this.currentBulletDef.index, false);
        }

        /// <summary>
        /// Uhhh network shit idk, dont call this, it just works
        /// </summary>
        public void ServerPickUpWeapon(DriverController driverController, DriverWeaponDef newWeapon, bool cutAmmo, int ammoIndex, bool isNewAmmoType)
        {
            NetworkIdentity identity = driverController.gameObject.GetComponent<NetworkIdentity>();
            if (!identity) return;

            new SyncWeapon(identity.netId, newWeapon.index, cutAmmo, ammoIndex, isNewAmmoType).Send(NetworkDestination.Clients);
        }

        private void ReturnToDefaultWeapon()
        {
            this.DiscardWeapon();
            if (this.hasPickedUpHammer)
            {
                this.PickUpWeapon(DriverWeaponCatalog.LunarHammer);
            }
            else
            {
                this.PickUpWeapon(this.defaultWeaponDef);
            }
        }

        private void DiscardWeapon()
        {
            // just create the effect here
            GameObject newEffect = GameObject.Instantiate(Modules.Assets.discardedWeaponEffect);
            newEffect.GetComponent<DiscardedWeaponComponent>().Init(this.weaponDef, (this.characterBody.characterDirection.forward * -this.backForce) + (Vector3.up * this.upForce) + this.characterBody.characterMotor.velocity);
            newEffect.transform.rotation = this.characterBody.modelLocator.modelTransform.rotation;
            newEffect.transform.position = this.childLocator.FindChild("Pistol").position + (Vector3.up * 0.5f);
        }
        
        public void FinishReload()
        {
            if (needReload) this.skillLocator.primary.UnsetSkillOverride(this, Driver.pistolReloadSkillDef, GenericSkill.SkillOverridePriority.Contextual);
            needReload = false;

            SetBulletAmmo();

            // notify hud
            this.onWeaponUpdate?.Invoke(this);
        }

        /// <summary>
        /// Decides what to do for dropped weapons with each passive
        /// </summary>
        public void PickUpWeaponDrop(DriverWeaponDef newWeapon, float ammo, int ammoIndex, bool isNewAmmoType, bool cutAmmo)
        {
            if (this.passive.isPistolOnly)
            {
                this.FinishReload();
            }
            else if (this.passive.isBullets)
            {
                LoadNewBullets(ammoIndex, cutAmmo, ammo);
            }
            else if (this.passive.isRyan)
            {
                // change ammo type
                if (isNewAmmoType) LoadNewBullets(ammoIndex, cutAmmo, ammo);
                // picked up new weapon
                else PickUpWeapon(newWeapon, cutAmmo, ammo);
            }
            else
            {
                PickUpWeapon(newWeapon, cutAmmo, ammo);
            }
        }

        /// <summary>
        /// Changes weapon, does not change ammo type
        /// </summary>
        private void PickUpWeapon(DriverWeaponDef newWeapon, bool cutAmmo = false, float ammo = -1f)
        {
            this.weaponDef = newWeapon;

            if (newWeapon == DriverWeaponCatalog.LunarHammer) this.hasPickedUpHammer = true; // hardcoding the mithrix hammer as default once picked up. fuck it

            this.EquipWeapon();
            this.SetBulletAmmo(ammo, cutAmmo);

            this.TryCallout();
            this.TryPickupNotification();

            this.onWeaponUpdate?.Invoke(this);
        }

        /// <summary>
        /// Changes ammo type, does not change weapon
        /// </summary>
        private void LoadNewBullets(int newAmmoIndex, bool cutAmmo, float ammo)
        {
            if (this.needReload) this.skillLocator.primary.UnsetSkillOverride(this, Driver.pistolReloadSkillDef, GenericSkill.SkillOverridePriority.Contextual);
            needReload = false;

            //Just in case
            if (muzzleTrail)
            {
                UnityEngine.Object.Destroy(muzzleTrail);
                muzzleTrail = null;
            }

            currentBulletDef = BulletTypes.GetBulletDefFromIndex(newAmmoIndex);

            SetBulletAmmo(ammo, cutAmmo);

            // this shit doesnt work
            Transform muzzleTransform;
            if (DriverWeaponCatalog.IsWeaponPistol(weaponDef)) muzzleTransform = this.childLocator.FindChild("PistolMuzzle");
            else muzzleTransform = this.childLocator.FindChild("ShotgunMuzzle");

            muzzleTrail = Assets.defaultMuzzleTrail;
            muzzleTrail.GetComponent<TrailRenderer>().startColor = currentBulletDef.trailColor;
            muzzleTrail = UnityEngine.Object.Instantiate(muzzleTrail, muzzleTransform);

            // notify hud
            this.onWeaponUpdate?.Invoke(this);
        }

        /// <summary>
        /// Resets ammo for current weapon
        /// </summary>
        private void SetBulletAmmo(float ammo = -1, bool cutAmmo = false)
        {
            float shotCount;
            // set ammo for non-pistols
            if (!DriverWeaponCatalog.IsWeaponPistol(weaponDef))
            {
                shotCount = this.weaponDef.shotCount;

                if (Modules.Config.GetWeaponConfig(this.weaponDef)) shotCount = Modules.Config.GetWeaponConfigShotCount(this.weaponDef);
            }
            // set ammo for pistols
            else if (this.HasSpecialBullets)
            {
                shotCount = basePistolAmmo;
            }
            // finite ammo
            else if (this.passive.isPistolOnly)
            {
                this.weaponTimer = 26f;
                this.maxWeaponTimer = 26f;
                return;
            }
            // infinite ammo
            else
            {
                this.weaponTimer = 0f;
                this.maxWeaponTimer = 0f;
                return;
            }

            if (Modules.Config.backupMagExtendDuration.Value)
            {
                if (this.characterBody && this.characterBody.inventory)
                {
                    shotCount += (0.5f * this.characterBody.inventory.GetItemCount(RoR2Content.Items.SecondarySkillMagazine));
                }
            }

            // supply drop
            if (cutAmmo) weaponTimer = shotCount * 0.5f;
            // persisted ammo count
            else weaponTimer = ammo == -1 ? shotCount : ammo;
            this.maxWeaponTimer = shotCount;

            //notify hud
            this.onWeaponUpdate?.Invoke(this);
        }

        private void TryPickupNotification(bool force = false)
        {
            if (!Modules.Config.enablePickupNotifications.Value) return;

            // attempt to add the component if it's not there
            if (!this.notificationQueue && this.characterBody.master)
            {
                this.notificationQueue = this.characterBody.master.GetComponent<WeaponNotificationQueue>();
               // if (!this.notificationQueue) this.notificationQueue = this.characterBody.master.gameObject.AddComponent<WeaponNotificationQueue>();
            }

            if (this.notificationQueue)
            {
                if (force)
                {
                    WeaponNotificationQueue.PushWeaponNotification(this.characterBody.master, this.weaponDef.index);
                    this.lastWeaponDef = this.weaponDef;
                    return;
                }

                if (this.weaponDef != this.lastWeaponDef)
                {
                    if (this.weaponDef != this.defaultWeaponDef && this.weaponDef != DriverWeaponCatalog.Pistol)
                    {
                        WeaponNotificationQueue.PushWeaponNotification(this.characterBody.master, this.weaponDef.index);
                    }
                }
                this.lastWeaponDef = this.weaponDef;
            }
        }

        private void TryCallout()
        {
            if (this.weaponDef && this.weaponDef.calloutSoundString != "")
            {
                if (Modules.Config.weaponCallouts.Value)
                {
                    Util.PlaySound(this.weaponDef.calloutSoundString, this.gameObject);
                }
            }
        }

        private void EquipWeapon()
        {
            // unset all the overrides....
            for (int i = 0; i < this.primarySkillOverrides.Length; i++)
            {
                if (this.primarySkillOverrides[i])
                {
                    this.skillLocator.primary.UnsetSkillOverride(this.skillLocator.primary, this.primarySkillOverrides[i], GenericSkill.SkillOverridePriority.Contextual);
                }
            }

            for (int i = 0; i < this.secondarySkillOverrides.Length; i++)
            {
                if (this.secondarySkillOverrides[i])
                {
                    this.skillLocator.secondary.UnsetSkillOverride(this.skillLocator.secondary, this.secondarySkillOverrides[i], GenericSkill.SkillOverridePriority.Contextual);
                }
            }
            // fuck this, seriously

            // set new overrides
            if (this.weaponDef.primarySkillDef) this.skillLocator.primary.SetSkillOverride(this.skillLocator.primary, this.weaponDef.primarySkillDef, GenericSkill.SkillOverridePriority.Contextual);
            if (this.weaponDef.secondarySkillDef) this.skillLocator.secondary.SetSkillOverride(this.skillLocator.secondary, this.weaponDef.secondarySkillDef, GenericSkill.SkillOverridePriority.Contextual);

            // model swap
            if (this.weaponDef.mesh)
            {
                this.weaponRenderer.sharedMesh = this.weaponDef.mesh;
                this.characterModel.baseRendererInfos[this.characterModel.baseRendererInfos.Length - 1].defaultMaterial = this.weaponDef.material;
            }
            else
            {
                if (this.weaponDef.animationSet == DriverWeaponDef.AnimationSet.TwoHanded)
                {
                    this.weaponRenderer.sharedMesh = DriverWeaponCatalog.Behemoth.mesh;
                    this.characterModel.baseRendererInfos[this.characterModel.baseRendererInfos.Length - 1].defaultMaterial = DriverWeaponCatalog.Behemoth.material;
                }
                else
                {
                    this.weaponRenderer.sharedMesh = DriverWeaponCatalog.Pistol.mesh;
                    this.characterModel.baseRendererInfos[this.characterModel.baseRendererInfos.Length - 1].defaultMaterial = DriverWeaponCatalog.Pistol.material;
                }
            }

            // crosshair
            this.crosshairPrefab = this.weaponDef.crosshairPrefab;
            this.characterBody._defaultCrosshairPrefab = this.crosshairPrefab;

            // animator layer
            switch (this.weaponDef.animationSet)
            {
                case DriverWeaponDef.AnimationSet.Default:
                    this.EnableLayer("");
                    break;
                case DriverWeaponDef.AnimationSet.TwoHanded:
                    this.EnableLayer("Body, Shotgun");
                    break;
                case DriverWeaponDef.AnimationSet.BigMelee:
                    this.EnableLayer("Body, Hammer");
                    break;
            }

            // extra shit
            if (this.hammerEffectInstance && this.hammerEffectInstance2)
            {
                if (this.weaponDef == DriverWeaponCatalog.LunarHammer)
                {
                    this.hammerEffectInstance.SetActive(true);
                    this.hammerEffectInstance2.SetActive(false); // this one needs to be remade from scratch ig
                }
                else
                {
                    this.hammerEffectInstance.SetActive(false);
                    this.hammerEffectInstance2.SetActive(false);
                }
            }
        }

        private void EnableLayer(string layerName)
        {
            if (!this.animator) return;

            this.animator.SetLayerWeight(this.animator.GetLayerIndex("Body, Shotgun"), 0f);
            this.animator.SetLayerWeight(this.animator.GetLayerIndex("Body, Hammer"), 0f);

            if (layerName == "") return;

            this.animator.SetLayerWeight(this.animator.GetLayerIndex(layerName), 1f);
        }

        public bool AddJamBuildup(bool jammed = false)
        {
            this.jamTimer += 3f;

            if (this.jamTimer >= 10f)
            {
                this.jamTimer = 0f;
                jammed = true;
            }

            return jammed;
        }

        private void InitShells()
        {
            this.currentShell = 0;

            this.shellObjects = new GameObject[this.maxShellCount + 1];

            GameObject desiredShell = Assets.shotgunShell;

            for (int i = 0; i < this.maxShellCount; i++)
            {
                this.shellObjects[i] = GameObject.Instantiate(desiredShell, this.childLocator.FindChild("Pistol"), false);
                this.shellObjects[i].transform.localScale = Vector3.one * 1.1f;
                this.shellObjects[i].SetActive(false);
                this.shellObjects[i].GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;

                this.shellObjects[i].layer = LayerIndex.ragdoll.intVal;
                this.shellObjects[i].transform.GetChild(0).gameObject.layer = LayerIndex.ragdoll.intVal;
            }

            this.currentSlug = 0;

            this.slugObjects = new GameObject[this.maxShellCount + 1];

            desiredShell = Assets.shotgunSlug;

            for (int i = 0; i < this.maxShellCount; i++)
            {
                this.slugObjects[i] = GameObject.Instantiate(desiredShell, this.childLocator.FindChild("Pistol"), false);
                this.slugObjects[i].transform.localScale = Vector3.one * 1.2f;
                this.slugObjects[i].SetActive(false);
                this.slugObjects[i].GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;

                this.slugObjects[i].layer = LayerIndex.ragdoll.intVal;
                this.slugObjects[i].transform.GetChild(0).gameObject.layer = LayerIndex.ragdoll.intVal;
            }
        }

        public void DropShell(Vector3 force)
        {
            if (this.shellObjects == null) return;

            if (this.shellObjects[this.currentShell] == null) return;

            Transform origin = this.childLocator.FindChild("Pistol");

            this.shellObjects[this.currentShell].SetActive(false);

            this.shellObjects[this.currentShell].transform.position = origin.position;
            this.shellObjects[this.currentShell].transform.SetParent(null);

            this.shellObjects[this.currentShell].SetActive(true);

            Rigidbody rb = this.shellObjects[this.currentShell].gameObject.GetComponent<Rigidbody>();
            if (rb) rb.velocity = force;

            this.currentShell++;
            if (this.currentShell >= this.maxShellCount) this.currentShell = 0;
        }

        public void DropSlug(Vector3 force)
        {
            if (this.slugObjects == null) return;

            if (this.slugObjects[this.currentSlug] == null) return;

            Transform origin = this.childLocator.FindChild("Pistol");

            this.slugObjects[this.currentSlug].SetActive(false);

            this.slugObjects[this.currentSlug].transform.position = origin.position;
            this.slugObjects[this.currentSlug].transform.SetParent(null);

            this.slugObjects[this.currentSlug].SetActive(true);

            Rigidbody rb = this.slugObjects[this.currentSlug].gameObject.GetComponent<Rigidbody>();
            if (rb) rb.velocity = force;

            this.currentSlug++;
            if (this.currentSlug >= this.maxShellCount) this.currentSlug = 0;
        }

        private void OnDestroy()
        {
            if (this.shellObjects != null && this.shellObjects.Length > 0)
            {
                for (int i = 0; i < this.shellObjects.Length; i++)
                {
                    if (this.shellObjects[i]) Destroy(this.shellObjects[i]);
                }
            }

            if (this.slugObjects != null && this.slugObjects.Length > 0)
            {
                for (int i = 0; i < this.slugObjects.Length; i++)
                {
                    if (this.slugObjects[i]) Destroy(this.slugObjects[i]);
                }
            }

            if (this.characterBody && this.characterBody.master && this.characterBody.master.inventory)
            {
                this.characterBody.master.inventory.onItemAddedClient -= this.Inventory_onItemAddedClient;
                this.characterBody.master.inventory.onInventoryChanged -= this.Inventory_onInventoryChanged;
            }

            if (NetworkServer.active)
            {
                this.StoreWeapon();
            }
        }

        public void ExtendTimer()
        {
            return;
            // fuck, i have to network this before adding it actually
            if (this.weaponTimer > 0f && this.maxWeaponTimer > 0f)
            {
                float amount = 1f * this.comboDecay;

                this.comboDecay = Mathf.Clamp(this.comboDecay - 0.1f, 0f, 1f);

                this.weaponTimer = Mathf.Clamp(this.weaponTimer + amount, 0f, this.maxWeaponTimer);
            }
        }
    }
}
