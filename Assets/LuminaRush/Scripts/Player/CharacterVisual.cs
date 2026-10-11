using UnityEngine;

namespace LuminaRush
{
    public sealed class CharacterVisual : MonoBehaviour
    {
        Transform rig;
        Transform body;
        Transform head;
        Transform scarf;
        Transform legL;
        Transform legR;
        Transform armL;
        Transform armR;
        Transform shield;
        MaterialPropertyBlock block;
        Color bodyColor = Color.white;
        Color accent = Color.white;
        Color cloth = Color.white;
        Color dark = Color.black;
        float phase;

        public void Build(Material material, Transform parent)
        {
            if (rig != null)
                return;
            block = new MaterialPropertyBlock();
            rig = new GameObject("Rig").transform;
            rig.SetParent(parent, false);
            body = Part("Body", MeshLibrary.Box(), material, rig);
            head = Part("Head", MeshLibrary.Sphere(), material, rig);
            scarf = Part("Scarf", MeshLibrary.Wedge(), material, rig);
            legL = Part("LegL", MeshLibrary.Box(), material, rig);
            legR = Part("LegR", MeshLibrary.Box(), material, rig);
            armL = Part("ArmL", MeshLibrary.Box(), material, rig);
            armR = Part("ArmR", MeshLibrary.Box(), material, rig);
            shield = Part("Shield", MeshLibrary.Sphere(), material, rig);
            shield.gameObject.SetActive(false);
        }

        public void Apply(int characterIndex, int skinIndex)
        {
            RunnerShape shape = Catalog.Characters[Mathf.Clamp(characterIndex, 0, Catalog.Characters.Length - 1)];
            SkinTint skin = Catalog.Skins[Mathf.Clamp(skinIndex, 0, Catalog.Skins.Length - 1)];
            bodyColor = skin.Body;
            accent = skin.Accent;
            cloth = skin.Cloth;
            dark = skin.Dark;
            body.localScale = new Vector3(shape.BodyW, shape.BodyH, shape.BodyW * 0.72f);
            body.localPosition = new Vector3(0f, shape.Leg + shape.BodyH * 0.5f, 0f);
            head.localScale = Vector3.one * shape.Head;
            head.localPosition = body.localPosition + Vector3.up * (shape.BodyH * 0.5f + shape.Head * 0.38f);
            float leg = shape.Leg;
            legL.localScale = new Vector3(0.18f, leg, 0.18f);
            legR.localScale = legL.localScale;
            legL.localPosition = new Vector3(-shape.BodyW * 0.28f, leg * 0.5f, 0f);
            legR.localPosition = new Vector3(shape.BodyW * 0.28f, leg * 0.5f, 0f);
            armL.localScale = new Vector3(0.14f, shape.BodyH * 0.55f, 0.14f);
            armR.localScale = armL.localScale;
            armL.localPosition = body.localPosition + new Vector3(-shape.BodyW * 0.62f, 0.05f, 0f);
            armR.localPosition = body.localPosition + new Vector3(shape.BodyW * 0.62f, 0.05f, 0f);
            scarf.gameObject.SetActive(shape.Style == 0 || shape.Style == 2);
            scarf.localScale = shape.Style == 2 ? new Vector3(0.18f, 0.55f, 0.18f) : new Vector3(0.42f, 0.5f, 0.16f);
            scarf.localPosition = head.localPosition + new Vector3(0f, -0.05f, -shape.Head * 0.2f);
            shield.localScale = Vector3.one * (shape.BodyH + 0.8f);
            shield.localPosition = body.localPosition;
            Paint();
        }

        public void SetShield(bool on)
        {
            if (shield != null)
                shield.gameObject.SetActive(on);
        }

        public void Animate(float dt, float speed, bool grounded, bool sliding, float height, float squash, bool dead)
        {
            phase += dt * Mathf.Lerp(8f, 16f, Mathf.Clamp01(speed / 20f));
            float swing = grounded && !sliding && !dead ? Mathf.Sin(phase) : 0f;
            legL.localRotation = Quaternion.Euler(swing * 48f, 0f, 0f);
            legR.localRotation = Quaternion.Euler(-swing * 48f, 0f, 0f);
            armL.localRotation = Quaternion.Euler(-swing * 35f, 0f, 0f);
            armR.localRotation = Quaternion.Euler(swing * 35f, 0f, 0f);
            float bob = grounded && !dead ? Mathf.Abs(swing) * 0.06f : 0f;
            float squashY = 1f;
            if (squash > 0f)
                squashY = 0.82f;
            else if (!grounded)
                squashY = 1.12f;
            if (sliding)
            {
                rig.localRotation = Quaternion.Euler(78f, 0f, 0f);
                rig.localPosition = new Vector3(0f, 0.15f, 0f);
            }
            else if (dead)
            {
                rig.localRotation = Quaternion.Euler(70f, 20f, 30f);
                rig.localPosition = new Vector3(0f, 0.2f, 0f);
            }
            else
            {
                rig.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-swing * 4f, -8f, 8f));
                rig.localPosition = new Vector3(0f, bob, 0f);
            }
            rig.localScale = new Vector3(1f, squashY, 1f);
            if (height > 0.2f && scarf != null)
                scarf.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            else if (scarf != null)
                scarf.localRotation = Quaternion.Euler(18f + swing * 8f, 0f, 0f);
        }

        void Paint()
        {
            Tint(body, bodyColor);
            Tint(head, bodyColor);
            Tint(scarf, cloth);
            Tint(legL, dark);
            Tint(legR, dark);
            Tint(armL, accent);
            Tint(armR, accent);
            Tint(shield, new Color(accent.r, accent.g, accent.b, 1f));
        }

        void Tint(Transform part, Color color)
        {
            if (part == null)
                return;
            Renderer renderer = part.GetComponent<Renderer>();
            MeshLibrary.Tint(renderer, color, block);
        }

        static Transform Part(string name, Mesh mesh, Material material, Transform parent)
        {
            return MeshLibrary.Create(name, mesh, material, parent).transform;
        }
    }
}
