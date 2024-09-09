using UnityEngine;
using RoR2;

namespace RobDriver.Modules.Components
{
    public class DriverAnimationEvents : MonoBehaviour
    {
        public void PlaySound(string soundString) => Util.PlaySound(soundString, this.gameObject);
    }
}