using System;
using System.Collections;
using UnityEngine;

namespace Ashbound
{
    public enum AudioCue { CampAmbience, Campfire, ForgeHammer, MapInteraction, UiConfirm, UiBack, NpcInteraction, ResourceGain, ExplorationMusic, CombatMusic, BossMusic, BossDeath, MusicFadeOut, Silence, CorruptionCue, FinalFightMusic, RunComplete }
    public sealed class AudioDirector : MonoBehaviour
    {
        public AudioClip campAmbience, campfire, forgeHammer, mapInteraction, uiConfirm, uiBack, npcInteraction, resourceGain;
        public AudioClip explorationMusic, combatMusic, bossMusic, bossDeath, corruptionCue, finalFightMusic;
        public event Action<AudioCue> Cue;
        private AudioSource music, effects;
        private Coroutine fade;
        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = .3f;
            effects = gameObject.AddComponent<AudioSource>(); effects.volume = .5f;
        }
        public void OnState(RunState state)
        {
            switch (state)
            {
                case RunState.Exploration: PlayMusic(AudioCue.ExplorationMusic, explorationMusic); break;
                case RunState.Combat: PlayMusic(AudioCue.CombatMusic, combatMusic); break;
                case RunState.BossFight: PlayMusic(AudioCue.BossMusic, bossMusic); break;
                case RunState.BossDefeated:
                    Cue?.Invoke(AudioCue.BossDeath); if (bossDeath) effects.PlayOneShot(bossDeath);
                    Cue?.Invoke(AudioCue.MusicFadeOut); fade = StartCoroutine(FadeToSilence()); break;
                case RunState.CorruptionTransition:
                    music.Stop(); Cue?.Invoke(AudioCue.CorruptionCue); if (corruptionCue) effects.PlayOneShot(corruptionCue); break;
                case RunState.FinalPvP: PlayMusic(AudioCue.FinalFightMusic, finalFightMusic); break;
                case RunState.RunComplete: music.Stop(); Cue?.Invoke(AudioCue.RunComplete); break;
                case RunState.Lobby: PlayMusic(AudioCue.CampAmbience,campAmbience); break;
            }
        }
        public void Emit(AudioCue cue)
        {
            Cue?.Invoke(cue);AudioClip clip=cue==AudioCue.Campfire?campfire:cue==AudioCue.ForgeHammer?forgeHammer:cue==AudioCue.MapInteraction?mapInteraction:cue==AudioCue.UiConfirm?uiConfirm:cue==AudioCue.UiBack?uiBack:cue==AudioCue.NpcInteraction?npcInteraction:cue==AudioCue.ResourceGain?resourceGain:null;if(clip)effects.PlayOneShot(clip);
        }
        private void PlayMusic(AudioCue cue, AudioClip clip)
        {
            if (fade != null) { StopCoroutine(fade); fade = null; }
            Cue?.Invoke(cue); if (music.clip == clip && music.isPlaying) return;
            music.Stop(); music.clip = clip; music.volume = .3f; if (clip) music.Play();
        }
        private IEnumerator FadeToSilence()
        {
            float t = 0;
            while (t < .7f) { t += Time.unscaledDeltaTime; music.volume = .3f * (1 - t / .7f); yield return null; }
            music.Stop(); Cue?.Invoke(AudioCue.Silence); fade = null;
        }
    }
}
