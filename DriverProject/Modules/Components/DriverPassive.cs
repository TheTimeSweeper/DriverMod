using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace RobDriver.Modules.Components
{
    public class DriverPassive : MonoBehaviour
    {
        public SkillDef defaultPassive;
        public SkillDef pistolOnlyPassive;
        public SkillDef bulletsPassive;
        public SkillDef godslingPassive;

        public GenericSkill passiveSkillSlot;

        public bool isDefault => this.defaultPassive && this.passiveSkillSlot && this.passiveSkillSlot.skillDef == this.defaultPassive;

        public bool isPistolOnly => this.pistolOnlyPassive && this.passiveSkillSlot && this.passiveSkillSlot.skillDef == this.pistolOnlyPassive;

        public bool isBullets => this.bulletsPassive && this.passiveSkillSlot && this.passiveSkillSlot.skillDef == this.bulletsPassive;

        public bool isRyan => this.godslingPassive && this.passiveSkillSlot && this.passiveSkillSlot.skillDef == this.godslingPassive;
    }
}