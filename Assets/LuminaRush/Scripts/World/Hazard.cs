using UnityEngine;

namespace LuminaRush
{
    public sealed class Hazard : MonoBehaviour
    {
        public void Prepare()
        {
            gameObject.SetActive(true);
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;
        }

        public void DisableHit()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }
    }
}
