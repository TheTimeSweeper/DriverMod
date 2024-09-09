﻿using UnityEngine;
using RoR2;
using EntityStates;
using RobDriver.Modules;
using RobDriver.SkillStates.Emote;
using BepInEx.Configuration;
using RobDriver.Modules.Components;

namespace RobDriver.SkillStates.Driver
{
    public class MainState : GenericCharacterMain
    {
		private Animator animator;
		public LocalUser localUser;
		private DriverController iDrive;

		public override void OnEnter()
        {
            base.OnEnter();
            if (!iDrive) iDrive = this.GetComponent<DriverController>();
            this.animator = this.modelAnimator;
			this.FindLocalUser();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (this.animator)
            {
                var cock = false;
                if (!this.characterBody.outOfDanger || !this.characterBody.outOfCombat) cock = true;

                this.animator.SetBool("inCombat", cock);

				if (this.isGrounded) this.animator.SetFloat("airBlend", 0f);
				else this.animator.SetFloat("airBlend", 1f);
            }
			//emotes
			if (base.isAuthority && base.characterMotor.isGrounded)
			{
				this.CheckEmote<Rest>(Config.restKey);
				this.CheckEmote<Taunt>(Config.tauntKey);
				this.CheckEmote<Dance>(Config.danceKey);
			}
		}

		private void CheckEmote(KeyCode keybind, EntityState state)
		{
			if (Input.GetKeyDown(keybind))
			{
				if (!localUser.isUIFocused)
				{
					outer.SetInterruptState(state, InterruptPriority.Any);
				}
			}
		}

		private void CheckEmote<T>(ConfigEntry<KeyboardShortcut> keybind) where T : EntityState, new()
		{
			if (Modules.Config.GetKeyPressed(keybind))
			{
				FindLocalUser();

				if (localUser != null && !localUser.isUIFocused)
				{
					outer.SetInterruptState(new T(), InterruptPriority.Any);
				}
			}
		}

		private void FindLocalUser()
		{
			if (this.localUser == null)
			{
				if (base.characterBody)
				{
					foreach (var lu in LocalUserManager.readOnlyLocalUsersList)
					{
						if (lu.cachedBody == base.characterBody)
						{
							this.localUser = lu;
							break;
						}
					}
				}
			}
		}

		public override void ProcessJump()
        {
			if (this.hasCharacterMotor)
			{
				var hopooFeather = false;
				var waxQuail = false;

				if (this.jumpInputReceived && base.characterBody && base.characterMotor.jumpCount < base.characterBody.maxJumpCount)
				{
					var waxQuailCount = base.characterBody.inventory.GetItemCount(RoR2Content.Items.JumpBoost);
					var horizontalBonus = 1f;
					var verticalBonus = 1f;

					if (base.characterMotor.jumpCount >= base.characterBody.baseJumpCount)
					{
						this.iDrive.featherTimer = 0.1f;
						hopooFeather = true;
						horizontalBonus = 1.5f;
						verticalBonus = 1.5f;
					}
					else if (waxQuailCount > 0 && base.characterBody.isSprinting)
					{
						var v = base.characterBody.acceleration * base.characterMotor.airControl;

						if (base.characterBody.moveSpeed > 0f && v > 0f)
						{
							waxQuail = true;
							var num2 = Mathf.Sqrt(10f * (float)waxQuailCount / v);
							var num3 = base.characterBody.moveSpeed / v;
							horizontalBonus = (num2 + num3) / num3;
						}
					}

					GenericCharacterMain.ApplyJumpVelocity(base.characterMotor, base.characterBody, horizontalBonus, verticalBonus, false);

					if (this.hasModelAnimator)
					{
						var layerIndex = base.modelAnimator.GetLayerIndex("Body");
						if (layerIndex >= 0)
						{
							if (this.characterBody.isSprinting)
                            {
								this.modelAnimator.CrossFadeInFixedTime("SprintJump", this.smoothingParameters.intoJumpTransitionTime, layerIndex);
							}
							else
                            {
								if (hopooFeather)
                                {
									this.modelAnimator.CrossFadeInFixedTime("BonusJump", this.smoothingParameters.intoJumpTransitionTime, layerIndex);
								}
								else
                                {
									this.modelAnimator.CrossFadeInFixedTime("Jump", this.smoothingParameters.intoJumpTransitionTime, layerIndex);
								}
                            }
						}
					}

					if (hopooFeather)
					{
						EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/FeatherEffect"), new EffectData
						{
							origin = base.characterBody.footPosition
						}, true);
					}
					else if (base.characterMotor.jumpCount > 0)
					{
						EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CharacterLandImpact"), new EffectData
						{
							origin = base.characterBody.footPosition,
							scale = base.characterBody.radius
						}, true);
					}

					if (waxQuail)
					{
						EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BoostJumpEffect"), new EffectData
						{
							origin = base.characterBody.footPosition,
							rotation = Util.QuaternionSafeLookRotation(base.characterMotor.velocity)
						}, true);
					}

					base.characterMotor.jumpCount++;

					// set up double jump anim
					if (this.animator)
					{
						var x = this.animatorWalkParamCalculator.animatorWalkSpeed.y;
						var y = this.animatorWalkParamCalculator.animatorWalkSpeed.x;

						// neutral jump
						if (Mathf.Abs(x) <= 0.45f && Mathf.Abs(y) <= 0.45f || this.inputBank.moveVector == Vector3.zero)
						{
							x = 0f;
							y = 0f;
						}

						if (Mathf.Abs(x) > Mathf.Abs(y))
						{
							// side flip
							if (x > 0f) x = 1f;
							else x = -1f;
							y = 0f;
						}
						else if (Mathf.Abs(x) < Mathf.Abs(y))
						{
							// forward/backflips
							if (y > 0f) y = 1f;
							else y = -1f;
							x = 0f;
						}
						// eh this feels less dynamic. ignore the slight anim clipping issues ig and just blend them
						//  actualyl don't because the clipping issues are nightmarish

						// have to cache it at time of jump otherwise you can fuck up the jump anim in weird ways by turning during it
						this.animator.SetFloat("forwardSpeedCached", y);
						this.animator.SetFloat("rightSpeedCached", x);
						// turns out this wasn't even used in the end. the animation didn't break at all in practice, only in theory
						// Fuck You rob you fucking moron

						//  update: this was actually used. what the hell are you doing?
					}
				}
			}
		}
    }
}