﻿using UnityEngine;
using RoR2;
using EntityStates;
using UnityEngine.Networking;

namespace RobDriver.SkillStates
{
    public class FuckMyAss : EntityStates.GenericCharacterDeath
    {
		public override void OnEnter()
		{
			base.OnEnter();

			var vector = Vector3.up * 3f;
			if (base.characterMotor)
			{
				vector += base.characterMotor.velocity;
				base.characterMotor.enabled = false;
			}

			if (base.cachedModelTransform)
			{
				var ragdollController = base.cachedModelTransform.GetComponent<RagdollController>();
				if (ragdollController)
				{
					// i hate that i have to do this

					foreach (var i in ragdollController.bones)
                    {
						if (i)
						{
							i.gameObject.layer = LayerIndex.ragdoll.intVal;
							i.gameObject.SetActive(true);
						}
                    }

					ragdollController.BeginRagdoll(vector);
				}
			}
		}

		public override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
		{
		}

		public override bool shouldAutoDestroy
		{
			get
			{
				return false;
			}
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active && base.fixedAge > 4f)
			{
				EntityState.Destroy(base.gameObject);
			}
		}

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Death;
    }
}