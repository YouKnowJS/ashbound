using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class RoomDirector : MonoBehaviour
    {
        private EntityFactory factory;
        private PrototypeCatalog catalog;
        private CombatService combat;
        private readonly List<Combatant> enemies = new List<Combatant>();
        private bool watching;
        public RoomView View { get; private set; }
        public int RoomIndex { get; private set; }
        public int WaveIndex { get; private set; }
        public int RemainingEnemies => enemies.Count(x => x && x.Alive);
        public bool ExitOpen { get; private set; }
        public RoomDefinition Current => catalog.rooms[RoomIndex];
        public Combatant Boss { get; private set; }
        public event Action WaveCleared;
        public event Action BossDied;
        public void Configure(EntityFactory factory, PrototypeCatalog catalog, CombatService combat)
        { this.factory = factory; this.catalog = catalog; this.combat = combat; View = gameObject.AddComponent<RoomView>(); }

        public void Load(int room)
        {
            Clear(); RoomIndex = room; WaveIndex = -1; ExitOpen = false;
            View.Build(room); View.SetGate(false);
        }
        public void SpawnNextWave(int partySize)
        {
            ClearEnemies(); WaveIndex++;
            var wave = Current.waves[WaveIndex];
            int index = 0;
            foreach (var kind in wave.enemies) Spawn(kind, index++, partySize);
            for (int i = 1; i < partySize; i++) { Spawn(EnemyKind.Cinderling, index++, partySize); Spawn(EnemyKind.Lantern, index++, partySize); }
            watching = true;
        }
        private void Spawn(EnemyKind kind, int index, int partySize)
        {
            Vector3 position = Current.spawnPoints[index % Current.spawnPoints.Length];
            position += Vector3.right * (index / Current.spawnPoints.Length) * .8f;
            var enemy = factory.Enemy(kind, position, partySize); enemy.ScaleHealth(Current.enemyHealthMultiplier); enemies.Add(enemy);
        }
        public void DebugSpawnElementalGroup(ElementTag element,int partySize)
        {
            EnemyKind[] kinds={EnemyKind.Cinderling,EnemyKind.Lantern,EnemyKind.Bulwark};int start=enemies.Count;
            for(int i=0;i<kinds.Length;i++){Vector3 position=Current.spawnPoints[(start+i)%Current.spawnPoints.Length];var enemy=factory.Enemy(kinds[i],position,partySize);enemy.ElementAffinity=element;if(enemy.View)enemy.View.SetElement(element);enemies.Add(enemy);}watching=true;
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
            if (watching && combat.Active && RemainingEnemies == 0) { watching = false; WaveCleared?.Invoke(); }
        }
        public void ClearTransientCombat()
        {
            foreach (var projectile in FindObjectsByType<CombatProjectile>()) Destroy(projectile.gameObject);
            foreach (var area in FindObjectsByType<AreaAttack>()) Destroy(area.gameObject);
            foreach (var actor in combat.Actors) if (actor) actor.Statuses.Clear();
        }
        public void ClearEnemies()
        {
            watching = false;
            foreach (var enemy in enemies) if (enemy) { combat.Unregister(enemy); enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            enemies.Clear(); Boss = null;
        }
        public void Clear() { watching = false; ClearTransientCombat(); ClearEnemies(); ExitOpen = false; }
    }
}
