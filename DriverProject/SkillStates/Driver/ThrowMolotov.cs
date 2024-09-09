using UnityEngine;
using RoR2;
using EntityStates;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;

namespace RobDriver.SkillStates.Driver
{
    public class ThrowMolotov : GenericProjectileBaseState
    {
        public static float DamageCoefficient = 5f;

        public override void OnEnter()
        {
            base.attackSoundString = "sfx_driver_gun_throw";

            base.baseDuration = 0.55f;
            base.baseDelayBeforeFiringProjectile = 0.1f * base.baseDuration;

            base.damageCoefficient = DamageCoefficient;
            base.force = 120f;

            base.projectilePitchBonus = -7.5f;
            //base.minSpread = 0;
            //base.maxSpread = 0;

            base.recoilAmplitude = 0.1f;
            base.bloom = 10;

            base.OnEnter();
        }

        public override void FireProjectile()
        {
            // if i just rewrite it surely it can't break right?
            if (base.isAuthority)
            {
                var projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Molotov/MolotovClusterProjectile.prefab").WaitForCompletion();

                var aimRay = base.GetAimRay();
                aimRay = this.ModifyProjectileAimRay(aimRay);
                aimRay.direction = Util.ApplySpread(aimRay.direction, this.minSpread, this.maxSpread, 1f, 1f, 0f, this.projectilePitchBonus);

                // copied from moff's rocket
                // the fact that this item literally has to be hardcoded into character skillstates makes me so fucking angry you have no idea
                if (this.characterBody.inventory && this.characterBody.inventory.GetItemCount(DLC1Content.Items.MoreMissile) > 0)
                {
                    var damageMult = DriverPlugin.GetICBMDamageMult(this.characterBody);

                    var rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                    var axis = Vector3.Cross(aimRay.direction, rhs);

                    var currentSpread = 0f;
                    var angle = 0f;
                    var num2 = 0f;
                    num2 = UnityEngine.Random.Range(1f + currentSpread, 1f + currentSpread) * 3f;   //Bandit is x2
                    angle = num2 / 2f;  //3 - 1 rockets

                    var direction = Quaternion.AngleAxis(-num2 * 0.5f, axis) * aimRay.direction;
                    var rotation = Quaternion.AngleAxis(angle, axis);
                    var aimRay2 = new Ray(aimRay.origin, direction);
                    for (var i = 0; i < 3; i++)
                    {
                        ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay2.origin, Util.QuaternionSafeLookRotation(aimRay2.direction), this.gameObject, damageMult * this.damageStat * ThrowGrenade.DamageCoefficient, this.force, this.RollCrit(), DamageColorIndex.Default, null, -1f);
                        aimRay2.direction = rotation * aimRay2.direction;
                    }
                }
                else
                {
                    ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), this.gameObject, this.damageStat * ThrowGrenade.DamageCoefficient, this.force, this.RollCrit(), DamageColorIndex.Default, null, -1f);
                }
            }
        }

        public override void FixedUpdate() => base.FixedUpdate();

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Pain;

        public override void PlayAnimation(float duration)
        {
            if (base.GetModelAnimator())
            {
                base.PlayAnimation("Gesture, Override", "ThrowGrenade", "Grenade.playbackRate", this.duration);
            }
        }
    }
}