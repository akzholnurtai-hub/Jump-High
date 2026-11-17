using UnityEngine;

// Rotating low bar: jump over it. Damage comes from a Hazard on the same object,
// turn speed from ItemValues.spinnerSpeed.
[RequireComponent(typeof(Rigidbody))]
public class Spinner : MonoBehaviour
{
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        float degPerSec = GameConfig.Get().items.spinnerSpeed;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, degPerSec * Time.fixedDeltaTime, 0f));
    }
}
