using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RobDriver.SkillStates.Driver.SupplyDrop.Nerfed
{
    public class AimCrapDrop : AimSupplyDrop
    {
        protected override void Cancel() => this.outer.SetNextState(new CancelCrapDrop());

        protected override void Fire()
        {
            var nextState = new FireCrapDrop();

            var indicatorTransform = this.areaIndicatorInstance ? this.areaIndicatorInstance.transform : transform;

            nextState.dropPosition = indicatorTransform.position;
            nextState.dropRotation = indicatorTransform.rotation;

            this.outer.SetNextState(nextState);
        }
    }
}