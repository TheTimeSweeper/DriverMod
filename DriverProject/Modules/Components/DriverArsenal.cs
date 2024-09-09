using RoR2;
using System.Linq;
using UnityEngine;

namespace RobDriver.Modules.Components
{
    public class DriverArsenal : MonoBehaviour
    {
        public GenericSkill weaponSkillSlot;

        public DriverWeaponDef DefaultWeapon
        {
            get
            {
                if (this.weaponSkillSlot && this.weaponSkillSlot.skillDef && !string.IsNullOrEmpty(this.weaponSkillSlot.skillDef.skillName))
                    return DriverWeaponCatalog.weaponDefs.FirstOrDefault(def => def && def.name == this.weaponSkillSlot.skillDef.skillName) ?? DriverWeaponCatalog.Pistol;

                return DriverWeaponCatalog.Pistol;
            }
        }
    }
}