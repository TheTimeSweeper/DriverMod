namespace RobDriver.SkillStates.Driver.PyriteGun
{
    public class SteadyAim : Driver.SteadyAim
    {
        protected override float _damageCoefficient => 6f;
        protected override string GetSoundString(bool crit, bool charged) => "sfx_driver_pistol_shoot_charged";
    }
}