using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RobDriver.Modules.Components
{
    public class CoinController : NetworkBehaviour, IProjectileImpactBehavior, IOnIncomingDamageServerReceiver
    {
        public enum RicochetPriority
        {
            None,
            Body,
            Coin
        }

        public ProjectileController controller;
        public DriverController iDrive;

        public NetworkSoundEventDef ricochetSound;

        public bool canRicochet = true;
        private float coolStopwatchScale = 0.01f;
        private bool startCoolStopwatch = false;
        public float ricochetMultiplier = 2f;
        private Vector3 rotationSpeed = new(2000f, 0f, 0f);
        public int bounceCountStored = 0;
        private DamageInfo damageInfo;

        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            if (damageInfo.attacker && 
               (damageInfo.attacker.TryGetComponent<DriverController>(out _) ||
                damageInfo.attacker.TryGetComponent<CoinController>(out _)))
            {
                RicochetBullet(damageInfo);
            }
            else damageInfo.rejected = true;
        }

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (!impactInfo.collider.GetComponent<HurtBox>() && !impactInfo.collider.GetComponent<CoinController>())
            {
                var effectData = new EffectData
                {
                    origin = base.transform.position,
                    scale = 1f
                };
                EffectManager.SpawnEffect(Assets.coinImpact, effectData, transmit: true);
                Destroy(base.gameObject);
            }
        }

        private void Start()
        {
            var speed = UnityEngine.Random.Range(500f, 2000f);
            this.rotationSpeed = new Vector3(speed, 0f, 0f);

            iDrive = controller.owner.GetComponent<DriverController>();
            this.GetComponent<TeamFilter>().teamIndex = TeamIndex.Neutral;
        }

        private void FixedUpdate()
        {
            base.transform.Rotate(this.rotationSpeed * Time.fixedDeltaTime);
            if(startCoolStopwatch)
            {
                this.coolStopwatchScale -= Time.fixedDeltaTime;
                if (damageInfo.attacker && this.coolStopwatchScale <= 0f)
                {
                    this.canRicochet = false;
                    var teamComponent = damageInfo.attacker.GetComponent<TeamComponent>();
                    var co = damageInfo.damage / teamComponent.body.damage;
                    var orb = new CoinRicochetOrb
                    {
                        coinPosition = base.transform.position,
                        origin = base.transform.position,
                        speed = 180f + (10f * bounceCountStored),
                        attacker = this.damageInfo.attacker,
                        inflictor = this.damageInfo.inflictor,
                        damageCoefficient = co,
                        damageValue = this.damageInfo.damage * this.ricochetMultiplier,
                        teamIndex = teamComponent.teamIndex,
                        procCoefficient = 1f,
                        isCrit = this.damageInfo.crit,
                        bounceCount = bounceCountStored,
                        iDrive = this.iDrive
                    };

                    this.GetComponent<Rigidbody>().velocity = Vector3.zero;

                    OrbManager.instance.AddOrb(orb);

                    var effectData = new EffectData
                    {
                        origin = base.transform.position,
                        scale = 1f
                    };
                    EffectManager.SpawnEffect(Assets.coinImpact, effectData, transmit: true);
                    EffectManager.SimpleSoundEffect(this.ricochetSound.index, base.transform.position, true);

                    Destroy(base.gameObject);
                }
            }
        }

        [Command]
        public void CmdRicochetBullet(GameObject attacker, GameObject inflictor, bool isCrit, float damage, uint procChainMask, Vector3 force, bool canRejectForce, byte colorIndex, ulong damageType)
        {
            this.damageInfo = new DamageInfo
            {
                attacker = attacker,
                inflictor = inflictor,
                crit = isCrit,
                damage = damage,
                procCoefficient = 0f,
                force = force,
                canRejectForce = canRejectForce,
                damageColorIndex = (DamageColorIndex)colorIndex,
                damageType = damageType
            };
            this.damageInfo.procChainMask.mask = procChainMask;

            bounceCountStored++;
            coolStopwatchScale = (coolStopwatchScale * bounceCountStored) + 0.01f;
            startCoolStopwatch = true;
        }

        public void RicochetBullet(DamageInfo damageInfo)
        {
            if (this.damageInfo != null)
            {
                this.damageInfo.damage += damageInfo.damage * 0.5f;
                return;
            }
            this.damageInfo = damageInfo;
            this.damageInfo.procCoefficient = 0f;
            this.damageInfo.damageColorIndex = DamageColorIndex.Item;

            bounceCountStored++;
            coolStopwatchScale = (coolStopwatchScale * bounceCountStored) + 0.01f;
            startCoolStopwatch = true;
        }

        public static List<CoinController> OverlapAttackGetCoins(OverlapAttack attack)
        {
            var coinList = new List<CoinController>();
            foreach (var healthComponent in attack.ignoredHealthComponentList)
            {
                if (healthComponent && healthComponent.TryGetComponent<CoinController>(out var coin))
                {
                    coinList.Add(coin);
                }
            }
            return coinList;
        }
    }
}