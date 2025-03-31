using UnityEngine;

namespace DungeonGeneration
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float chaseDistance = 10f;
        [SerializeField] private float attackDistance = 2f;
        [SerializeField] private float stopDistance = 1f;

        [Header("Combat Settings")]
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float attackSpeed = 1f;

        [Header("Visualization")]
        [SerializeField] private Color chaseGizmoColor = new Color(1f, 0f, 0f, 0.2f);
        [SerializeField] private Color attackGizmoColor = new Color(1f, 0.5f, 0f, 0.2f);

        private Transform player;
        private Health health;
        private float lastAttackTime;
        private bool isChasing;
        private bool isAttacking;

        private void Start()
        {
            health = GetComponent<Health>();
            if (health == null)
            {
                Debug.LogError("Health component not found on enemy!");
                return;
            }
        }

        private void Update()
        {
            // Ищем игрока в каждом кадре
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (player == null)
                {
                    return; // Если игрок не найден, пропускаем обновление
                }
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Определяем состояние врага
            isChasing = distanceToPlayer <= chaseDistance;
            isAttacking = distanceToPlayer <= attackDistance;

            if (isChasing)
            {
                ChasePlayer();
            }

            if (isAttacking && Time.time >= lastAttackTime + attackSpeed)
            {
                AttackPlayer();
                lastAttackTime = Time.time;
            }
        }

        private void ChasePlayer()
        {
            // Поворачиваем врага к игроку
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Двигаем врага к игроку
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer > stopDistance)
            {
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
            }
        }

        private void AttackPlayer()
        {
            // Наносим урон игроку
            var playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Визуализация дистанции преследования
            Gizmos.color = chaseGizmoColor;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);

            // Визуализация дистанции атаки
            Gizmos.color = attackGizmoColor;
            Gizmos.DrawWireSphere(transform.position, attackDistance);
        }
    }
} 