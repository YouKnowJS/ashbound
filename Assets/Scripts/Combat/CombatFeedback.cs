using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public sealed class CombatFeedbackTuning
    {
        [Range(0, .12f)] public float lightStop = .035f;
        [Range(0, .12f)] public float criticalStop = .07f;
        [Range(0, .12f)] public float procStop = .055f;
        [Range(0, .12f)] public float heavyStop = .06f;
        [Range(0, .12f)] public float abilityStop = .075f;
        [Range(0, .12f)] public float majorStop = .095f;
        public float lightShake = .035f, heavyShake = .09f, majorShake = .14f;
    }

    public sealed class CombatFeedback : MonoBehaviour
    {
        private CombatService combat;
        public CombatFeedbackTuning tuning = new CombatFeedbackTuning();
        public bool HitStopEnabled { get; set; } = true;
        public bool CameraShakeEnabled { get; set; } = true;
        public bool VfxEnabled { get; set; } = true;
        public float HitStopUntil { get; private set; }
        public bool HitStopped => HitStopEnabled && Time.unscaledTime < HitStopUntil;
        public event Action<ImpactTier, DamageElement> AudioHook;
        public event Action<Vector3, float, bool> DamageNumberHook;

        public void Configure(CombatService owner) { combat = owner; combat.DamageResolved += OnDamage; }
        private void OnDestroy() { if (combat) combat.DamageResolved -= OnDamage; }
        private void OnDamage(DamageEvent hit)
        {
            ImpactTier tier = hit.Critical ? ImpactTier.Critical : hit.Info.Impact;
            float stop = tier == ImpactTier.Major ? tuning.majorStop : tier == ImpactTier.Ability ? tuning.abilityStop :
                tier == ImpactTier.Heavy ? tuning.heavyStop : tier == ImpactTier.Proc ? tuning.procStop :
                tier == ImpactTier.Critical ? tuning.criticalStop : tuning.lightStop;
            if (HitStopEnabled) HitStopUntil = Mathf.Max(HitStopUntil, Time.unscaledTime + stop);
            if (CameraShakeEnabled)
            {
                float strength = tier >= ImpactTier.Ability ? tuning.majorShake : tier >= ImpactTier.Heavy ? tuning.heavyShake : tuning.lightShake;
                ArenaCamera.Shake(strength, Mathf.Max(.07f, stop * 2));
            }
            if (VfxEnabled)
            {
                Color tint = ElementColor(hit.Info.Element);
                CombatVfx.Sparks(hit.Target.transform.position, hit.Critical ? Palette.Gold : tint, hit.Critical);
                if (hit.Target.View) hit.Target.View.Flash(Color.white, hit.Critical ? .12f : .075f);
            }
            AudioHook?.Invoke(tier, hit.Info.Element);
            DamageNumberHook?.Invoke(hit.Target.transform.position, hit.Loss.Total, hit.Critical || tier >= ImpactTier.Heavy);
        }
        private static Color ElementColor(DamageElement element)
        {
            switch (element)
            {
                case DamageElement.Fire: return new Color(1, .25f, .05f);
                case DamageElement.Frost: return new Color(.3f, .85f, 1);
                case DamageElement.Lightning: return Palette.Lightning;
                case DamageElement.Poison: return new Color(.35f, .95f, .25f);
                case DamageElement.Void: return new Color(.65f, .25f, 1);
                default: return new Color(1, .7f, .25f);
            }
        }
    }
}
