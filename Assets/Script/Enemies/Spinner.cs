using UnityEngine;

// Rotating low bar: jump over it. Damage comes from a Hazard on the same object.
[RequireComponent(typeof(Rigidbody))]
public class Spinner : MonoBehaviour
{
    public float degPerSec = 90f;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void FixedUpdate()
    {
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, degPerSec * Time.fixedDeltaTime, 0f));
    }
}
