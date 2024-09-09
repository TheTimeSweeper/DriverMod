﻿using UnityEngine;
using RoR2;
using EntityStates;

namespace RobDriver.SkillStates.Driver
{
	public class Slide : BaseDriverSkillState
	{
		private Vector3 forwardDirection;
		private GameObject slideEffectInstance;
		private bool startedStateGrounded;
		private Animator animator;
		private bool classicSound;

		public override void OnEnter()
		{
			base.OnEnter();
			this.animator = this.GetModelAnimator();

			this.classicSound = Modules.Config.classicDodgeSound.Value;

			if (this.classicSound) Util.PlaySound(EntityStates.Commando.SlideState.soundString, base.gameObject);

			if (base.inputBank && base.characterDirection)
			{
				base.characterDirection.forward = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
			}

			if (base.characterMotor)
			{
				this.startedStateGrounded = base.characterMotor.isGrounded;
			}

			base.characterBody.SetSpreadBloom(0f, false);

			if (this.iDrive && this.iDrive.weaponDef)
            {
				if (this.iDrive.weaponDef.animationSet == DriverWeaponDef.AnimationSet.TwoHanded)
                {
					this.animator.SetBool("holding", true);
					base.PlayAnimation("Gesture, Override", "HoldGun");
                }
            }

			if (!this.startedStateGrounded)
			{
				if (!this.classicSound) Util.PlaySound("sfx_driver_air_dodge", this.gameObject);

				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CharacterLandImpact"), new EffectData
				{
					origin = base.characterBody.footPosition,
					scale = base.characterBody.radius
				}, true);

				/*this.animator.SetFloat("forwardSpeedCached", 1f);
				this.animator.SetFloat("rightSpeedCached", 0);
				this.PlayAnimation("Body", "BonusJump");*/
				this.PlayAnimation("FullBody, Override", "AirDodge");

				var velocity = base.characterMotor.velocity;
				velocity.y = base.characterBody.jumpPower;
				base.characterMotor.velocity = velocity;
				return;
			}

			if (!this.classicSound) Util.PlaySound("sfx_driver_dodge", this.gameObject);
			base.PlayAnimation("FullBody, Override", "Slide", "Slide.playbackRate", EntityStates.Commando.SlideState.slideDuration);

			if (EntityStates.Commando.SlideState.slideEffectPrefab)
			{
				var parent = base.FindModelChild("Root");
				this.slideEffectInstance = UnityEngine.Object.Instantiate<GameObject>(EntityStates.Commando.SlideState.slideEffectPrefab, parent);
			}
		}

        public override void Update()
        {
            base.Update();
			if (this.animator) this.animator.SetBool("isGrounded", this.isGrounded);
		}

        public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (this.animator) this.animator.SetBool("isGrounded", this.isGrounded);

			if (!this.isGrounded && this.slideEffectInstance) EntityState.Destroy(this.slideEffectInstance);

			if (base.isAuthority)
			{
				var num = this.startedStateGrounded ? EntityStates.Commando.SlideState.slideDuration : EntityStates.Commando.SlideState.jumpDuration;

				if (base.inputBank && base.characterDirection)
				{
					base.characterDirection.moveVector = base.inputBank.moveVector;
					this.forwardDirection = base.characterDirection.forward;
				}

				if (base.characterMotor)
				{
					float num2;

					if (this.startedStateGrounded)
					{
						num2 = EntityStates.Commando.SlideState.forwardSpeedCoefficientCurve.Evaluate(base.fixedAge / num);
					}
					else
					{
						num2 = EntityStates.Commando.SlideState.jumpforwardSpeedCoefficientCurve.Evaluate(base.fixedAge / num);
					}

					base.characterMotor.rootMotion += num2 * this.moveSpeedStat * this.forwardDirection * Time.fixedDeltaTime;
				}

				if (base.fixedAge >= num)
				{
					this.outer.SetNextStateToMain();
				}
			}
		}

		public override void OnExit()
		{
			this.PlayImpactAnimation();
			if (this.slideEffectInstance) EntityState.Destroy(this.slideEffectInstance);

			this.animator.SetBool("holding", false);

			base.OnExit();
		}

		private void PlayImpactAnimation()
		{
			var modelAnimator = base.GetModelAnimator();
			var layerIndex = modelAnimator.GetLayerIndex("Impact");
			if (layerIndex >= 0)
			{
				modelAnimator.SetLayerWeight(layerIndex, 1f);
			}
		}

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}