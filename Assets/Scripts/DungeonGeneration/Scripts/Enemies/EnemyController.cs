using UnityEngine;
using DungeonGeneration.Scripts.Health;
using UnityEngine.AI;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DungeonGeneration.Scripts.Enemies
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Настройки навигации")]
        [SerializeField] private float pathUpdateInterval = 0.5f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float minPathUpdateDistance = 1f; // Минимальное расстояние для обновления пути
        
        [Header("Настройки боя")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float aggroRange = 10f;

        [Header("Оптимизация")]
        [SerializeField] private float updateIntervalOutsideView = 1f; // Интервал обновления вне поля зрения
        [SerializeField] private float maxPathUpdateRange = 30f; // Максимальная дистанция для обновления пути
        
        [Header("Отладка")]
        [SerializeField] private bool showDebugLogs = true;

        private NavMeshAgent agent;
        private GameObject player;
        private bool isInitialized = false;
        private float lastPathUpdateTime;
        private float lastAttackTime;
        private EnemyState currentState = EnemyState.Idle;
        private Vector3 startPosition;
        private float nextPlayerSearchTime = 0f;
        private const float PLAYER_SEARCH_INTERVAL = 0.5f;
        private Vector3 lastTargetPosition;
        private Camera mainCamera;
        private bool isInViewport;
        private Renderer enemyRenderer;
        private Animator animator;
        private static readonly int IsRunning = Animator.StringToHash("isRunning");
        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int IsHit = Animator.StringToHash("isHit");
        private static readonly int IsDead = Animator.StringToHash("isDead");
        
        private enum EnemyState
        {
            Idle,
            Chasing,
            Attacking,
            Returning
        }

        private void Start()
        {
            DungeonGenerator.OnPlayerCreated += HandlePlayerCreated;
            InitializeAgent();
            startPosition = transform.position;
            lastTargetPosition = startPosition;
            mainCamera = Camera.main;
            enemyRenderer = GetComponentInChildren<Renderer>();
            animator = GetComponentInChildren<Animator>();
            DebugLog($"Враг создан на позиции: {startPosition}");
            
            FindPlayer();
        }

        private void InitializeAgent()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError($"[EnemyController] NavMeshAgent не найден на {gameObject.name}");
                return;
            }

            // Оптимизация настроек NavMeshAgent
            agent.acceleration = 12f; // Быстрее достигает целевой скорости
            agent.angularSpeed = 360f; // Быстрее поворачивается
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Менее точное, но более быстрое избегание препятствий
            
            DebugLog($"Параметры NavMeshAgent:" +
                    $"\n - Speed: {agent.speed}" +
                    $"\n - Acceleration: {agent.acceleration}" +
                    $"\n - Angular Speed: {agent.angularSpeed}" +
                    $"\n - Obstacle Avoidance: {agent.obstacleAvoidanceType}");

            StartCoroutine(WaitForNavMesh());
        }

        private void FindPlayer()
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    DebugLog($"Игрок найден на позиции: {player.transform.position}");
                    isInitialized = agent != null && agent.isOnNavMesh;
                }
            }
        }

        private IEnumerator WaitForNavMesh()
        {
            DebugLog("Ожидание инициализации NavMesh...");
            int attempts = 0;
            while (!agent.isOnNavMesh && attempts < 50)
            {
                attempts++;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    DebugLog($"Найдена позиция на NavMesh: {hit.position}");
                    break;
                }
                DebugLog($"Попытка {attempts}: Не удалось найти позицию на NavMesh");
                yield return new WaitForSeconds(0.1f);
            }
            
            if (agent.isOnNavMesh)
            {
                isInitialized = player != null;
                DebugLog("NavMeshAgent успешно инициализирован");
            }
            else
            {
                Debug.LogError($"[EnemyController] {gameObject.name}: Не удалось разместить на NavMesh после {attempts} попыток");
            }
        }

        private void HandlePlayerCreated(GameObject playerObject)
        {
            player = playerObject;
            DebugLog($"Игрок обнаружен через событие на позиции: {player.transform.position}");
            isInitialized = agent != null && agent.isOnNavMesh;
        }

        private void OnDestroy()
        {
            DungeonGenerator.OnPlayerCreated -= HandlePlayerCreated;
        }

        private void Update()
        {
            // Периодически пытаемся найти игрока, если он не найден
            if (player == null && Time.time >= nextPlayerSearchTime)
            {
                FindPlayer();
                nextPlayerSearchTime = Time.time + PLAYER_SEARCH_INTERVAL;
                return;
            }

            if (!isInitialized || player == null || !agent.isOnNavMesh)
            {
                if (!isInitialized) DebugLog("Ожидание инициализации...");
                if (player == null) DebugLog("Игрок не найден");
                if (!agent.isOnNavMesh) DebugLog("Агент не на NavMesh");
                return;
            }

            UpdateEnemyState();
            HandleCurrentState();
        }

        private void UpdateEnemyState()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            bool hasLineOfSight = HasLineOfSightToPlayer();
            EnemyState previousState = currentState;

            if (distanceToPlayer <= attackRange && hasLineOfSight)
            {
                currentState = EnemyState.Attacking;
            }
            else if (distanceToPlayer <= aggroRange && hasLineOfSight)
            {
                currentState = EnemyState.Chasing;
            }
            else if (currentState == EnemyState.Chasing && distanceToPlayer > aggroRange)
            {
                currentState = EnemyState.Returning;
            }
            else if (currentState == EnemyState.Returning && 
                     Vector3.Distance(transform.position, startPosition) < 0.5f)
            {
                currentState = EnemyState.Idle;
            }

            if (previousState != currentState)
            {
                DebugLog($"Смена состояния: {previousState} -> {currentState}" +
                        $"\n - Дистанция до игрока: {distanceToPlayer:F2}" +
                        $"\n - Видимость игрока: {hasLineOfSight}" +
                        $"\n - Позиция врага: {transform.position}" +
                        $"\n - Позиция игрока: {player.transform.position}");
            }
        }

        private void HandleCurrentState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    HandleIdleState();
                    break;
                case EnemyState.Chasing:
                    HandleChasingState();
                    break;
                case EnemyState.Attacking:
                    HandleAttackingState();
                    break;
                case EnemyState.Returning:
                    HandleReturningState();
                    break;
            }
        }

        private void HandleIdleState()
        {
            Debug.Log($"[EnemyController] {gameObject.name}: HandleIdleState");
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
                Debug.Log($"[EnemyController] {gameObject.name}: NavMeshAgent остановлен и путь сброшен");
            }
            if (animator != null)
            {
                animator.SetBool(IsRunning, false);
                Debug.Log($"[Animator][{gameObject.name}] SetBool isRunning = false (Idle)");
            }
        }

        private bool ShouldUpdatePath()
        {
            if (player == null) return false;

            // Проверяем, находится ли враг в поле зрения камеры
            if (enemyRenderer != null)
            {
                isInViewport = GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(mainCamera), 
                                                            enemyRenderer.bounds);
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            // Если враг слишком далеко, не обновляем путь
            if (distanceToPlayer > maxPathUpdateRange) return false;

            // Проверяем, достаточно ли сместился игрок для обновления пути
            float targetMovement = Vector3.Distance(player.transform.position, lastTargetPosition);
           // Используем разные интервалы обновления в зависимости от видимости
            float currentUpdateInterval = isInViewport ? pathUpdateInterval : updateIntervalOutsideView;
            return Time.time - lastPathUpdateTime >= currentUpdateInterval && 
                   (targetMovement > minPathUpdateDistance || currentState == EnemyState.Returning);
        }

        private void HandleChasingState()
        {
            Debug.Log($"[EnemyController] {gameObject.name}: HandleChasingState");
            if (!ShouldUpdatePath()) return;

            agent.isStopped = false;
            Vector3 targetPos = player.transform.position;
            agent.SetDestination(targetPos);
            // Debug.Log($"[EnemyController] {gameObject.name}: NavMeshAgent движется к {targetPos}");
            lastTargetPosition = targetPos;
            lastPathUpdateTime = Time.time;
            if (animator != null)
            {
                animator.SetBool(IsRunning, true);
                Debug.Log($"[Animator][{gameObject.name}] SetBool isRunning = true (Chasing)");
            }
        }

        private void HandleAttackingState()
        {
            Debug.Log($"[EnemyController] {gameObject.name}: HandleAttackingState");
            if (!agent.isStopped)
            {
                agent.isStopped = true;
                agent.ResetPath();
                Debug.Log($"[EnemyController] {gameObject.name}: NavMeshAgent остановлен для атаки");
            }
            RotateTowardsPlayer();
            if (animator != null)
            {
                animator.SetBool(IsRunning, false);
                Debug.Log($"[Animator][{gameObject.name}] SetBool isRunning = false (Attacking)");
                animator.SetTrigger(IsAttacking);
                Debug.Log($"[Animator][{gameObject.name}] SetTrigger isAttacking (Attacking)");
            }
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                AttackPlayer();
                lastAttackTime = Time.time;
            }
        }

        private void HandleReturningState()
        {
            Debug.Log($"[EnemyController] {gameObject.name}: HandleReturningState");
            if (Time.time - lastPathUpdateTime >= updateIntervalOutsideView)
            {
                agent.isStopped = false;
                agent.SetDestination(startPosition);
                Debug.Log($"[EnemyController] {gameObject.name}: NavMeshAgent возвращается к стартовой позиции {startPosition}");
                lastPathUpdateTime = Time.time;
            }
            if (animator != null)
            {
                animator.SetBool(IsRunning, true);
                Debug.Log($"[Animator][{gameObject.name}] SetBool isRunning = true (Returning)");
            }
        }

        private void RotateTowardsPlayer()
        {
            if (player == null) return;
            
            Vector3 direction = (player.transform.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
        }

        private bool HasLineOfSightToPlayer()
        {
            if (player == null) return false;

            Vector3 directionToPlayer = player.transform.position - transform.position;
            RaycastHit hit;
            bool hasLineOfSight = Physics.Raycast(transform.position, directionToPlayer, out hit, aggroRange) 
                                && hit.collider.gameObject == player;
            
            return hasLineOfSight;
        }

        private void AttackPlayer()
        {
            if (player == null) return;
            
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }

        private void DebugLog(string message, bool forceShow = false)
        {
            if ((showDebugLogs || forceShow) && Debug.isDebugBuild)
            {
                Debug.Log($"[{gameObject.name}] {message}");
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Радиус атаки
            Handles.color = Color.red;
            Handles.DrawWireDisc(transform.position, Vector3.up, attackRange);
            
            // Радиус агро
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(transform.position, Vector3.up, aggroRange);
            
            // Путь
            if (agent != null && agent.hasPath)
            {
                Handles.color = Color.cyan;
                var path = agent.path;
                Vector3 previousCorner = transform.position;
                foreach (Vector3 corner in path.corners)
                {
                    Handles.DrawLine(previousCorner, corner);
                    previousCorner = corner;
                }
            }

            // Текущее состояние
            if (Application.isPlaying)
            {
                Vector3 textPosition = transform.position + Vector3.up * 2f;
                Handles.Label(textPosition, $"State: {currentState}");
            }
        }
        #endif

        // Публичные методы для анимаций получения урона и смерти
        public void PlayHitAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(IsHit);
                Debug.Log($"[Animator][{gameObject.name}] SetTrigger isHit (PlayHitAnimation)");
            }
        }

        public void PlayDeathAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(IsDead);
                Debug.Log($"[Animator][{gameObject.name}] SetTrigger isDead (PlayDeathAnimation)");
            }
        }
    }
} 