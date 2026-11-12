using System.Collections.Generic;
using UnityEngine;

namespace TilkayoRun
{
    /// <summary>Материалы и помощники для частиц.</summary>
    public static class Fx
    {
        public static float ParticleScale = 1f;
        public static Material World, Glow, Sky, Soft, GlowParticle, SpriteBase;

        public static void InitMaterials()
        {
            World = Create("Tilkayo/World", "World");
            World.SetFloat("_Curve", 0.0004f);
            World.SetFloat("_Sway", 0.28f);

            Glow = Create("Tilkayo/World", "WorldGlow");
            Glow.SetFloat("_Glow", 0.7f);
            Glow.SetFloat("_Curve", 0.0004f);
            Glow.SetFloat("_Sway", 0f);

            Sky = Create("Tilkayo/World", "Sky");
            Sky.SetFloat("_Glow", 1f);
            Sky.SetFloat("_Curve", 0f);
            Sky.SetFloat("_Sway", 0f);
            Sky.SetFloat("_FogOn", 0f);
            Sky.renderQueue = 999;

            Soft = Create("Tilkayo/Soft", "Soft");
            GlowParticle = Create("Tilkayo/Glow", "GlowParticle");
            SpriteBase = Create("Tilkayo/Sprite", "SpriteBase");
        }

        public static Material Create(string shaderName, string name)
        {
            Shader s = Shader.Find(shaderName);
            if (s == null)
            {
                Debug.LogError("Shader not found: " + shaderName + " (должен лежать в Assets/Resources/Shaders)");
                s = Shader.Find("Sprites/Default");
            }
            return new Material(s) { name = name };
        }

        public static void SetThemeGlobals(ThemeDef th)
        {
            Shader.SetGlobalColor("_TFogColor", th.skyBottom);
            Shader.SetGlobalVector("_TFogParams", new Vector4(th.fogStart, th.fogEnd, 0, 0));
            Vector3 d = th.sunDir.normalized;
            Shader.SetGlobalVector("_TSunDir", new Vector4(d.x, d.y, d.z, 0));
            Shader.SetGlobalColor("_TAmbient", th.ambient);
            Shader.SetGlobalColor("_TSunColor", th.sunLight);
        }

        // ------------------------------------------------------------------ частицы
        public static ParticleSystem MakeSystem(string name, Transform parent, Material mat, int maxParticles, bool burstOnly)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = Mathf.Max(8, Mathf.RoundToInt(maxParticles * ParticleScale));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.2f;
            main.startColor = Color.white;
            var shape = ps.shape; shape.enabled = false;
            var em = ps.emission; em.enabled = !burstOnly; em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        public static void FadeInOut(ParticleSystem ps, float fadeIn = 0.15f, float fadeOutStart = 0.6f)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeIn), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        public static void ScrollBack(ParticleSystem ps, float speed)
        {
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(0f);
            v.y = new ParticleSystem.MinMaxCurve(0f);
            v.z = new ParticleSystem.MinMaxCurve(-speed);
        }

        public static void Burst(ParticleSystem ps, Vector3 pos, int count, Color color, float speed, float size)
        {
            count = Mathf.Max(1, Mathf.RoundToInt(count * ParticleScale));
            var ep = new ParticleSystem.EmitParams();
            ep.position = pos;
            ep.startColor = color;
            ep.startSize = size;
            for (int i = 0; i < count; i++)
            {
                ep.velocity = Random.onUnitSphere * speed * Random.Range(0.4f, 1f);
                ep.startLifetime = Random.Range(0.35f, 0.8f);
                ps.Emit(ep, 1);
            }
        }

        /// <summary>Настройка ауры частиц под скин.</summary>
        public static void ConfigureAura(ParticleSystem ps, SkinDef s)
        {
            var main = ps.main;
            var em = ps.emission;
            var shape = ps.shape;
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (s.aura == AuraKind.None)
            {
                em.enabled = false;
                return;
            }

            em.enabled = true;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.3f, 1.1f, 0.4f);
            shape.position = new Vector3(0.1f, 0.2f, 0.2f);
            rend.sharedMaterial = GlowParticle;
            var grav = 0f;
            float rate = 18f;
            float life = 1f, size = 0.14f, speed = 0.4f;
            Color c = s.auraColor;
            bool rainbow = false;

            switch (s.aura)
            {
                case AuraKind.Sparkle: rate = 16f; life = 1.1f; size = 0.11f; speed = 0.5f; grav = -0.25f; break;
                case AuraKind.Fire: rate = 38f; life = 0.7f; size = 0.28f; speed = 0.8f; grav = -1.2f; break;
                case AuraKind.Snow: rate = 22f; life = 1.4f; size = 0.1f; speed = 0.2f; grav = 0.25f; break;
                case AuraKind.Ghost: rate = 14f; life = 1.4f; size = 0.34f; speed = 0.2f; grav = -0.3f; break;
                case AuraKind.Neon: rate = 40f; life = 0.5f; size = 0.12f; speed = 0.2f; grav = 0f; break;
                case AuraKind.Gold: rate = 24f; life = 1.0f; size = 0.12f; speed = 0.4f; grav = -0.3f; break;
                case AuraKind.Rainbow: rate = 30f; life = 1.0f; size = 0.14f; speed = 0.5f; grav = -0.3f; rainbow = true; break;
            }

            main.startLifetime = life;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.2f);
            main.startSpeed = speed;
            main.gravityModifier = grav;
            main.maxParticles = Mathf.Max(20, Mathf.RoundToInt(80 * ParticleScale));
            if (rainbow) main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.cyan);
            else main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.5f));
            em.rateOverTime = rate * ParticleScale;
            FadeInOut(ps, 0.15f, 0.55f);

            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, s.aura == AuraKind.Fire ? 0.1f : 0.5f));

            ps.Play();
        }
    }
}
