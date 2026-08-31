using System.Collections;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class WeaponSkillExecutor : MonoBehaviour
    {
        private Combatant actor;
        public void Configure(Combatant owner){actor=owner;}
        public bool Execute(WeaponSkillDefinition skill)
        {
            if(!skill||!actor.Weapon||actor.Weapon.rarity<skill.minimumRarity)return false;
            DamageElement element=Element(skill.elements.FirstOrDefault()); Color color=Tint(element);
            if(skill.vfxPrefab)Instantiate(skill.vfxPrefab,actor.transform.position,actor.transform.rotation);
            if(skill.audioClip)AudioSource.PlayClipAtPoint(skill.audioClip,actor.transform.position);
            switch(skill.delivery)
            {
                case SkillDelivery.MeleeDash:
                    actor.Motor.Lunge(actor.Motor.Facing,skill.movementDistance/Mathf.Max(.1f,.3f),.3f);
                    StartCoroutine(DelayedBurst(skill,.16f,actor.transform.position+actor.Motor.Facing*skill.movementDistance*.55f,element,color)); break;
                case SkillDelivery.ProjectileVolley:
                    for(int i=0;i<Mathf.Max(1,skill.projectileCount);i++){float a=(i-(skill.projectileCount-1)*.5f)*10;CombatProjectile.Spawn(actor,Quaternion.Euler(0,a,0)*actor.Motor.Facing,skill.projectileSpeed,skill.damage,color,2,element,ImpactTier.Ability,true);} break;
                case SkillDelivery.PersistentZone:
                case SkillDelivery.GravityWell:
                    ElementalZone.Spawn(actor,actor.transform.position+actor.Motor.Facing*Mathf.Max(1,skill.movementDistance),skill,element,color); break;
                default: Burst(skill,actor.transform.position+actor.Motor.Facing*Mathf.Min(1.5f,skill.radius*.4f),element,color); break;
            }
            actor.Combat.RecordProc(actor,"weapon-skill:"+skill.id); return true;
        }
        private IEnumerator DelayedBurst(WeaponSkillDefinition skill,float delay,Vector3 position,DamageElement element,Color color){yield return new WaitForSeconds(delay);if(actor&&actor.Alive)Burst(skill,position,element,color);}
        private void Burst(WeaponSkillDefinition skill,Vector3 center,DamageElement element,Color color)
        {
            CombatVfx.Pulse(center,skill.radius,color);
            foreach(var target in actor.Combat.Actors.ToArray())if(actor.Combat.AreEnemies(actor,target)&&(target.transform.position-center).sqrMagnitude<=skill.radius*skill.radius)
            {actor.Combat.DealDamage(target,new DamageInfo(actor,skill.damage,skill.damageKind,(target.transform.position-center).normalized,.2f,5,true,true,element,ImpactTier.Ability));foreach(var status in skill.statuses)target.Statuses.Apply(actor,status);}
        }
        public static DamageElement Element(ElementTag element)=>element==ElementTag.Fire?DamageElement.Fire:element==ElementTag.Frost?DamageElement.Frost:element==ElementTag.Lightning?DamageElement.Lightning:element==ElementTag.Poison?DamageElement.Poison:element==ElementTag.Void?DamageElement.Void:DamageElement.Physical;
        public static Color Tint(DamageElement element)=>element==DamageElement.Fire?new Color(1,.25f,.05f):element==DamageElement.Frost?Color.cyan:element==DamageElement.Lightning?Palette.Lightning:element==DamageElement.Poison?new Color(.3f,.9f,.2f):element==DamageElement.Void?new Color(.65f,.2f,1):Palette.Player;
    }

    public sealed class ElementalZone : MonoBehaviour
    {
        private Combatant owner; private WeaponSkillDefinition skill; private DamageElement element; private float remaining,tick;
        public static void Spawn(Combatant owner,Vector3 position,WeaponSkillDefinition skill,DamageElement element,Color color)
        {var obj=new GameObject(skill.displayName+" zone");obj.transform.position=position;CombatVfx.Ring(position,skill.radius,color,skill.duration,.12f,true).transform.SetParent(obj.transform,true);var zone=obj.AddComponent<ElementalZone>();zone.owner=owner;zone.skill=skill;zone.element=element;zone.remaining=skill.duration;}
        private void Update()
        {
            if(!owner||!owner.Alive||!owner.Combat.Active){Destroy(gameObject);return;}remaining-=Time.deltaTime;tick-=Time.deltaTime;if(remaining<=0){Destroy(gameObject);return;}if(tick>0)return;tick=.5f;
            foreach(var target in owner.Combat.Actors.ToArray())if(owner.Combat.AreEnemies(owner,target)&&(target.transform.position-transform.position).sqrMagnitude<=skill.radius*skill.radius)
            {
                owner.Combat.DealDamage(target,new DamageInfo(owner,skill.damage*.2f,skill.damageKind,(target.transform.position-transform.position).normalized,.04f,0,false,false,element,ImpactTier.Proc));
                foreach(var status in skill.statuses)target.Statuses.Apply(owner,status);
                if(skill.delivery==SkillDelivery.GravityWell&&!target.IsBoss){Vector3 pull=(transform.position-target.transform.position);pull.y=0;target.Motor.Impact(pull.normalized*1.4f,0);}
            }
        }
    }
}
