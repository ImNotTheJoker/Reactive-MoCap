using UnityEngine;

/// <summary>
/// Calculates and smooths the velocity of the object.
/// </summary>
public class UserVelocityHandler : MonoBehaviour
{
    private Vector3 previousPosition; // Stores the previous position
    public float currentVelocity; // Raw velocity
    public float smoothVelocity = 0f; // Smoothed velocity
    private float smoothingFactor = 0.01f; // Controls the smoothing rate

    void Awake()
    {
        previousPosition = transform.position;
    }

    void Update()
    {
        Vector3 deltaPosition = transform.position - previousPosition;
        float rawVelocity = deltaPosition.magnitude / Time.deltaTime;

        currentVelocity = rawVelocity;
        smoothVelocity = Mathf.Lerp(smoothVelocity, rawVelocity, smoothingFactor); // Smooth the velocity

        previousPosition = transform.position;
    }
}
