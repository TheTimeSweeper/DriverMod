﻿using RoR2;
using UnityEngine;
using EntityStates;
using R2API;

namespace RobDriver.SkillStates.Driver.SniperRifle
{
    public class Shoot : BaseDriverSkillState
    {
        public static float damageCoefficient = 18f;
        public static float procCoefficient = 1f;
        public float baseDuration = 1.6f;
        public static int bulletCount = 1;
        public static float bulletRecoil = 16f;
        public static float bulletRange = 2000f;
        public float selfForce = 0f;
        public bool aiming;

        private float earlyExitTime;
        protected float duration;
        protected float fireDuration;
        protected bool hasFired;
        private bool isCrit;
        protected string muzzleString;

        public override void OnEnter()
        {
            base.OnEnter();
            this.characterBody.SetAimTimer(5f);
            this.muzzleString = "ShotgunMuzzle";
            this.hasFired = false;
            this.duration = this.baseDuration / this.attackSpeedStat;
            this.isCrit = base.RollCrit();
            this.earlyExitTime = 0.75f * this.duration;

            Util.PlaySound("sfx_driver_sniper_shoot", base.gameObject);

            base.PlayAnimation("Gesture, Override", "FireSniper", "Shoot.playbackRate", this.duration);
            base.PlayAnimation("AimPitch", "Shoot");

            this.fireDuration = 0;

            if (this.iDrive) this.iDrive.ConsumeAmmo();
        }

        public virtual void FireBullet()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                if (this.iDrive) this.iDrive.machineGunVFX.Play();

                var recoilAmplitude = Shoot.bulletRecoil / this.attackSpeedStat;

                base.AddRecoil2(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
                this.characterBody.AddSpreadBloom(4f);
                EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FireBarrage.effectPrefab, gameObject, muzzleString, false);

                var tracer = Modules.Assets.shotgunTracer;
                if (this.isCrit) tracer = Modules.Assets.shotgunTracerCrit;

                if (base.isAuthority)
                {
                    var damage = Shoot.damageCoefficient * this.damageStat;

                    var aimRay = GetAimRay();

                    float force = 2500;

                    var maxSpread = 6f;
                    var minSpread = 3f;

                    var radius = 1f;

                    var stopperMask = LayerIndex.CommonMasks.bullet;
                    var damageType = iDrive.DamageType;
                    if (this.aiming)
                    {
                        maxSpread = 0f;
                        minSpread = 0f;
                        stopperMask = LayerIndex.world.mask;
                        damageType = DamageType.Stun1s | iDrive.DamageType;
                        tracer = Modules.Assets.sniperTracer;
                        radius = 0.25f;
                    }

                    var bulletAttack = new BulletAttack
                    {
                        aimVector = aimRay.direction,
                        origin = aimRay.origin,
                        damage = damage,
                        damageColorIndex = DamageColorIndex.Default,
                        damageType = damageType,
                        falloffModel = BulletAttack.FalloffModel.None,
                        maxDistance = bulletRange,
                        force = force,
                        hitMask = LayerIndex.CommonMasks.bullet,
                        isCrit = this.isCrit,
                        owner = gameObject,
                        muzzleName = muzzleString,
                        smartCollision = true,
                        procChainMask = default,
                        procCoefficient = procCoefficient,
                        radius = radius,
                        sniper = false,
                        stopperMask = stopperMask,
                        weapon = null,
                        tracerEffectPrefab = tracer,
                        spreadPitchScale = 1f,
                        spreadYawScale = 1f,
                        queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
                        hitEffectPrefab = EntityStates.Commando.CommandoWeapon.FireBarrage.hitEffectPrefab,
                        HitEffectNormal = false,
                        maxSpread = maxSpread,
                        minSpread = minSpread,
                        bulletCount = 1
                    };

                    bulletAttack.AddModdedDamageType(iDrive.ModdedDamageType);

                    if (this.aiming)
                    {
                        bulletAttack.modifyOutgoingDamageCallback = delegate (BulletAttack _bulletAttack, ref BulletAttack.BulletHit hitInfo, DamageInfo damageInfo)
                        {
                            if (BulletAttack.IsSniperTargetHit(hitInfo))
                            {
                                damageInfo.damage *= 2f;
                                damageInfo.damageColorIndex = DamageColorIndex.Sniper;

                                var effectData = new EffectData
                                {
                                    origin = hitInfo.point,
                                    rotation = Quaternion.LookRotation(-hitInfo.direction)
                                };

                                effectData.SetHurtBoxReference(hitInfo.hitHurtBox);
                                //EffectManager.SpawnEffect(BaseSnipeState.headshotEffectPrefab, effectData, true);
                                //RoR2.Util.PlaySound("Play_SniperClassic_headshot", base.gameObject);
                            }
                        };
                    }
                    bulletAttack.Fire();

                    this.characterMotor.ApplyForce(aimRay.direction * -this.selfForce);
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireDuration)
            {
                this.FireBullet();
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