using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RobDriver.SkillStates.Driver.SupplyDrop.Scepter
{
    public class AimVoidDrop : AimSupplyDrop
    {
        private GameObject effectInstance;

        public override void OnEnter()
        {
            base.OnEnter();

            this.effectInstance = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidSurvivor/VoidSurvivorChargeMegaBlaster.prefab").WaitForCompletion());
            this.effectInstance.transform.parent = this.FindModelChild("HandL");
            this.effectInstance.transform.localPosition = new Vector3(-0.5f, 0f, -0.2f);
            this.effectInstance.transform.localRotation = Quaternion.identity;
        }

        public override void OnExit()
        {
            base.OnExit();

            if (this.effectInstance) Destroy(this.effectInstance);
        }

        protected override void Cancel() => this.outer.SetNextState(new CancelVoidDrop());

        protected override void Fire()
        {
            var nextState = new FireVoidDrop();

            var indicatorTransform = this.areaIndicatorInstance ? this.areaIndicatorInstance.transform : transform;

            nextState.dropPosition = indicatorTransform.position;
            nextState.dropRotation = indicatorTransform.rotation;

            this.outer.SetNextState(nextState);
        }

        protected override void HideButton()
        {
        }

        protected override void ShowButton()
        {
            base.PlayAnimation("Gesture, Override", "ReadyVoidButton", "Action.playbackRate", 0.8f);
            base.PlayAnimation("AimPitch", "SteadyAimPitch");
        }
    }
}