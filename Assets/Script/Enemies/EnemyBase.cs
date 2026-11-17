using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Shared "Sense" part for all three enemies: sight (distance -> angle -> raycast),
// wall-aware movement, warning color, state label and gizmos.
// Every enemy keeps its OWN enum + switch + SetState (the "Decide" part).
// All balance numbers (HP, damage, sight, speeds, timings) are in an EnemyStats asset.
public abstract class EnemyBase : MonoBehaviour
{
    // shape of the robot body, not balance
    protected const float EyeHeight = 1f;                 // m above the feet
    const float BodyRadius = 0.4f;                        // m, for the wall check when moving
    const float WallGap = 0.1f;                           // m kept from walls

    [Header("Data")]
    [SerializeField] protected EnemyStats stats;          // PatrollerStats / ShooterStats / ChargerStats asset

    [Header("Sight")]
    [SerializeField] protected LayerMask obstacleMask;    // tick ONLY the Walls layer

    [Header("Look & debug")]
    [SerializeField] protected Renderer body;             // single mesh that changes color (if no model)
    [SerializeField] protected Transform model;           // robot model: ALL its meshes change color
    [SerializeField] protected Color warnColor = new Color(1f, 0.15f, 0.1f);
    [SerializeField] protected TMP_Text stateLabel;       // 3D TextMeshPro above the head
    [SerializeField] protected AudioClip warnClip;        // sound at the start of a wind-up

    protected Transform player;
    protected Health playerHealth;
    protected bool seesPlayer;
    protected Vector3 home;

    public EnemyStats Stats => stats;

    float sightTimer;
    Material[] mats = new Material[0];
    Color[] baseColors = new Color[0];
    Transform cam;

    protected virtual void Awake()
    {
        // get the player ONCE, never in Update
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerHealth = p.GetComponentInParent<Health>();
            if (playerHealth == null) playerHealth = p.GetComponentInChildren<Health>();
        }

        Renderer[] rends = model != null ? model.GetComponentsInChildren<MeshRenderer>()
                         : body != null ? new Renderer[] { body } : new Renderer[0];
        mats = new Material[rends.Length];
        baseColors = new Color[rends.Length];
        for (int i = 0; i < rends.Length; i++)
        {
            mats[i] = rends[i].material;
            baseColors[i] = mats[i].color;
        }

        home = transform.position;
        if (Camera.main != null) cam = Camera.main.transform;

        if (stats == null)
        {
            Debug.LogError(name + ": no EnemyStats asset. Drag one into the Stats field.");
            enabled = false;                                   // no Update without numbers
        }

#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        if (stateLabel != null) stateLabel.gameObject.SetActive(false);   // hide the label in release
#endif
    }

    // LevelRunner calls this right after Instantiate (after Awake, before Start).
    // route = Patroller waypoints from the LevelData; other enemies ignore it.
    public virtual void Init(EnemyStats data, List<Vector3> route)
    {
        if (data == null) return;
        stats = data;
        enabled = true;                                        // in case Awake had no stats
    }

    // ---------- Sense ----------

    protected void UpdateSight()
    {
        sightTimer -= Time.deltaTime;
        if (sightTimer > 0f) return;
        sightTimer = stats.sightInterval;
        seesPlayer = CanSeePlayer();
    }

    protected bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 eye = transform.position + Vector3.up * EyeHeight;
        Vector3 toPlayer = player.position + Vector3.up * EyeHeight - eye;

        // 1. distance (cheap)
        if (toPlayer.magnitude > stats.viewRange) return false;

        // 2. angle: inside the cone? (flat, so jumping does not hide the player)
        Vector3 flat = toPlayer; flat.y = 0f;
        if (Vector3.Angle(transform.forward, flat) > stats.fov / 2f) return false;

        // 3. walls (expensive, last)
        if (Physics.Raycast(eye, toPlayer.normalized, toPlayer.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    protected float DistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        Vector3 d = player.position - transform.position; d.y = 0f;
        return d.magnitude;
    }

    protected float DistanceTo(Vector3 p)
    {
        Vector3 d = p - transform.position; d.y = 0f;
        return d.magnitude;
    }

    // ---------- Act ----------

    protected void TurnTowards(Vector3 target, float degPerSec)
    {
        Vector3 d = target - transform.position; d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(d), degPerSec * Time.deltaTime);
    }

    // Moves on the XZ plane. Returns false if a wall is in the way (does not move then).
    protected bool MoveFlat(Vector3 dir, float distance)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.000001f) return false;
        dir.Normalize();

        if (Physics.SphereCast(transform.position + Vector3.up * EyeHeight, BodyRadius, dir, out _, distance + WallGap,
                               obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        transform.position += dir * distance;
        return true;
    }

    protected void HitPlayer()
    {
        if (playerHealth != null) playerHealth.TakeDamage(stats.damage);
    }

    // ---------- Readability ----------

    protected void SetTint(Color c)
    {
        for (int i = 0; i < mats.Length; i++) mats[i].color = Color.Lerp(baseColors[i], c, 0.75f);
    }

    protected void ResetTint()
    {
        for (int i = 0; i < mats.Length; i++) mats[i].color = baseColors[i];
    }

    protected void SetWarning(bool on) { if (on) SetTint(warnColor); else ResetTint(); }

    protected void PlayWarn()
    {
        if (warnClip != null) AudioSource.PlayClipAtPoint(warnClip, transform.position);
    }

    protected void ShowState(string s)
    {
        if (stateLabel != null) stateLabel.text = s;
    }

    protected virtual void LateUpdate()
    {
        if (stateLabel != null && cam != null)
            stateLabel.transform.rotation = cam.rotation;          // 3D label faces the camera

        if (player != null)
            Debug.DrawLine(transform.position + Vector3.up * EyeHeight, player.position + Vector3.up * EyeHeight,
                           seesPlayer ? Color.green : Color.red);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (stats == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.viewRange);

        if (stats.fov >= 360f) return;
        Vector3 left = Quaternion.Euler(0f, -stats.fov / 2f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, stats.fov / 2f, 0f) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * EyeHeight, left * stats.viewRange);
        Gizmos.DrawRay(transform.position + Vector3.up * EyeHeight, right * stats.viewRange);
    }
}
