﻿using RoR2;
using UnityEngine;
using EntityStates;
using UnityEngine.AddressableAssets;
using R2API;

namespace RobDriver.SkillStates.Driver.GolemGun
{
    public class Shoot : BaseDriverSkillState
    {
        public const float RAD2 = 1.414f;

        public static float damageCoefficient = 12f;
        public static float procCoefficient = 1f;
        public float baseDuration = 1f;
        public static int bulletCount = 1;
        public static float bulletSpread = 0f;
        public static float bulletRecoil = 12f;
        public static float bulletRange = 150f;
        public static float bulletThiccness = 1f;
        public float selfForce = -500f;

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
            this.earlyExitTime = 0.5f * this.duration;

            if (this.isCrit) Util.PlaySound("sfx_driver_lunar_rifle_shoot", base.gameObject);
            else Util.PlaySound("sfx_driver_lunar_rifle_shoot", base.gameObject);

            base.PlayAnimation("Gesture, Override", "FireTwohand", "Shoot.playbackRate", this.duration);
            base.PlayAnimation("AimPitch", "Shoot");

            this.fireDuration = 0;

            if (this.iDrive) this.iDrive.ConsumeAmmo();
        }

        public virtual void FireBullet()
        {
            if (!this.hasFired)
            {
                this.hasFired = true;

                var recoilAmplitude = Shoot.bulletRecoil / this.attackSpeedStat;

                base.AddRecoil(-0.4f * recoilAmplitude, -0.8f * recoilAmplitude, -0.3f * recoilAmplitude, 0.3f * recoilAmplitude);
                this.characterBody.AddSpreadBloom(4f);
                EffectManager.SimpleMuzzleFlash(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/MuzzleflashLunarShard.prefab").WaitForCompletion(), gameObject, muzzleString, false);

                var tracer = Modules.Assets.lunarTracer;
                //if (this.isCrit) tracer = Modules.Assets.shotgunTracerCrit;

                if (base.isAuthority)
                {
                    var damage = Shoot.damageCoefficient * this.damageStat;

                    var aimRay = GetAimRay();

                    var spread = Shoot.bulletSpread;
                    var thiccness = Shoot.bulletThiccness;
                    float force = 2500;

                    var bulletAttack = new BulletAttack
                    {
                        aimVector = aimRay.direction,
                        origin = aimRay.origin,
                        damage = damage,
                        damageColorIndex = DamageColorIndex.Default,
                        damageType = iDrive.DamageType,
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
                        radius = thiccness,
                        sniper = false,
                        stopperMask = LayerIndex.CommonMasks.bullet,
                        weapon = null,
                        tracerEffectPrefab = tracer,
                        spreadPitchScale = 1f,
                        spreadYawScale = 1f,
                        queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
                        hitEffectPrefab = EntityStates.Commando.CommandoWeapon.FireBarrage.hitEffectPrefab,
                        HitEffectNormal = false,
                    };
                    bulletAttack.AddModdedDamageType(iDrive.ModdedDamageType);
                    bulletAttack.minSpread = 0;
                    bulletAttack.maxSpread = 0;
                    bulletAttack.bulletCount = 1;
                    bulletAttack.Fire();

                    var secondShot = (uint)Mathf.CeilToInt(bulletCount / 2f) - 1;
                    bulletAttack.minSpread = 0;
                    bulletAttack.maxSpread = spread / 1.45f;
                    bulletAttack.bulletCount = secondShot;
                    bulletAttack.Fire();

                    bulletAttack.minSpread = spread / 1.45f;
                    bulletAttack.maxSpread = spread;
                    bulletAttack.bulletCount = (uint)Mathf.FloorToInt(bulletCount / 2f);
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
                base.PlayAnimation("Gesture, Override", "BufferEmpty");
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