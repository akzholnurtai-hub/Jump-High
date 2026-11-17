using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Patroller: Patrol -> Chase -> Attack
// Tests POSITIONING: stay out of the cone, use walls as cover.
[RequireComponent(typeof(NavMeshAgent))]
public class PatrollerAI : EnemyBase
{
    enum State { Patrol, Chase, Attack }

    const float RepathTime = 0.2f;                        // s, new NavMesh path 5 times per second
    const float WaypointReach = 0.4f;                     // m, "arrived" at a waypoint

    [Header("Patroller")]
    [SerializeField] Transform[] points;                  // waypoints placed in a scene by hand (optional)

    PatrollerStats S => (PatrollerStats)stats;
    readonly List<Vector3> route = new List<Vector3>();   // waypoint positions used at run time

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
        if (stats == null) return;                        // EnemyBase already logged it
        agent = GetComponent<NavMeshAgent>();
        agent.speed = S.patrolSpeed;
        agent.angularSpeed = S.turnSpeed;
        agent.stoppingDistance = WaypointReach / 4f;

        if (points != null)
            foreach (Transform p in points)
                if (p != null) route.Add(p.position);

        SetState(State.Patrol);
    }

    // Spawned from LevelData: waypoints come from the data, not from scene objects.
    public override void Init(EnemyStats data, List<Vector3> waypoints)
    {
        base.Init(data, waypoints);
        if (stats == null) return;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (waypoints != null && waypoints.Count > 0)
        {
            route.Clear();
            route.AddRange(waypoints);
        }
        agent.angularSpeed = S.turnSpeed;
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
                GoTo(lastSeen, S.chaseSpeed);
                if (seesPlayer && DistanceToPlayer() < S.attackRange) SetState(State.Attack);
                else if (timer > S.loseTime) SetState(State.Patrol);
                break;

            case State.Attack:                                   // stands still: this is the warning
                if (timer < S.windUp) TurnTowards(player.position, S.aimTurnSpeed);
                else if (!struck)
                {
                    struck = true;
                    ResetTint();
                    if (DistanceToPlayer() <= S.hitRange) HitPlayer();   // stepping away during wind-up works
                }
                if (timer >= S.windUp + S.recovery) SetState(State.Chase);
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
        if (route.Count == 0) return;
        Vector3 target = route[i];
        GoTo(target, S.patrolSpeed);
        if (DistanceTo(target) < WaypointReach)
        {
            i = (i + 1) % route.Count;     // next point, loop to 0
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
            repath = RepathTime;
            agent.SetDestination(target);
        }
        else
        {
            TurnTowards(target, S.turnSpeed);
            MoveFlat(target - transform.position, speed * Time.deltaTime);
        }
    }

    void Stop()
    {
        if (agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
    }

    int NearestPoint()
    {
        if (route.Count == 0) return 0;
        int best = 0; float bd = Mathf.Infinity;
        for (int k = 0; k < route.Count; k++)
        {
            float d = DistanceTo(route[k]);
            if (d < bd) { bd = d; best = k; }
        }
        return best;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.white;
        for (int k = 0; k < route.Count; k++)
            Gizmos.DrawLine(route[k], route[(k + 1) % route.Count]);
    }
}
