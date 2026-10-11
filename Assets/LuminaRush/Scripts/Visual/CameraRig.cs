using UnityEngine;

namespace LuminaRush
{
    public sealed class CameraRig : MonoBehaviour
    {
        Vector3 velocity;
        Vector3 lookVelocity;
        Vector3 lookPoint;
        float shake;

        public void SnapToRun(Vector3 target)
        {
            transform.position = target + new Vector3(0f, 4.7f, -8.4f);
            lookPoint = target + new Vector3(0f, 1.1f, 6f);
            transform.LookAt(lookPoint);
            velocity = Vector3.zero;
        }

        public void SnapToMenu(Vector3 focus)
        {
            transform.position = focus + new Vector3(0f, 2.4f, -6.2f);
            lookPoint = focus + new Vector3(0f, 1.15f, 0f);
            transform.LookAt(lookPoint);
            velocity = Vector3.zero;
        }

        public void Follow(Vector3 target, float dt)
        {
            Vector3 desired = target + new Vector3(0f, 4.7f, -8.4f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.12f, 40f, dt);
            Vector3 desiredLook = target + new Vector3(0f, 1.15f, 6.5f);
            lookPoint = Vector3.SmoothDamp(lookPoint, desiredLook, ref lookVelocity, 0.1f, 40f, dt);
            Vector3 shakeOffset = Vector3.zero;
            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - dt * 2.5f);
                shakeOffset = new Vector3(Mathf.Sin(Time.unscaledTime * 47f), Mathf.Cos(Time.unscaledTime * 53f), 0f) * shake * 0.18f;
            }
            transform.LookAt(lookPoint + shakeOffset);
        }

        public void Punch()
        {
            shake = 1f;
        }
    }
}
