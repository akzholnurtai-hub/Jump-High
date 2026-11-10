using UnityEngine;

// Charger: Idle -> WindUp -> Dash -> Stunned
// Tests TIMING: sidestep at the end of the wind-up, punish the stun.
public class ChargerAI : EnemyBase
{
    enum State { Idle, WindUp, Dash, Stunned }

    [Header("Charger")]
    [SerializeField] LineRenderer lane;                   // red line on the floor = where the dash goes
    [SerializeField] float windUp = 0.7f;                 // s
    [SerializeField] float aimLock = 0.15f;               // s, stops turning at the end of the wind-up
    [SerializeField] float dashSpeed = 14f;               // m/s
    [SerializeField] float dashDistance = 9f;             // m
    [SerializeField] float hitRadius = 1.1f;              // m
    [SerializeField] float stunTime = 1f;                 // s, free hits for the player
    [SerializeField] float homeRadius = 3f;               // m, charges only when near its corner
    [SerializeField] float returnSpeed = 1.8f;            // m/s
    [SerializeField] Color stunColor = new Color(0.3f, 0.6f, 1f);

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
                if (timer > 0.3f && h < homeRadius && seesPlayer) SetState(State.WindUp);
                else if (h > 0.3f)
                {
                    TurnTowards(home, 200f);
                    MoveFlat(home - transform.position, returnSpeed * Time.deltaTime);
                }
                break;

            case State.WindUp:
                if (timer < windUp - aimLock) TurnTowards(player.position, 480f);
                UpdateLane();
                if (timer >= windUp) { dashDir = transform.forward; SetState(State.Dash); }
                break;

            case State.Dash:
                float step = dashSpeed * Time.deltaTime;
                bool moved = MoveFlat(dashDir, step);
                traveled += step;
                if (!hitDone && DistanceToPlayer() < hitRadius) { hitDone = true; HitPlayer(); }
                if (!moved || traveled >= dashDistance) SetState(State.Stunned);   // wall or end of dash
                break;

            case State.Stunned:
                if (timer >= stunTime) SetState(State.Idle);
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
        Vector3 from = transform.position + Vector3.up * 0.05f;
        float len = dashDistance;
        if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit, dashDistance,
                            obstacleMask, QueryTriggerInteraction.Ignore))
            len = hit.distance;
        lane.SetPosition(0, from);
        lane.SetPosition(1, from + transform.forward * len);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(Application.isPlaying ? home : transform.position, homeRadius);
    }
}
