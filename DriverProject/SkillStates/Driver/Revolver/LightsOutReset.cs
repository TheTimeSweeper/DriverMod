﻿using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RobDriver.SkillStates.Driver.Revolver
{
    public class LightsOutReset : LightsOut
    {
        protected override void FireBullet()
        {
            var aimRay = base.GetAimRay();

            var bulletAttack = new BulletAttack
            {
                bulletCount = 1,
                aimVector = aimRay.direction,
                origin = aimRay.origin,
                damage = LightsOut.damageCoefficient * this.damageStat,
                damageColorIndex = DamageColorIndex.Default,
                damageType = DamageType.ResetCooldownsOnKill | iDrive.DamageType,
                falloffModel = BulletAttack.FalloffModel.None,
                maxDistance = 9999f,
                force = 9999f,
                hitMask = LayerIndex.CommonMasks.bullet,
                minSpread = 0f,
                maxSpread = 0f,
                isCrit = this.RollCrit(),
                owner = this.gameObject,
                muzzleName = "PistolMuzzle",
                smartCollision = true,
                procChainMask = default(ProcChainMask),
                procCoefficient = LightsOut.procCoefficient,
                radius = 1f,
                sniper = false,
                stopperMask = LayerIndex.CommonMasks.bullet,
                weapon = null,
                tracerEffectPrefab = Shoot.critTracerEffectPrefab,
                spreadPitchScale = 1f,
                spreadYawScale = 1f,
                queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
                hitEffectPrefab = EntityStates.Commando.CommandoWeapon.FirePistol2.hitEffectPrefab,
            };
            bulletAttack.AddModdedDamageType(iDrive.ModdedDamageType);

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
                    EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/Common/VFX/WeakPointProcEffect.prefab").WaitForCompletion(), effectData, true);
                    Util.PlaySound("sfx_driver_headshot", base.gameObject);
                    hitInfo.hitHurtBox.healthComponent.gameObject.AddComponent<Modules.Components.DriverHeadshotTracker>();
                }
            };
            bulletAttack.Fire();
        }
    }
}