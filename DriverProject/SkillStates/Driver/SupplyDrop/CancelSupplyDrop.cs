using EntityStates;

namespace RobDriver.SkillStates.Driver.SupplyDrop
{
    public class CancelSupplyDrop : BaseDriverSkillState
    {
        public float baseDuration = 0.25f;

        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;

            base.PlayCrossfade("Gesture, Override", "BufferEmpty", this.duration / 2f);
        }

        public override void OnExit()
        {
            base.OnExit();
            this.HideButton();
        }

        protected virtual void HideButton() => this.FindModelChild("ButtonModel").gameObject.SetActive(false);

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge >= this.duration)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Pain;
    }
}