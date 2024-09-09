using RoR2;
using UnityEngine;
using EntityStates;
using UnityEngine.AddressableAssets;

namespace RobDriver.SkillStates.Driver.GolemGun
{
	public class ChargeLaser : BaseDriverSkillState
	{
		public static float baseDuration = 0.3f;
		public static float laserMaxWidth = 0.2f;

		private float duration;
		private uint chargePlayID;
		private GameObject chargeEffect;
		private GameObject laserEffect;
		private LineRenderer laserLineComponent;
		private Vector3 laserDirection;
		private Vector3 visualEndPosition;
		private float flashTimer;
		private bool laserOn;

		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = ChargeLaser.baseDuration / this.attackSpeedStat;
			var modelTransform = base.GetModelTransform();

			this.chargePlayID = Util.PlayAttackSpeedSound(EntityStates.GolemMonster.ChargeLaser.attackSoundString, this.gameObject, 10f + this.attackSpeedStat);

			if (modelTransform)
			{
				var component = modelTransform.GetComponent<ChildLocator>();
				if (component)
				{
					var transform = component.FindChild("ShotgunMuzzle");
					if (transform)
					{
						if (EntityStates.GolemMonster.ChargeLaser.effectPrefab)
						{
							this.chargeEffect = UnityEngine.Object.Instantiate<GameObject>(EntityStates.GolemMonster.ChargeLaser.effectPrefab, transform.position, transform.rotation);
							this.chargeEffect.transform.parent = transform;
							var component2 = this.chargeEffect.GetComponent<ScaleParticleSystemDuration>();
							if (component2)
							{
								component2.newDuration = this.duration;
							}
						}

						if (EntityStates.GolemMonster.ChargeLaser.laserPrefab)
						{
							this.laserEffect = UnityEngine.Object.Instantiate<GameObject>(EntityStates.GolemMonster.ChargeLaser.laserPrefab, transform.position, transform.rotation);
							this.laserEffect.transform.parent = transform;
							this.laserLineComponent = this.laserEffect.GetComponent<LineRenderer>();
						}
					}
				}
			}

			if (base.characterBody)
			{
				base.characterBody.SetAimTimer(this.duration);
			}

			this.flashTimer = 0f;
			this.laserOn = true;

			base.PlayCrossfade("Gesture, Override", "AimTwohand", this.duration * 0.5f);
			base.PlayAnimation("AimPitch", "ShotgunAimPitch");
		}

		public override void OnExit()
		{
			AkSoundEngine.StopPlayingID(this.chargePlayID);
			base.OnExit();
			if (this.chargeEffect)
			{
				EntityState.Destroy(this.chargeEffect);
			}
			if (this.laserEffect)
			{
				EntityState.Destroy(this.laserEffect);
			}

			if (this.outer.destroying)
			{
				base.PlayAnimation("Gesture, Override", "BufferEmpty");
				base.PlayAnimation("AimPitch", "AimPitch");
			}
		}

		public override void Update()
		{
			base.Update();
			if (this.laserEffect && this.laserLineComponent)
			{
				var num = 1000f;
				var aimRay = base.GetAimRay();
				var position = this.laserEffect.transform.parent.position;
				var point = aimRay.GetPoint(num);
				this.laserDirection = point - position;
				RaycastHit raycastHit;
				if (Physics.Raycast(aimRay, out raycastHit, num, LayerIndex.world.mask | LayerIndex.entityPrecise.mask))
				{
					point = raycastHit.point;
				}
				this.laserLineComponent.SetPosition(0, position);
				this.laserLineComponent.SetPosition(1, point);
				float num2;
				if (this.duration - base.age > 0.5f)
				{
					num2 = base.age / this.duration;
				}
				else
				{
					this.flashTimer -= Time.deltaTime;
					if (this.flashTimer <= 0f)
					{
						this.laserOn = !this.laserOn;
						this.flashTimer = 0.033333335f;
					}
					num2 = (this.laserOn ? 1f : 0f);
				}
				num2 *= ChargeLaser.laserMaxWidth;
				this.laserLineComponent.startWidth = num2;
				this.laserLineComponent.endWidth = num2;
			}
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			this.characterBody.outOfCombatStopwatch = 0f;
			base.characterBody.SetAimTimer(0.2f);

			if (this.iDrive && this.iDrive.weaponDef.nameToken != this.cachedWeaponDef.nameToken)
			{
				base.PlayAnimation("Gesture, Override", "BufferEmpty");
				this.outer.SetNextStateToMain();
				return;
			}

			if (base.fixedAge >= this.duration && base.isAuthority)
			{
				var fireLaser = new FireLaser();
				fireLaser.laserDirection = this.laserDirection;
				this.outer.SetNextState(fireLaser);
				return;
			}
		}

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Skill;
    }
}