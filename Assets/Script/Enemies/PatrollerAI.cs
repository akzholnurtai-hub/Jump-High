using UnityEngine;
using UnityEngine.AI;

// Patroller: Patrol -> Chase -> Attack
// Tests POSITIONING: stay out of the cone, use walls as cover.
[RequireComponent(typeof(NavMeshAgent))]
public class PatrollerAI : EnemyBase
{
    enum State { Patrol, Chase, Attack }

    [Header("Patroller")]
    [SerializeField] Transform[] points;                  // waypoints in the level, NOT children of the enemy
    [SerializeField] float patrolSpeed = 1.7f;            // m/s
    [SerializeField] float chaseSpeed = 4f;               // m/s (player runs 6 m/s)
    [SerializeField] float attackRange = 1.6f;            // m, starts the wind-up
    [SerializeField] float hitRange = 2.2f;               // m, the strike only lands if still this close
    [SerializeField] float windUp = 0.5f;                 // s, red warning before the hit
    [SerializeField] float recovery = 0.6f;               // s, stands still after the hit: hit it back
    [SerializeField] float loseTime = 2f;                 // s without seeing the player -> back to Patrol

    State state;
    float timer;          // seconds in the current state (in Chase: since last seen)
    int i;
    bool struck;
    float repath;
    Vector3 lastSeen;
    NavMeshAgent agent;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.1f;
        SetState(State.Patrol);
    }

    void Update()
    {
        timer += Time.deltaTime;
        UpdateSight();

        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (seesPlayer) SetState(State.Chase);
                break;

            case State.Chase:
                if (seesPlayer) { timer = 0f; lastSeen = player.position; }
                GoTo(lastSeen, chaseSpeed);
                if (seesPlayer && DistanceToPlayer() < attackRange) SetState(State.Attack);
                else if (timer > loseTime) SetState(State.Patrol);
                break;

            case State.Attack:                                   // stands still: this is the warning
                if (timer < windUp) TurnTowards(player.position, 360f);
                else if (!struck)
                {
                    struck = true;
                    ResetTint();
                    if (DistanceToPlayer() <= hitRange) HitPlayer();   // stepping away during wind-up works
                }
                if (timer >= windUp + recovery) SetState(State.Chase);
                break;
        }
    }

    // the ONLY place where the state changes
    void SetState(State next)
    {
        state = next;
        timer = 0f;
        struck = false;
        repath = 0f;
        ShowState(next.ToString());

        SetWarning(next == State.Attack);
        if (next == State.Attack) { Stop(); PlayWarn(); }
        if (next == State.Chase && player != null) lastSeen = player.position;
        if (next == State.Patrol) i = NearestPoint();
    }

    void Patrol()
    {
        if (points == null || points.Length == 0) return;
        Vector3 target = points[i].position;
        GoTo(target, patrolSpeed);
        if (DistanceTo(target) < 0.4f)
        {
            i = (i + 1) % points.Length;   // next point, loop to 0
            repath = 0f;
        }
    }

    // NavMesh if it is baked, otherwise a straight walk that stops at walls
    void GoTo(Vector3 target, float speed)
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            repath -= Time.deltaTime;
            if (repath > 0f) return;
            repath = 0.2f;                                  // new path 5 times per second
            agent.SetDestination(target);
        }
        else
        {
            TurnTowards(target, 360f);
            MoveFlat(target - transform.position, speed * Time.deltaTime);
        }
    }

    void Stop()
    {
        if (agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
    }

    int NearestPoint()
    {
        if (points == null || points.Length == 0) return 0;
        int best = 0; float bd = Mathf.Infinity;
        for (int k = 0; k < points.Length; k++)
        {
            float d = DistanceTo(points[k].position);
            if (d < bd) { bd = d; best = k; }
        }
        return best;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (points == null) return;
        Gizmos.color = Color.white;
        for (int k = 0; k < points.Length; k++)
            if (points[k] != null && points[(k + 1) % points.Length] != null)
                Gizmos.DrawLine(points[k].position, points[(k + 1) % points.Length].position);
    }
}
