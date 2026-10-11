using UnityEngine;

namespace LuminaRush
{
    public sealed class Effects : MonoBehaviour
    {
        ParticleSystem[] bursts;
        int cursor;

        public void Build()
        {
            bursts = new ParticleSystem[4];
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            var material = new Material(shader);
            for (int i = 0; i < bursts.Length; i++)
            {
                var go = new GameObject("Burst" + i);
                go.transform.SetParent(transform, false);
                ParticleSystem system = go.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 0.35f;
                main.startLifetime = 0.4f;
                main.startSpeed = 3.2f;
                main.startSize = 0.16f;
                main.maxParticles = 18;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = system.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
                var shape = system.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.15f;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                bursts[i] = system;
            }
        }

        public void Burst(Vector3 position, Color color)
        {
            if (bursts == null || bursts.Length == 0)
                return;
            ParticleSystem system = bursts[cursor];
            cursor = (cursor + 1) % bursts.Length;
            system.transform.position = position;
            var main = system.main;
            main.startColor = color;
            system.Play(true);
        }
    }
}
