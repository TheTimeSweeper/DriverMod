namespace RobDriver.SkillStates.Driver.GoldenGun
{
    public class AimLightsOut : Revolver.AimLightsOut
    {
        protected override void SetNextState() => this.outer.SetNextState(new LightsOut());
    }
}