﻿using UnityEngine;
using RoR2;
using EntityStates;
using UnityEngine.AddressableAssets;
using RobDriver.Modules.Components;
using RoR2.Projectile;

namespace RobDriver.SkillStates.Driver.MachineGun
{
    public class Zap : GenericProjectileBaseState
    {
        public static float damage = 3.8f;

        private uint playID;

        public override void OnEnter()
        {
            base.projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainTazer.prefab").WaitForCompletion();
            //base.effectPrefab = Modules.Assets.SomeMuzzleEffect;
            //targetmuzzle = "muzzleThrow"

            //base.attackSoundString = "sfx_driver_gun_throw";

            base.baseDuration = 0.8f;
            base.baseDelayBeforeFiringProjectile = 0.5f * base.baseDuration;
            base.damageCoefficient = 3.8f;
            base.force = 120f;
            base.projectilePitchBonus = 0f;
            //base.minSpread = 0;
            //base.maxSpread = 0;

            base.recoilAmplitude = 0.1f;
            base.bloom = 10;

            base.OnEnter();

            this.playID = Util.PlaySound("sfx_driver_zap_prep", this.gameObject);

            var iDrive = this.GetComponent<DriverController>();
            if (iDrive) iDrive.ConsumeAmmo();
        }

        public override void FixedUpdate() => base.FixedUpdate();

        public override void FireProjectile()
        {
            base.FireProjectile();

            Util.PlaySound("sfx_driver_zap", this.gameObject);

            AkSoundEngine.StopPlayingID(this.playID);
        }

        public override void OnExit()
        {
            base.OnExit();

            this.GetModelAnimator().SetTrigger("endAim");
            AkSoundEngine.StopPlayingID(this.playID);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return (base.fixedAge >= (this.duration * this.delayBeforeFiringProjectile) + 0.1f && this.firedProjectile)
                ? InterruptPriority.Any
                : InterruptPriority.Pain;
        }

        public override void PlayAnimation(float duration)
        {
            if (base.GetModelAnimator())
            {
                base.PlayAnimation("Gesture, Override", "Zap", "Action.playbackRate", this.duration);
                base.PlayAnimation("AimPitch", "Shoot");
            }
        }
    }
}