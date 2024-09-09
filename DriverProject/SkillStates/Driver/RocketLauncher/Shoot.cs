﻿using RoR2;
using UnityEngine;
using EntityStates;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;

namespace RobDriver.SkillStates.Driver.RocketLauncher
{
    public class Shoot : BaseDriverSkillState
    {
        public static float damageCoefficient = 5f;
        public static float procCoefficient = 1f;
        public float baseDuration = 1.3f; // the base skill duration. i.e. attack speed
        public static float recoil = 16f;

        private float earlyExitTime;
        protected float duration;
        protected float fireDuration;
        protected bool hasFired;
        private bool isCrit;
        protected string muzzleString;

        protected virtual string soundString => this.isCrit ? "sfx_driver_rocket_launcher_shoot" : "sfx_driver_rocket_launcher_shoot";
        protected virtual GameObject projectilePrefab => Modules.Projectiles.rocketProjectilePrefab;
        protected virtual float ammoMod => 1.5f;
        protected virtual float _damageCoefficient => Shoot.damageCoefficient;

        public override void OnEnter()
        {
            base.OnEnter();
            this.characterBody.SetAimTimer(5f);
            this.muzzleString = "ShotgunMuzzle";
            this.hasFired = false;
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.isCrit = base.RollCrit();
            this.earlyExitTime = 0.4f * this.duration;

            Util.PlaySound(this.soundString, base.gameObject);

            //this.PlayCrossfade("Gesture, Override", "FireShotgun", "Shoot.playbackRate", Mathf.Max(0.05f, 1.75f * duration), 0.06f);
            base.PlayAnimation("Gesture, Override", "FireTwohand", "Shoot.playbackRate", this.duration);
            base.PlayAnimation("AimPitch", "Shoot");

            this.fireDuration = 0;

            if (this.iDrive) this.iDrive.ConsumeAmmo(this.ammoMod);
        }

        public virtual void Fire()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                var recoilAmplitude = Shoot.recoil / this.attackSpeedStat;

                base.AddRecoil2(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
                this.characterBody.AddSpreadBloom(4f);
                EffectManager.SimpleMuzzleFlash(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/MuzzleflashSmokeRing.prefab").WaitForCompletion(), this.gameObject, this.muzzleString, false);

                if (base.isAuthority)
                {
                    var aimRay = this.GetAimRay();

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
                                crit = this.isCrit,
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
                            crit = this.isCrit,
                            damageColorIndex = DamageColorIndex.Default,
                            target = null,
                            speedOverride = 120f,
                            useSpeedOverride = true,
                            damageTypeOverride = iDrive.DamageType
                        });
                    }
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                this.Fire();
            }

            if (this.iDrive && this.iDrive.weaponDef.nameToken != this.cachedWeaponDef.nameToken)
            {
                base.PlayAnimation("Gesture, Override", this.iDrive.weaponDef.equipAnimationString);
                this.outer.SetNextStateToMain();
                return;
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextState(new WaitForReload());
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            this.GetModelAnimator().SetTrigger("endAim");
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (base.fixedAge >= this.earlyExitTime) return InterruptPriority.Any;
            return InterruptPriority.Skill;
        }
    }
}