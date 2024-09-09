namespace RobDriver.SkillStates.Driver.Revolver
{
    public class AimLightsOutReset : AimLightsOut
    {
        protected override void SetNextState() => this.outer.SetNextState(new LightsOutReset());
    }
}