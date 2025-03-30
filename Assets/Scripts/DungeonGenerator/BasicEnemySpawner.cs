using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    /// <summary>
    /// Basic implementation of IEnemySpawner for spawning enemies in dungeon rooms
    /// </summary>
    public class BasicEnemySpawner : MonoBehaviour, IEnemySpawner
    {
        [Tooltip("Enemy prefabs that can be spawned by this spawner")]
        [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
        
        [Tooltip("Spawn points for the enemies (if empty, will spawn at the spawner's position)")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        
        [Tooltip("Number of enemies to spawn")]
        [SerializeField] private int enemiesToSpawn = 1;
        
        [Tooltip("Whether to randomize the enemy selection")]
        [SerializeField] private bool randomizeEnemies = true;
        
        [Tooltip("Whether to randomize spawn points")]
        [SerializeField] private bool randomizeSpawnPoints = true;
        
        [Tooltip("Radius around spawn points to randomize position (0 for exact position)")]
        [SerializeField] private float spawnRadius = 1.0f;
        
        /// <summary>
        /// Spawn enemies according to the spawner's configuration
        /// </summary>
        /// <returns>Array of spawned enemy GameObjects</returns>
        public GameObject[] SpawnEnemies()
        {
            if (enemyPrefabs.Count == 0)
            {
                Debug.LogWarning("No enemy prefabs assigned to spawner!", this);
                return new GameObject[0];
            }
            
            List<GameObject> spawnedEnemies = new List<GameObject>();
            
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                // Get enemy prefab to spawn
                GameObject enemyPrefab = GetEnemyPrefab();
                if (enemyPrefab == null) continue;
                
                // Get spawn position
                Vector3 spawnPosition = GetSpawnPosition(i);
                
                // Spawn the enemy
                GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform.parent);
                enemy.name = $"{enemyPrefab.name}_{i}";
                
                spawnedEnemies.Add(enemy);
            }
            
            return spawnedEnemies.ToArray();
        }
        
        /// <summary>
        /// Get an enemy prefab to spawn
        /// </summary>
        private GameObject GetEnemyPrefab()
        {
            if (enemyPrefabs.Count == 0) return null;
            
            if (randomizeEnemies)
            {
                // Get random enemy prefab
                int index = Random.Range(0, enemyPrefabs.Count);
                return enemyPrefabs[index];
            }
            else
            {
                // Get first enemy prefab
                return enemyPrefabs[0];
            }
        }
        
        /// <summary>
        /// Get a position to spawn an enemy
        /// </summary>
        private Vector3 GetSpawnPosition(int enemyIndex)
        {
            // If no spawn points, use spawner position
            if (spawnPoints.Count == 0)
            {
                return transform.position + Random.insideUnitSphere * spawnRadius;
            }
            
            Transform spawnPoint;
            
            if (randomizeSpawnPoints)
            {
                // Get random spawn point
                int index = Random.Range(0, spawnPoints.Count);
                spawnPoint = spawnPoints[index];
            }
            else
            {
                // Get spawn point based on enemy index (cycling through if needed)
                int index = enemyIndex % spawnPoints.Count;
                spawnPoint = spawnPoints[index];
            }
            
            // Return spawn position with optional randomization
            if (spawnRadius > 0)
            {
                Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
                randomOffset.y = 0; // Keep on same height plane
                return spawnPoint.position + randomOffset;
            }
            else
            {
                return spawnPoint.position;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius gizmos
            Gizmos.color = Color.red;
            
            if (spawnPoints.Count == 0)
            {
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
            else
            {
                foreach (Transform spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
                    }
                }
            }
        }
    }
} 