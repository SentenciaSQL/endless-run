using UnityEngine;

namespace LuminaRush
{
    public sealed class PowerOrb : MonoBehaviour
    {
        public PowerKind Kind;
        public bool Taken { get; private set; }
        float spin;

        public void Prepare(PowerKind kind)
        {
            Kind = kind;
            Taken = false;
            spin = 0f;
            gameObject.SetActive(true);
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;
        }

        public void Take()
        {
            Taken = true;
            gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (Taken)
                return;
            spin += dt * 140f;
            transform.Rotate(0f, 140f * dt, 0f, Space.World);
            Vector3 pos = transform.position;
            pos.y = 1.05f + Mathf.Sin(spin * 0.03f) * 0.12f;
            transform.position = pos;
        }
    }
}
