using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Third-person camera you turn yourself (like a shooter):
//  PC: move the mouse (the cursor is hidden/locked; click the game to lock, Esc = pause and free cursor)
//  Phone: swipe anywhere that is not a button or the joystick
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 4f, -7f);   // start position relative to the player
    public float smooth = 12f;                          // how softly it follows the player

    [Header("Look")]
    public float mouseSensitivity = 0.12f;              // degrees per pixel
    public float touchSensitivity = 0.2f;               // degrees per pixel
    public bool invertY = false;
    public float minPitch = -5f, maxPitch = 65f;        // degrees, down / up limits
    public bool lockCursor = true;                      // PC: hide and lock the cursor while playing

    [Header("Walls")]
    public LayerMask obstacleMask;                      // walls between camera and player

    float yaw, pitch, distance;
    Vector3 lookPoint;
    int lookTouch = -1;                                 // id of the finger that turns the camera
    readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    void Start()
    {
        if (target == null) return;

        Vector3 o = offset - Vector3.up * 1.2f;         // from the look point to the camera
        distance = o.magnitude;
        pitch = Mathf.Atan2(o.y, new Vector2(o.x, o.z).magnitude) * Mathf.Rad2Deg;
        yaw = Mathf.Atan2(-o.x, -o.z) * Mathf.Rad2Deg;
        lookPoint = LookTarget();

        if (lockCursor && Touchscreen.current == null) Lock(true);
    }

    void Update()
    {
        if (target == null) return;

        // paused (Time.timeScale = 0): give the cursor back so menu buttons work
        if (Time.timeScale == 0f)
        {
            if (Cursor.lockState == CursorLockMode.Locked) Lock(false);
            lookTouch = -1;
            return;
        }

        Vector2 d = Vector2.zero;

        // ---- mouse ----
        Mouse mouse = Mouse.current;
        if (mouse != null && Touchscreen.current == null)
        {
            if (lockCursor && Cursor.lockState != CursorLockMode.Locked &&
                mouse.leftButton.wasPressedThisFrame && !OverUI(mouse.position.ReadValue()))
                Lock(true);

            if (!lockCursor || Cursor.lockState == CursorLockMode.Locked)
                d += mouse.delta.ReadValue() * mouseSensitivity;
        }

        // ---- touch swipe ----
        Touchscreen ts = Touchscreen.current;
        if (ts != null)
        {
            foreach (var t in ts.touches)
            {
                int id = t.touchId.ReadValue();
                var phase = t.phase.ReadValue();

                if (lookTouch < 0 && phase == UnityEngine.InputSystem.TouchPhase.Began && !OverUI(t.position.ReadValue()))
                    lookTouch = id;

                if (id != lookTouch) continue;

                if (phase == UnityEngine.InputSystem.TouchPhase.Moved)
                    d += t.delta.ReadValue() * touchSensitivity;
                else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    lookTouch = -1;
            }
        }

        yaw += d.x;
        pitch += invertY ? d.y : -d.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null) return;

        lookPoint = Vector3.Lerp(lookPoint, LookTarget(), smooth * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = rot * Vector3.back;
        float dist = distance;

        // a wall behind the player: pull the camera in front of it
        if (obstacleMask.value != 0 &&
            Physics.SphereCast(lookPoint, 0.3f, back, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            dist = Mathf.Max(hit.distance - 0.1f, 0.6f);

        transform.position = lookPoint + back * dist;
        transform.rotation = rot;
    }

    Vector3 LookTarget() { return target.position + Vector3.up * 1.2f; }

    // true if the screen point is over a button, the joystick or another UI element
    bool OverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        PointerEventData ped = new PointerEventData(EventSystem.current) { position = screenPos };
        uiHits.Clear();
        EventSystem.current.RaycastAll(ped, uiHits);
        return uiHits.Count > 0;
    }

    static void Lock(bool on)
    {
        Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !on;
    }

    void OnDisable() { Lock(false); }
}
