using UnityEngine;

// Added to the Shooter's projectile at runtime: the shot is destroyed when it touches a wall.
public class ShotStopper : MonoBehaviour
{
    public LayerMask walls;

    void OnTriggerEnter(Collider other)
    {
        if ((walls.value & (1 << other.gameObject.layer)) != 0)
            Destroy(gameObject);
    }
}
