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

    const float HomeReach = 0.2f;                         // m, "back home"
    const float MuzzleOffset = 0.8f;                      // m, if there is no firePoint

    ShooterStats S => (ShooterStats)stats;

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
                if (DistanceTo(home) > HomeReach)
                {
                    TurnTowards(home, S.turnSpeed);
                    MoveFlat(home - transform.position, S.returnSpeed * Time.deltaTime);
                }
                if (seesPlayer) SetState(State.KeepDistance);
                break;

            case State.KeepDistance:
                if (seesPlayer) TurnTowards(player.position, S.turnSpeed);
                KeepDistance();
                unseen = seesPlayer ? 0f : unseen + Time.deltaTime;
                if (unseen > S.loseTime) SetState(State.Idle);
                else if (seesPlayer && timer >= S.reactionTime) SetState(State.Aim);
                break;

            case State.Aim:
                if (timer < S.aimTime - S.aimLock) TurnTowards(player.position, S.aimTurnSpeed);
                UpdateLaser();
                if (timer >= S.aimTime) { Fire(); SetState(State.Cooldown); }
                break;

            case State.Cooldown:
                if (seesPlayer) TurnTowards(player.position, S.turnSpeed);
                KeepDistance();
                if (timer >= S.cooldown) SetState(State.KeepDistance);
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
        float step = S.moveSpeed * Time.deltaTime;

        if (d < S.preferredDistance - S.distanceSlack)
            MoveFlat(-toPlayer, step);                                     // too close: back off
        else if (d > S.preferredDistance + S.distanceSlack && DistanceTo(home) < S.leash)
            MoveFlat(toPlayer, step);                                      // too far: come closer
    }

    void UpdateLaser()
    {
        if (laser == null) return;
        Vector3 from = firePoint != null ? firePoint.position : transform.position + Vector3.up * EyeHeight;
        float len = S.viewRange;
        if (Physics.Raycast(from, transform.forward, out RaycastHit hit, S.viewRange, obstacleMask, QueryTriggerInteraction.Ignore))
            len = hit.distance;
        laser.SetPosition(0, from);
        laser.SetPosition(1, from + transform.forward * len);
    }

    void Fire()
    {
        if (shotPrefab == null) return;
        Vector3 from = firePoint != null ? firePoint.position : transform.position + Vector3.up * EyeHeight + transform.forward * MuzzleOffset;
        GameObject shot = Instantiate(shotPrefab, from, Quaternion.LookRotation(transform.forward));

        Fireball fb = shot.GetComponent<Fireball>();
        if (fb != null) fb.speed = S.shotSpeed;         // set before its Start() runs

        ShotStopper stop = shot.AddComponent<ShotStopper>();
        stop.walls = obstacleMask;                      // the shot does not fly through walls
    }
}
