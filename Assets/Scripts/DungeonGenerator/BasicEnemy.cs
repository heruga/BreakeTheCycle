using System;
using UnityEngine;

namespace DungeonGenerator
{
    /// <summary>
    /// Basic implementation of IEnemy for enemies in dungeon rooms
    /// </summary>
    public class BasicEnemy : MonoBehaviour, IEnemy
    {
        [Tooltip("Health points of this enemy")]
        [SerializeField] private float health = 100f;
        
        [Tooltip("Whether to destroy this game object on death")]
        [SerializeField] private bool destroyOnDeath = true;
        
        [Tooltip("Delay before destroying the game object on death")]
        [SerializeField] private float destroyDelay = 2f;
        
        public event Action<GameObject> OnEnemyDeath;
        
        /// <summary>
        /// Apply damage to this enemy
        /// </summary>
        public void ApplyDamage(float damage)
        {
            health -= damage;
            
            // Check if enemy is dead
            if (health <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Kill this enemy
        /// </summary>
        public void Die()
        {
            // Trigger death event
            OnEnemyDeath?.Invoke(gameObject);
            
            if (destroyOnDeath)
            {
                // Disable components
                Collider[] colliders = GetComponentsInChildren<Collider>();
                foreach (Collider collider in colliders)
                {
                    collider.enabled = false;
                }
                
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
                
                // Destroy after delay
                Destroy(gameObject, destroyDelay);
            }
        }
        
        // For testing in the editor
        [ContextMenu("Kill Enemy")]
        private void KillInEditor()
        {
            Die();
        }
    }
} 