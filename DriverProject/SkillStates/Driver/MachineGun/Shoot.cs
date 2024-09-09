﻿using EntityStates;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RobDriver.SkillStates.Driver.MachineGun
{
    public class Shoot : BaseDriverSkillState
    {
        public static float damageCoefficient = 1.6f;
        public static float procCoefficient = 1f;
        public static float baseDuration = 0.21f;
        public static float force = 20f;
        public static float recoil = 0.5f;
        public static float range = 256f;
        public static GameObject tracerEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/TracerCommandoDefault");

        private float duration;
        private float fireTime;
        private bool hasFired;
        private string muzzleString;
        private bool isCrit;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = Shoot.baseDuration / this.attackSpeedStat;
            this.characterBody.isSprinting = false;

            this.fireTime = 0.1f * this.duration;
            base.characterBody.SetAimTimer(2f);
            this.muzzleString = "PistolMuzzle";

            this.isCrit = base.RollCrit();

            if (base.isAuthority)
            {
                this.hasFired = true;
                this.Fire();
            }

            if (this.isCrit) base.PlayAnimation("Gesture, Override", "FireMachineGunCritical", "Shoot.playbackRate", this.duration * 2f);
            else base.PlayAnimation("Gesture, Override", "FireMachineGun", "Shoot.playbackRate", this.duration * 2f);
            base.PlayAnimation("AimPitch", "Shoot");

            if (this.iDrive)
            {
                this.iDrive.ConsumeAmmo();
                this.iDrive.machineGunVFX.Play();
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            this.GetModelAnimator().SetTrigger("endAim");
        }

        private void Fire()
        {
            EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab, base.gameObject, this.muzzleString, false);

            if (this.isCrit) Util.PlaySound("sfx_driver_machinegun_shoot_critical", base.gameObject);
            else Util.PlaySound("sfx_driver_machinegun_shoot", base.gameObject);

            if (base.isAuthority)
            {
                var aimRay = base.GetAimRay();

                var recoilAmplitude = Shoot.recoil / this.attackSpeedStat;

                base.AddRecoil2(-1f * recoilAmplitude, -2f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);

                var bulletAttack = new BulletAttack
                {
                    bulletCount = 1,
                    aimVector = aimRay.direction,
                    origin = aimRay.origin,
                    damage = Shoot.damageCoefficient * this.damageStat,
                    damageColorIndex = DamageColorIndex.Default,
                    damageType = iDrive.DamageType,
                    falloffModel = BulletAttack.FalloffModel.None,
                    maxDistance = Shoot.range,
                    force = Shoot.force,
                    hitMask = LayerIndex.CommonMasks.bullet,
                    minSpread = 0f,
                    maxSpread = this.characterBody.spreadBloomAngle * 2.5f,
                    isCrit = this.isCrit,
                    owner = this.gameObject,
                    muzzleName = muzzleString,
                    smartCollision = true,
                    procChainMask = default(ProcChainMask),
                    procCoefficient = procCoefficient,
                    radius = 0.5f,
                    sniper = false,
                    stopperMask = LayerIndex.CommonMasks.bullet,
                    weapon = null,
                    tracerEffectPrefab = this.tracerPrefab,
                    spreadPitchScale = 1f,
                    spreadYawScale = 1f,
                    queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
                    hitEffectPrefab = EntityStates.Commando.CommandoWeapon.FirePistol2.hitEffectPrefab,
                };
                bulletAttack.AddModdedDamageType(iDrive.ModdedDamageType);
                bulletAttack.Fire();
            }

            base.characterBody.AddSpreadBloom(0.224f);
        }

        private GameObject tracerPrefab
        {
            get
            {
                if (this.isCrit) return Shoot.tracerEffectPrefab;
                else return Shoot.tracerEffectPrefab;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.fireTime && base.isAuthority)
            {
                if (!this.hasFired)
                {
                    this.hasFired = true;
                    this.Fire();
                }
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextState(new WaitForReload());
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            var kek = 0.5f;

            if (base.fixedAge >= kek * this.duration)
            {
                return InterruptPriority.Any;
            }

            return InterruptPriority.Skill;
        }
    }
}