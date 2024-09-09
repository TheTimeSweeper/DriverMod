
using RoR2;
using RoR2.Orbs;

namespace RobDriver.Modules.Components
{
    public class CoinOrbEffect : OrbEffect
    {
        private EventFunctions eventFunctions;
        private void Awake()
        {
            eventFunctions = GetComponent<EventFunctions>();
            onArrival.AddListener(Event);
        }

        private void Event() => eventFunctions.UnparentTransform(base.transform.Find("Trail"));
    }
}
