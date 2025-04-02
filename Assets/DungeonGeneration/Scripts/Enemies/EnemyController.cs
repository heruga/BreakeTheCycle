using UnityEngine;
using DungeonGeneration.Scripts.Health;
using UnityEngine.AI;
using System.Collections;

namespace DungeonGeneration.Scripts.Enemies
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float updateTargetInterval = 0.5f;

        private NavMeshAgent agent;
        private GameObject player;
        private bool isInitialized = false;
        private float lastUpdateTime;
        private bool isAttacking = false;
        private DungeonGenerator dungeonGenerator;

        private void Start()
        {
            // Подписываемся на событие создания игрока
            DungeonGenerator.OnPlayerCreated += HandlePlayerCreated;
            
            // Получаем компонент NavMeshAgent
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError($"[EnemyController] NavMeshAgent не найден на {gameObject.name}");
                return;
            }

            // Изначально деактивируем агента
            agent.enabled = false;

            // Получаем ссылку на DungeonGenerator
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogError("[EnemyController] DungeonGenerator не найден!");
            }
        }

        private void OnDestroy()
        {
            // Отписываемся от события при уничтожении
            DungeonGenerator.OnPlayerCreated -= HandlePlayerCreated;
        }

        private void HandlePlayerCreated(GameObject playerObject)
        {
            player = playerObject;
            StartCoroutine(WaitForNavMesh());
        }

        private IEnumerator WaitForNavMesh()
        {
            // Ждем пока NavMesh будет готов
            while (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Активируем агента
            agent.enabled = true;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || player == null) return;

            // Проверяем дистанцию до игрока
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            // Обновляем цель только если прошло достаточно времени
            if (Time.time - lastUpdateTime >= updateTargetInterval)
            {
                if (distanceToPlayer <= attackRange)
                {
                    // Если в диапазоне атаки, останавливаемся
                    agent.isStopped = true;
                    isAttacking = true;
                }
                else
                {
                    // Если вне диапазона атаки, преследуем
                    agent.isStopped = false;
                    agent.SetDestination(player.transform.position);
                    isAttacking = false;
                }
                lastUpdateTime = Time.time;
            }

            // Если атакуем, наносим урон
            if (isAttacking)
            {
                // TODO: Реализовать нанесение урона
                Debug.Log($"[EnemyController] {gameObject.name} атакует игрока");
            }
        }

        private void AttackPlayer()
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        // Визуализация в редакторе Unity
        private void OnDrawGizmosSelected()
        {
            // Отображаем радиус атаки
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
} 