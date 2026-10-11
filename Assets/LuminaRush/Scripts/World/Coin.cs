using UnityEngine;

namespace LuminaRush
{
    public sealed class Coin : MonoBehaviour
    {
        public bool Taken { get; private set; }
        float spin;
        float baseY;

        public void Prepare(float y)
        {
            Taken = false;
            baseY = y;
            spin = Random.Range(0f, 180f);
            gameObject.SetActive(true);
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;
        }

        public int Take(int value)
        {
            if (Taken)
                return 0;
            Taken = true;
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            gameObject.SetActive(false);
            return value;
        }

        public bool Tick(float dt, bool magnet, Vector3 magnetPoint)
        {
            if (Taken)
                return false;
            spin += dt * 220f;
            Vector3 pos = transform.position;
            if (magnet)
            {
                Vector3 to = magnetPoint - pos;
                if (to.sqrMagnitude < Tune.MagnetRadius * Tune.MagnetRadius)
                    pos = Vector3.MoveTowards(pos, magnetPoint, 18f * dt);
            }
            else
            {
                pos.y = baseY + Mathf.Sin(spin * 0.05f) * 0.08f;
            }
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, spin, 0f);
            return magnet && (magnetPoint - pos).sqrMagnitude < 0.45f * 0.45f;
        }
    }
}
