using RoR2;
using System.Linq;
using UnityEngine;

namespace RobDriver.Modules.Components
{
    public class DriverArsenal : MonoBehaviour
    {
        public GenericSkill weaponSkillSlot;
        private DriverWeaponDef _defaultWeapon;

        public DriverWeaponDef DefaultWeapon
        {
            get
            {
                if (!this._defaultWeapon)
                {
                    if (this.weaponSkillSlot && this.weaponSkillSlot.skillDef && !string.IsNullOrEmpty(this.weaponSkillSlot.skillDef.skillName))
                    {
                        var skillName = this.weaponSkillSlot.skillDef.skillName;
                        _defaultWeapon = DriverWeaponCatalog.weaponDefs.FirstOrDefault(def => def && def.nameToken == skillName);
                    }

                    if (!_defaultWeapon)
                        _defaultWeapon = DriverWeaponCatalog.Pistol;
                }

                return _defaultWeapon;
            }
        }
    }
}