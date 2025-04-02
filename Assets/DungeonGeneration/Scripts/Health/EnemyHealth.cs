using UnityEngine;
using DungeonGeneration.Scripts.Health;

namespace DungeonGeneration.Scripts.Health
{
    public class EnemyHealth : Health
    {
        [SerializeField] private bool isBoss = false;

        protected override void Die()
        {
            base.Die();
            Destroy(gameObject);
        }
    }
} 