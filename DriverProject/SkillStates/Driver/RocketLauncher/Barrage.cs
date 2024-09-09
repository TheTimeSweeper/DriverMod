﻿using RoR2;
using UnityEngine;
using EntityStates;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;

namespace RobDriver.SkillStates.Driver.RocketLauncher
{
    public class Barrage : BaseDriverSkillState
    {
        public static float damageCoefficient = 3f;
        public static float procCoefficient = 1f;
        public float baseShotDuration = 0.05f;
        public static float recoil = 11f;

        protected virtual int baseRocketCount => 7;
        protected virtual float maxSpread => 0f;
        protected virtual GameObject projectilePrefab => Modules.Projectiles.missileProjectilePrefab;
        protected virtual float ammoMod => 1.5f;
        protected virtual float _damageCoefficient => Barrage.damageCoefficient;

        private int remainingShots;
        private float shotTimer;
        private float shotDuration;
        protected string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.characterBody.SetAimTimer(5f);
            this.muzzleString = "ShotgunMuzzle";
            this.shotDuration = this.baseShotDuration / this.attackSpeedStat;
            this.remainingShots = Mathf.Clamp(Mathf.RoundToInt(this.baseRocketCount * this.attackSpeedStat), this.baseRocketCount, 40);

            this.shotTimer = this.shotDuration;
            this.remainingShots--;
            this.Fire();
        }

        public virtual void Fire()
        {
            if (this.iDrive) this.iDrive.ConsumeAmmo(this.ammoMod * (2f / this.baseRocketCount));

            base.PlayAnimation("Gesture, Override", "FireBazooka", "Shoot.playbackRate", 1.4f);
            base.PlayAnimation("AimPitch", "Shoot");

            Util.PlaySound("sfx_driver_rocket_launcher_shoot", base.gameObject);

            var recoilAmplitude = Barrage.recoil / this.attackSpeedStat;

            base.AddRecoil2(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
            this.characterBody.AddSpreadBloom(2f);
            EffectManager.SimpleMuzzleFlash(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/MuzzleflashSmokeRing.prefab").WaitForCompletion(), this.gameObject, this.muzzleString, false);

            if (base.isAuthority)
            {
                var aimRay = this.GetAimRay();
                aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, this.maxSpread, 1f, 1f, 0f, 0f);

                var isCrit = base.RollCrit();

                // copied from moff's rocket
                // the fact that this item literally has to be hardcoded into character skillstates makes me so fucking angry you have no idea
                if (this.characterBody.inventory && this.characterBody.inventory.GetItemCount(DLC1Content.Items.MoreMissile) > 0)
                {
                    var damageMult = DriverPlugin.GetICBMDamageMult(this.characterBody);

                    var rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                    var axis = Vector3.Cross(aimRay.direction, rhs);

                    var direction = Quaternion.AngleAxis(-1.5f, axis) * aimRay.direction;
                    var rotation = Quaternion.AngleAxis(1.5f, axis);
                    var aimRay2 = new Ray(aimRay.origin, direction);
                    for (var i = 0; i < 3; i++)
                    {
                        ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                        {
                            projectilePrefab = this.projectilePrefab,
                            position = aimRay2.origin,
                            rotation = Util.QuaternionSafeLookRotation(aimRay2.direction),
                            owner = this.gameObject,
                            damage = damageMult * this.damageStat * this._damageCoefficient,
                            force = 1200f,
                            crit = isCrit,
                            damageColorIndex = DamageColorIndex.Default,
                            target = null,
                            speedOverride = 120f,
                            useSpeedOverride = true,
                            damageTypeOverride = iDrive.DamageType
                        });
                        
                        aimRay2.direction = rotation * aimRay2.direction;
                    }
                }
                else
                {
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = this.projectilePrefab,
                        position = aimRay.origin,
                        rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                        owner = this.gameObject,
                        damage = this.damageStat * this._damageCoefficient,
                        force = 1200f,
                        crit = isCrit,
                        damageColorIndex = DamageColorIndex.Default,
                        target = null,
                        speedOverride = 120f,
                        useSpeedOverride = true,
                        damageTypeOverride = iDrive.DamageType
                    });
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.shotTimer -= Time.fixedDeltaTime;

            if (this.shotTimer <= 0f)
            {
                if (this.remainingShots > 0)
                {
                    this.shotTimer = this.shotDuration;
                    this.remainingShots--;
                    this.Fire();
                }
                else
                {
                    if (base.isAuthority)
                    {
                        this.outer.SetNextStateToMain();
                    }
                }
            }

            if (this.iDrive && this.iDrive.weaponDef.nameToken != this.cachedWeaponDef.nameToken)
            {
                base.PlayAnimation("Gesture, Override", this.iDrive.weaponDef.equipAnimationString);
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            this.GetModelAnimator().SetTrigger("endAim");
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}