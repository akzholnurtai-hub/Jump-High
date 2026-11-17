using UnityEngine;

// Charger: Idle -> WindUp -> Dash -> Stunned
// Tests TIMING: sidestep at the end of the wind-up, punish the stun.
public class ChargerAI : EnemyBase
{
    enum State { Idle, WindUp, Dash, Stunned }

    [Header("Charger")]
    [SerializeField] LineRenderer lane;                   // red line on the floor = where the dash goes
    [SerializeField] Color stunColor = new Color(0.3f, 0.6f, 1f);

    const float HomeReach = 0.3f;                         // m, "back home"
    const float LaneLift = 0.05f;                         // m, lane drawn just above the floor

    ChargerStats S => (ChargerStats)stats;

    State state;
    float timer;
    float traveled;
    bool hitDone;
    Vector3 dashDir;

    protected override void Awake()
    {
        base.Awake();
        if (lane != null) lane.enabled = false;
        SetState(State.Idle);
    }

    void Update()
    {
        timer += Time.deltaTime;
        UpdateSight();

        switch (state)
        {
            case State.Idle:
                float h = DistanceTo(home);
                if (timer > S.restTime && h < S.homeRadius && seesPlayer) SetState(State.WindUp);
                else if (h > HomeReach)
                {
                    TurnTowards(home, S.turnSpeed);
                    MoveFlat(home - transform.position, S.returnSpeed * Time.deltaTime);
                }
                break;

            case State.WindUp:
                if (timer < S.windUp - S.aimLock) TurnTowards(player.position, S.aimTurnSpeed);
                UpdateLane();
                if (timer >= S.windUp) { dashDir = transform.forward; SetState(State.Dash); }
                break;

            case State.Dash:
                float step = S.dashSpeed * Time.deltaTime;
                bool moved = MoveFlat(dashDir, step);
                traveled += step;
                if (!hitDone && DistanceToPlayer() < S.hitRadius) { hitDone = true; HitPlayer(); }
                if (!moved || traveled >= S.dashDistance) SetState(State.Stunned);   // wall or end of dash
                break;

            case State.Stunned:
                if (timer >= S.stunTime) SetState(State.Idle);
                break;
        }
    }

    void SetState(State next)
    {
        state = next;
        timer = 0f;
        traveled = 0f;
        hitDone = false;
        ShowState(next.ToString());

        if (lane != null) lane.enabled = next == State.WindUp;

        if (next == State.WindUp) { SetWarning(true); PlayWarn(); }
        else if (next == State.Dash) SetWarning(true);
        else if (next == State.Stunned) SetTint(stunColor);
        else ResetTint();
    }

    void UpdateLane()
    {
        if (lane == null) return;
        Vector3 from = transform.position + Vector3.up * LaneLift;
        float len = S.dashDistance;
        if (Physics.Raycast(transform.position + Vector3.up * EyeHeight, transform.forward, out RaycastHit hit, S.dashDistance,
                            obstacleMask, QueryTriggerInteraction.Ignore))
            len = hit.distance;
        lane.SetPosition(0, from);
        lane.SetPosition(1, from + transform.forward * len);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (!(stats is ChargerStats)) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, S.homeRadius);
    }
}
