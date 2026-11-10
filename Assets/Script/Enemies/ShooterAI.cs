using UnityEngine;

// Shooter: Idle -> KeepDistance -> Aim -> Cooldown
// Tests REACTION: step aside when the red laser appears.
public class ShooterAI : EnemyBase
{
    enum State { Idle, KeepDistance, Aim, Cooldown }

    [Header("Shooter")]
    [SerializeField] GameObject shotPrefab;               // Fireball prefab (a Hazard)
    [SerializeField] Transform firePoint;
    [SerializeField] LineRenderer laser;                  // red aim line = the warning
    [SerializeField] float preferredDistance = 5f;        // m
    [SerializeField] float moveSpeed = 2.2f;              // m/s
    [SerializeField] float leash = 5f;                    // m, never walks further than this from home
    [SerializeField] float aimTime = 0.6f;                // s of laser before the shot
    [SerializeField] float aimLock = 0.2f;                // s, last part of the aim does not turn: dodge works
    [SerializeField] float shotSpeed = 7f;                // m/s (player runs 6 m/s)
    [SerializeField] float cooldown = 1.5f;               // s after a shot: time to leave cover
    [SerializeField] float loseTime = 1f;                 // s unseen -> Idle

    State state;
    float timer;
    float unseen;

    protected override void Awake()
    {
        base.Awake();
        if (laser != null) laser.enabled = false;
        SetState(State.Idle);
    }

    void Update()
    {
        timer += Time.deltaTime;
        UpdateSight();

        switch (state)
        {
            case State.Idle:
                if (DistanceTo(home) > 0.2f)
                {
                    TurnTowards(home, 240f);
                    MoveFlat(home - transform.position, 1.5f * Time.deltaTime);
                }
                if (seesPlayer) SetState(State.KeepDistance);
                break;

            case State.KeepDistance:
                if (seesPlayer) TurnTowards(player.position, 300f);
                KeepDistance();
                unseen = seesPlayer ? 0f : unseen + Time.deltaTime;
                if (unseen > loseTime) SetState(State.Idle);
                else if (seesPlayer && timer >= 0.25f) SetState(State.Aim);
                break;

            case State.Aim:
                if (timer < aimTime - aimLock) TurnTowards(player.position, 360f);
                UpdateLaser();
                if (timer >= aimTime) { Fire(); SetState(State.Cooldown); }
                break;

            case State.Cooldown:
                if (seesPlayer) TurnTowards(player.position, 300f);
                KeepDistance();
                if (timer >= cooldown) SetState(State.KeepDistance);
                break;
        }
    }

    void SetState(State next)
    {
        state = next;
        timer = 0f;
        unseen = 0f;
        ShowState(next.ToString());

        bool aiming = next == State.Aim;
        SetWarning(aiming);
        if (laser != null) laser.enabled = aiming;
        if (aiming) PlayWarn();
    }

    void KeepDistance()
    {
        if (!seesPlayer) return;
        float d = DistanceToPlayer();
        Vector3 toPlayer = player.position - transform.position;
        float step = moveSpeed * Time.deltaTime;

        if (d < preferredDistance - 0.5f)
            MoveFlat(-toPlayer, step);                                     // too close: back off
        else if (d > preferredDistance + 0.5f && DistanceTo(home) < leash)
            MoveFlat(toPlayer, step);                                      // too far: come closer
    }

    void UpdateLaser()
    {
        if (laser == null) return;
        Vector3 from = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        float len = viewRange;
        if (Physics.Raycast(from, transform.forward, out RaycastHit hit, viewRange, obstacleMask, QueryTriggerInteraction.Ignore))
            len = hit.distance;
        laser.SetPosition(0, from);
        laser.SetPosition(1, from + transform.forward * len);
    }

    void Fire()
    {
        if (shotPrefab == null) return;
        Vector3 from = firePoint != null ? firePoint.position : transform.position + Vector3.up + transform.forward * 0.8f;
        GameObject shot = Instantiate(shotPrefab, from, Quaternion.LookRotation(transform.forward));

        Fireball fb = shot.GetComponent<Fireball>();
        if (fb != null) fb.speed = shotSpeed;           // set before its Start() runs

        ShotStopper stop = shot.AddComponent<ShotStopper>();
        stop.walls = obstacleMask;                      // the shot does not fly through walls
    }
}
