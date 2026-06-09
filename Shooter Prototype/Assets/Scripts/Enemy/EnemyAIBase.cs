using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering; //Si no se coloca al poner el NavMesh colocar a mano

public class EnemyAIBase : MonoBehaviour
{
    #region General Variables
    [Header("AI Configuration")]
    [SerializeField] NavMeshAgent agent;    //Ref al cerebro NavMesh del objeto
    [SerializeField] Transform target; //Ref a la posición del targert a perseguir
    [SerializeField] LayerMask targetLayer; //Define la capa del target (detección)
    [SerializeField] LayerMask groundLayer; //Define la capa del suelo (Definir puntos navegables)

    [Header("Patrolig Stats")]
    [SerializeField] float walkPointRange = 8f; //Radio maximo de margen espacial para buscar punto
    Vector3 walkPoint; //Posición del punto a perseguir
    bool walkPointSet; //Si es falso, busca punto. Si es verdadero no puede buscar punto

    [Header("Attacking Stats")]
    [SerializeField] float timeBdetweenAttacks = 1f; //Tiempo entre ataque y ataque
    [SerializeField] GameObject projectile; //Ref al prefab del proyectil
    [SerializeField] Transform shootPoint; //Posición inicial del disparo
    [SerializeField] float shootSpeedY; //Potencia de disparo vertival (solo catapulta)
    [SerializeField] float shootSpeedZ = 10f; //Potencia de dispato hacia delante (Siempre está)
    bool alreadyAttacked; //Se pregunta si estamos atacando para no stackear ataques

    [Header("States & Detection Areas")]
    [SerializeField] float sightRange = 10f; //Radio de la detección de persecución
    [SerializeField] float attackRange = 4f; //Radio de la detección de ataque
    [SerializeField] bool targetInSightRange; //Determina se entra el estado PERSEGUIR
    [SerializeField] bool targetInAttackRange; //Determina se entra el estado ATACAR

    [Header("Stuck Detection")]
    [SerializeField] float stuckCheckTime = 2f; //Tiempo que el agente espera quieto antes de preguntarse si está stuck
    [SerializeField] float stuckThreshold = 0.1f; //Margen de detección de stuck
    [SerializeField] float maxStuckDuration = 3f; //Tiempo máximo de estar stuck

    float stuckTimer; //Reloj que cuenta el tiempo de estar stuck
    float lastCheckTime; //Tiempo de chequeo previo a estar stuck
    Vector3 lastPosition; //Posición del último walkpoint perseguido
    #endregion

    private void Awake()
    {
        target = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        lastPosition = transform.position;
        lastCheckTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        EnemyStateUpdater();
        CheckIfStuck();
    }

    void EnemyStateUpdater()
    {
        //Acción que se encarga de la gestión de los estados de la IA
        //Esfera de detección física
        Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, targetLayer);
        targetInSightRange = hits.Length > 0;
        //Si está persiguiendo, alcula la distancia hasta que el mínimo entre dentro del rango de ataque
        if (targetInSightRange)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            targetInAttackRange = distance <= attackRange;

        }

        //Lógica de los cambios de estado
        if (!targetInSightRange && !targetInAttackRange) Patroling();
        else if (targetInSightRange && !targetInAttackRange) ChaseTarget();
        else if (targetInSightRange && targetInAttackRange) AttackTarget();

    }

    void Patroling()
    {
        //Define que el objeto patrulle y genere puntos de patrulla random
        //1- Revisa si hay punto a patrullar
        if (!walkPointSet)
        {
            //Si no hay walkpoint, busca uno
            SearchWalkPoint();
        }
        else agent.SetDestination(walkPoint); //Si hay punto, lo persigue

        //2- Una vez ha llegado al punto, hay que decirle al sistema que puede generar uno nuevo
        if ((transform.position - walkPoint).sqrMagnitude < 1f)
        {
            walkPointSet = false;
        }
    }

    void SearchWalkPoint()
    {
        //Acción que busca un punto de patrulla random si no lo hay
        int attempts = 0; //Número interno de intentos de buscar punto nuevo
        const int maxAttempts = 5;

        while (!walkPointSet && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomPoint = transform.position + new Vector3(Random.Range(-walkPointRange, walkPointRange), 0, Random.Range(-walkPointRange, walkPointRange));

            //Chequear si el punto está en un lugar en el que haya NavMesh surface
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                walkPoint = hit.position; //Determina el Vector3 random a perseguir
                if (Physics.Raycast(walkPoint, -transform.up, 2f, groundLayer))
                {
                    walkPointSet = true; //Tenemos puunto y el agente va hacia él
                }
            }
        }
    }

    void ChaseTarget()
    {
        //Le dice al agente que persiga al target
        agent.SetDestination(target.position);
    }

    void AttackTarget()
    {
        //Acción que determina el ataque al objetivo

        //1- Detener el movimiento
        agent.SetDestination(transform.position);

        //2- Rotación suavizada para mirar al target
        Vector3 direction = (target.position - transform.position).normalized;
        //Condicional que revisa si agente y target NO se están mirando
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, agent.angularSpeed * Time.deltaTime);
        }

        //3- Definir el ataque en sí
        //Solo atacará si no está atacando
        if (!alreadyAttacked)
        {
            Rigidbody rb = Instantiate(projectile, shootPoint.position, Quaternion.identity).GetComponent<Rigidbody>();
            rb.AddForce(transform.forward * shootSpeedZ, ForceMode.Impulse);
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBdetweenAttacks);
        }
    }

    void ResetAttack()
    {
        //Acción dque resetea ataque
        alreadyAttacked = false;
    }

    void CheckIfStuck()
    {
        //Acción que revisa si el agente está atrapado
        if (Time.time - lastCheckTime > stuckCheckTime)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);

            if (distanceMoved < stuckThreshold && agent.hasPath)
            {
                stuckTimer += stuckCheckTime;
            }
            else
            {
                stuckTimer = 0f;
            }

            if (stuckTimer >= maxStuckDuration)
            {
                walkPointSet = false;
                agent.ResetPath();
                stuckTimer = 0;
            }

            lastPosition = transform.position;
            lastCheckTime = Time.time;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;//Solo se ejecutan en editor de Unity

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}