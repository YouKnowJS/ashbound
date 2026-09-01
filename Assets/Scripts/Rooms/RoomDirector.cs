using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

namespace Ashbound
{
    public sealed class RoomDirector : MonoBehaviour
    {
        private EntityFactory factory;
        private PrototypeCatalog catalog;
        private CombatService combat;
        private readonly List<Combatant> enemies = new List<Combatant>();
        private bool watching;
        private int pendingSpawns;
        public RoomView View { get; private set; }
        public int RoomIndex { get; private set; }
        public int WaveIndex { get; private set; }
        public int RemainingEnemies { get { int count=0;foreach(var enemy in enemies)if(enemy&&enemy.Alive)count++;return count; } }
        public bool ExitOpen { get; private set; }
        public RoomDefinition Current => catalog.rooms[RoomIndex];
        public Combatant Boss { get; private set; }
        public EncounterDefinition CurrentEncounter { get; private set; }
        public event Action WaveCleared;
        public event Action BossDied;
        public event Action<EncounterDefinition> EncounterStarted;
        public event Action<EncounterDefinition> EncounterCompleted;
        public void Configure(EntityFactory factory, PrototypeCatalog catalog, CombatService combat)
        { this.factory = factory; this.catalog = catalog; this.combat = combat; View = gameObject.AddComponent<RoomView>(); }

        public void Load(int room)
        {
            Clear(); RoomIndex = room; WaveIndex = -1; ExitOpen = false;
            View.Build(Current.combatSpace, room); View.SetGate(false);
        }
        public void SpawnNextWave(int partySize)
        {
            StopAllCoroutines(); ClearEnemies(); WaveIndex++;
            var wave = Current.waves[WaveIndex];
            int index = 0;
            CurrentEncounter = wave.encounter;
            if (CurrentEncounter && CurrentEncounter.groups != null && CurrentEncounter.groups.Length > 0)
            {
                foreach (var group in CurrentEncounter.groups)
                {
                    if (!group.enemy) continue;
                    pendingSpawns += Mathf.Max(1, group.count);
                    if (group.startDelay <= 0 && group.spawnInterval <= 0)
                        for (int i = 0; i < Mathf.Max(1, group.count); i++) { Spawn(group.enemy, index++, partySize, group.presentation); pendingSpawns--; }
                    else StartCoroutine(SpawnGroup(group, index, partySize));
                    index += Mathf.Max(1, group.count);
                }
            }
            else foreach (var kind in wave.enemies) Spawn(kind, index++, partySize);
            for (int i = 1; i < partySize; i++) { Spawn(EnemyKind.Cinderling, index++, partySize); Spawn(EnemyKind.Lantern, index++, partySize); }
            watching = true;
            EncounterStarted?.Invoke(CurrentEncounter);
        }
        private IEnumerator SpawnGroup(EnemySpawnGroup group, int startIndex, int partySize)
        {
            if (group.startDelay > 0) yield return new WaitForSeconds(group.startDelay);
            for (int i = 0; i < Mathf.Max(1, group.count); i++)
            {
                Spawn(group.enemy, startIndex + i, partySize, group.presentation); pendingSpawns--;
                if (group.spawnInterval > 0 && i + 1 < group.count) yield return new WaitForSeconds(group.spawnInterval);
            }
        }
        private void Spawn(EnemyKind kind, int index, int partySize)
        {
            Vector3 position = Current.spawnPoints[index % Current.spawnPoints.Length];
            position += Vector3.right * (index / Current.spawnPoints.Length) * .8f;
            var enemy = factory.Enemy(kind, position, partySize); enemy.ScaleHealth(Current.enemyHealthMultiplier); enemies.Add(enemy);
        }
        private void Spawn(EnemyDefinition definition, int index, int partySize, SpawnPresentation presentation)
        {
            Vector3 position = Current.spawnPoints[index % Current.spawnPoints.Length];
            position += Vector3.right * (index / Current.spawnPoints.Length) * .8f;
            if (presentation == SpawnPresentation.Flight) position += Vector3.right * 1.2f;
            else if (presentation == SpawnPresentation.Burrow) position += Vector3.back * .7f;
            else if (presentation == SpawnPresentation.Rift) position += Vector3.forward * .7f;
            var enemy = factory.Enemy(definition, position, partySize); enemy.ScaleHealth(Current.enemyHealthMultiplier); enemies.Add(enemy);
            PresentSpawn(enemy, presentation);
        }
        private static void PresentSpawn(Combatant enemy, SpawnPresentation presentation)
        {
            Color tint = presentation == SpawnPresentation.Rift ? WeaponSkillExecutor.Tint(DamageElement.Void) : presentation == SpawnPresentation.Flight ? new Color(.55f,.75f,1) : Palette.Danger;
            CombatVfx.Ring(enemy.transform.position, presentation == SpawnPresentation.Burrow ? 1.5f : 1, tint, .55f, .1f, true);
        }
        public void DebugSpawnElementalGroup(ElementTag element,int partySize)
        {
            var choices=catalog.enemies.Where(x=>x&&x.element==element).Take(3).ToArray();int start=enemies.Count;
            foreach(var definition in choices)Spawn(definition,start++,partySize,definition.spawnPresentation);watching=true;
        }
        public Combatant DebugSpawnEnemy(EnemyDefinition definition,int partySize,bool elite=false)
        {
            if(!definition)return null;int before=enemies.Count;Spawn(definition,before,partySize,definition.spawnPresentation);var enemy=enemies[enemies.Count-1];if(elite){enemy.ScaleHealth(1.6f);enemy.Health.Shield(35);}watching=true;return enemy;
        }
        public void DebugSpawnEncounter(EncounterDefinition encounter,int partySize)
        {
            if(!encounter)return;ClearEnemies();CurrentEncounter=encounter;int index=0;foreach(var group in encounter.groups)for(int i=0;i<Mathf.Max(1,group.count);i++)Spawn(group.enemy,index++,partySize,group.presentation);watching=true;EncounterStarted?.Invoke(encounter);
        }
        public void SpawnBoss(int partySize)
        {
            ClearEnemies();
            Boss = factory.Boss(new Vector3(0, 0, 4.5f), partySize); enemies.Add(Boss);
            Boss.Health.Died += () => { if (Boss && combat.State == RunState.BossFight) BossDied?.Invoke(); };
        }
        public bool HasMoreWaves => !Current.isBoss && WaveIndex + 1 < Current.waves.Length;
        public void UnlockExit() { ExitOpen = true; View.SetGate(true); }
        private void Update()
        {
            if (watching && combat.Active && pendingSpawns == 0 && RemainingEnemies == 0) { watching = false; EncounterCompleted?.Invoke(CurrentEncounter); WaveCleared?.Invoke(); }
        }
        public void ClearTransientCombat()
        {
            foreach (var projectile in FindObjectsByType<CombatProjectile>()) Destroy(projectile.gameObject);
            foreach (var area in FindObjectsByType<AreaAttack>()) Destroy(area.gameObject);
            foreach (var actor in combat.Actors) if (actor) actor.Statuses.Clear();
        }
        public void ClearEnemies()
        {
            watching = false; pendingSpawns = 0;
            foreach (var enemy in enemies) if (enemy) { combat.Unregister(enemy); enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear(); Boss = null; CurrentEncounter = null;
        }
        public void Clear() { watching = false; ClearTransientCombat(); ClearEnemies(); ExitOpen = false; }
    }
}
