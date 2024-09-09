using System.Collections.Generic;
using R2API;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using System.Linq;

namespace RobDriver.Modules.Components
{
    public class CoinRicochetOrb : GenericDamageOrb
    {
        public float redDamageCoefficient = 16f;

        public float searchRadius = 50f;
        public GameObject inflictor;
        public SphereSearch search;
        public Vector3 coinPosition;
        public DriverController iDrive;
        public int bounceCount = 1;

        public float damageCoefficient = 1f;

        public override void Begin()
        {
            this.target = PickNextTarget(this.coinPosition);

            this.duration = this.distanceToTarget / this.speed;

            var color = Color.Lerp(Color.yellow, Color.red, damageCoefficient / redDamageCoefficient);
            var scale = Mathf.Lerp(1, 2f, damageCoefficient / redDamageCoefficient);
            var effectData = new EffectData
            {
                scale = this.scale * scale,
                origin = this.coinPosition,
                genericFloat = this.duration,
                color = color
            };
            effectData.SetHurtBoxReference(this.target);
            EffectManager.SpawnEffect(Assets.coinOrbEffect, effectData, true);
        }


        public override void OnArrival()
        {
            if (this.target)
            {
                var healthComponent = target.healthComponent;
                if (healthComponent)
                {
                    if (!target.TryGetComponent<CoinController>(out var iCoin) && bounceCount > 2)
                    {
                        var blastAttack = new BlastAttack
                        {
                            baseDamage = this.damageValue,
                            attacker = this.attacker,
                            teamIndex = this.attacker.GetComponent<TeamComponent>().teamIndex,
                            inflictor = this.inflictor,
                            baseForce = 1000f,
                            bonusForce = Vector3.zero,
                            crit = this.isCrit,
                            procChainMask = this.procChainMask,
                            procCoefficient = this.procCoefficient,
                            falloffModel = BlastAttack.FalloffModel.Linear,
                            position = target.transform.position,
                            radius = bounceCount * 2f,
                            damageColorIndex = this.damageColorIndex,
                            damageType = iDrive.DamageType
                        };
                        blastAttack.AddModdedDamageType(iDrive.ModdedDamageType);
                        blastAttack.AddModdedDamageType(DamageTypes.bloodExplosionIdentifier);
                        blastAttack.Fire();

                        var effectData = new EffectData
                        {
                            origin = target.transform.position,
                            scale = bounceCount
                        };
                        EffectManager.SpawnEffect(Assets.coinImpact, effectData, transmit: true);
                    }
                    else
                    {
                        var damageInfo = new DamageInfo
                        {
                            damage = this.damageValue,
                            attacker = this.attacker,
                            inflictor = this.inflictor,
                            force = Vector3.zero,
                            crit = this.isCrit,
                            procChainMask = this.procChainMask,
                            procCoefficient = this.procCoefficient,
                            position = this.target.transform.position,
                            damageColorIndex = this.damageColorIndex
                        };

                        if (iCoin)
                        {
                            damageInfo.procCoefficient = 0f;
                            iCoin.bounceCountStored = bounceCount;
                            healthComponent.TakeDamage(damageInfo);
                        }
                        else
                        {
                            damageInfo.damageType = DamageType.Stun1s | iDrive.DamageType;
                            damageInfo.AddModdedDamageType(DamageTypes.bloodExplosionIdentifier);
                            damageInfo.AddModdedDamageType(iDrive.ModdedDamageType);

                            healthComponent.TakeDamage(damageInfo);
                            GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                            GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
                        }
                    }
                }
            }
        }

        public HurtBox PickNextTarget(Vector3 position)
        {
            HurtBox target = null;

            this.search = new SphereSearch
            {
                mask = LayerIndex.entityPrecise.mask,
                radius = searchRadius,
                origin = position
            };

            var teamMask = TeamMask.GetUnprotectedTeams(teamIndex);
            var hurtBoxes = search.RefreshCandidates().OrderCandidatesByDistance().FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();
            var prio = CoinController.RicochetPriority.None;

            foreach (var hurtBox in hurtBoxes)
            {
                var coins = new List<CoinController>();
                hurtBox.healthComponent.GetComponents(coins);
                foreach (var coin in coins)
                {
                    if (coin.canRicochet && prio < CoinController.RicochetPriority.Coin)
                    {
                        target = hurtBox;
                        prio = CoinController.RicochetPriority.Coin;
                    }
                }

                var body = hurtBox.healthComponent.body;
                if (body && teamMask.HasTeam(body.teamComponent.teamIndex) && prio < CoinController.RicochetPriority.Body)
                {
                    target = hurtBox;
                    prio = CoinController.RicochetPriority.Body;
                }
            }
            return target;
        }
    }

}