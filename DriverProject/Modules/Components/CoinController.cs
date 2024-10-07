using System.Collections;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RobDriver.Modules.Components
{
    public class CoinController : NetworkBehaviour, IProjectileImpactBehavior, IOnIncomingDamageServerReceiver
    {
        public HealthComponent projectileHealthComponent;
        public ProjectileController controller;
        public DriverController iDrive;

        public NetworkSoundEventDef ricochetSound;

        public bool canRicochet = true;
        private float coolStopwatchScale = 0.01f;
        private bool startCoolStopwatch = false;
        public float ricochetMultiplier = 2f;
        private Vector3 rotationSpeed;
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
                EffectData effectData = new EffectData
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
            float speed = UnityEngine.Random.Range(500f, 2000f);
            this.rotationSpeed = new Vector3(speed, 0f, 0f);

            iDrive = controller.owner.GetComponent<DriverController>();
        }

        private IEnumerator SwapBack()
        {
            yield return new WaitForSeconds(0.25f);
            this.gameObject.layer = LayerIndex.entityPrecise.intVal;
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
                    TeamComponent teamComponent = damageInfo.attacker.GetComponent<TeamComponent>();
                    float co = damageInfo.damage / teamComponent.body.damage;
                    CoinRicochetOrb orb = new CoinRicochetOrb
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

                    EffectData effectData = new EffectData
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
        public void CmdRicochetMelee(GameObject attacker, GameObject inflictor, bool isCrit, float damage, uint procChainMask, Vector3 force, bool canRejectForce, byte colorIndex, uint damageType)
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
                damageType = (DamageType)damageType
            };
            this.damageInfo.procChainMask.mask = procChainMask;

            bounceCountStored++;
            coolStopwatchScale = (coolStopwatchScale * bounceCountStored) + 0.01f;
            startCoolStopwatch = true;
            canRicochet = false;
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
    }
}