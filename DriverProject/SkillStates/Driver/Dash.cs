﻿using EntityStates;
using RoR2;
using UnityEngine;

namespace RobDriver.SkillStates.Driver
{
    public class Dash : BaseState
    {
        protected Vector3 slipVector = Vector3.zero;
        public float duration = 0.3f;
        public float speedCoefficient = 7f;
        private Vector3 cachedForward;

        public override void OnEnter()
        {
            base.OnEnter();
            this.slipVector = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
            this.cachedForward = this.characterDirection.forward;

            var anim = this.GetModelAnimator();

            var rhs = base.characterDirection ? base.characterDirection.forward : this.slipVector;
            var rhs2 = Vector3.Cross(Vector3.up, rhs);
            var num = Vector3.Dot(this.slipVector, rhs);
            var num2 = Vector3.Dot(this.slipVector, rhs2);
            anim.SetFloat("dashF", num);
            anim.SetFloat("dashR", num2);

            base.PlayCrossfade("FullBody, Override", "Dash", "Dash.playbackRate", this.duration * 1.5f, 0.05f);
            base.PlayAnimation("Gesture, Override", "BufferEmpty");

            Util.PlaySound("sfx_driver_dash", this.gameObject);

            this.ApplyBuff();
            this.CreateDashEffect();
        }

        public virtual void ApplyBuff()
        {
            //if (base.isAuthority) base.healthComponent.AddBarrierAuthority(StaticValues.dashBarrierAmount * base.healthComponent.fullBarrier);
        }

        public virtual void CreateDashEffect()
        {
            /*EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(this.slipVector);
            effectData.origin = base.characterBody.corePosition;

            EffectManager.SpawnEffect(Modules.Assets.dashFX, effectData, false);*/
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            base.characterMotor.velocity = Vector3.zero;
            base.characterMotor.rootMotion = this.slipVector * (this.moveSpeedStat * this.speedCoefficient * Time.fixedDeltaTime) * Mathf.Cos(base.fixedAge / this.duration * 1.57079637f);

            if (base.isAuthority)
            {
                if (base.characterDirection)
                {
                    base.characterDirection.forward = this.cachedForward;
                }
            }

            if (base.isAuthority && base.fixedAge >= this.duration)
            {
                this.outer.SetNextState(new WaitForReload());
            }
        }

        public virtual void DampenVelocity() => base.characterMotor.velocity *= 0.8f;

        public override void OnExit()
        {
            this.DampenVelocity();
            //base.PlayAnimation("FullBody, Override", "BufferEmpty");

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}