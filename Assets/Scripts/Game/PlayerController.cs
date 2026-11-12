using UnityEngine;

namespace TilkayoRun
{
    public class PlayerController : MonoBehaviour
    {
        public enum Mode { Menu, Run, Dead }

        public SkinVisual visual;
        public float X, Y, Vy;
        public int Lane;
        public bool Sliding;
        public float HalfW = 0.25f;
        public bool Grounded { get { return Y <= 0.0005f && Vy <= 0.01f; } }

        Transform shadow;
        ParticleSystem dust;
        float slideTimer, jumpBuffer, animT;
        bool slideOnLand;
        Mode mode = Mode.Menu;
        float deadT;
        bool fell;
        Vector3 shadowBase = Vector3.one;

        public static PlayerController Create(Transform parent)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<PlayerController>();
            p.Init();
            return p;
        }

        void Init()
        {
            visual = SkinVisual.Create(transform, true);

            // мягкая тень под персонажем
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(transform, false);
            shadow = sh.transform;
            var mf = sh.AddComponent<MeshFilter>();
            mf.sharedMesh = ShadowMesh();
            var mr = sh.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Fx.Soft;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            dust = Fx.MakeSystem("Dust", transform, Fx.Soft, 40, false);
            var main = dust.main;
            main.startLifetime = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
            main.startColor = new Color(0.75f, 0.68f, 0.55f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
            main.gravityModifier = -0.15f;
            var em = dust.emission; em.enabled = true; em.rateOverTime = 0f;
            var shape = dust.shape;
            shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(0.7f, 0.05f, 0.3f);
            Fx.FadeInOut(dust, 0.1f, 0.3f);
            dust.Play();
        }

        static Mesh shadowMesh;
        static Mesh ShadowMesh()
        {
            if (shadowMesh != null) return shadowMesh;
            shadowMesh = new Mesh { name = "ShadowQuad" };
            shadowMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f)
            };
            shadowMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            shadowMesh.colors = new[] { new Color(0, 0, 0, 0.55f), new Color(0, 0, 0, 0.55f), new Color(0, 0, 0, 0.55f), new Color(0, 0, 0, 0.55f) };
            shadowMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            shadowMesh.RecalculateBounds();
            return shadowMesh;
        }

        public void ResetPlayer(Mode m)
        {
            mode = m;
            Lane = 0; X = 0; Y = 0; Vy = 0; Sliding = false; slideTimer = 0; jumpBuffer = 0; slideOnLand = false;
            deadT = 0; fell = false;
            visual.root.gameObject.SetActive(true);
            visual.root.localPosition = Vector3.zero;
            visual.root.localRotation = Quaternion.identity;
            visual.root.localScale = Vector3.one;
            visual.SetAuraActive(true);
        }

        public void SetMode(Mode m) { mode = m; }

        // ---------- ввод ----------
        public void MoveLane(int dir)
        {
            if (mode != Mode.Run) return;
            int nl = Mathf.Clamp(Lane + dir, -1, 1);
            if (nl != Lane)
            {
                Lane = nl;
                AudioManager.I.Play(Sfx.Lane, 1f + 0.05f * Lane);
            }
        }

        public void Jump() { if (mode == Mode.Run) jumpBuffer = 0.16f; }

        public void Slide()
        {
            if (mode != Mode.Run) return;
            if (Grounded)
            {
                Sliding = true; slideTimer = GameConfig.SlideTime;
                AudioManager.I.Play(Sfx.Slide);
            }
            else
            {
                Vy = GameConfig.FastFallVelocity;
                slideOnLand = true;
            }
        }

        public void Die(bool fall)
        {
            mode = Mode.Dead;
            fell = fall;
            deadT = 0f;
            Sliding = false;
            if (!fall) { Vy = 6f; }
        }

        // ---------- обновление ----------
        public void Tick(float dt, float speed)
        {
            float targetX = Lane * GameConfig.LaneWidth;

            if (mode == Mode.Run)
            {
                X = Mathf.MoveTowards(X, targetX, GameConfig.LaneChangeSpeed * dt);

                if (jumpBuffer > 0f)
                {
                    jumpBuffer -= dt;
                    if (Y <= 0.0005f && Vy <= 0.01f)
                    {
                        Sliding = false; slideTimer = 0f;
                        Vy = GameConfig.JumpVelocity;
                        Y = 0.002f;
                        jumpBuffer = 0f;
                        AudioManager.I.Play(Sfx.Jump);
                    }
                }

                Vy -= GameConfig.Gravity * dt;
                Y += Vy * dt;
                if (Y <= 0f)
                {
                    bool wasAir = Y < -0.001f && Vy < -3f;
                    Y = 0f; Vy = 0f;
                    if (wasAir) { Fx.Burst(dust, transform.position + new Vector3(X, 0.1f, 0), 6, new Color(0.8f, 0.72f, 0.6f, 0.5f), 1.2f, 0.3f); }
                    if (slideOnLand) { slideOnLand = false; Sliding = true; slideTimer = GameConfig.SlideTime; }
                }

                if (Sliding)
                {
                    slideTimer -= dt;
                    if (slideTimer <= 0f) Sliding = false;
                }
            }
            else if (mode == Mode.Dead)
            {
                deadT += dt;
                if (!fell)
                {
                    Vy -= GameConfig.Gravity * dt;
                    Y = Mathf.Max(0f, Y + Vy * dt);
                    if (Y <= 0f) Vy = 0f;
                }
                else
                {
                    Vy -= GameConfig.Gravity * dt * 0.6f;
                    Y += Vy * dt;
                }
            }

            Animate(dt, speed, targetX);
            visual.Tick(dt, mode == Mode.Dead ? 0f : speed);

            var em = dust.emission;
            em.rateOverTime = (mode == Mode.Run && Grounded && !Sliding) ? 16f * Fx.ParticleScale : (mode == Mode.Run && Sliding ? 28f * Fx.ParticleScale : 0f);
            dust.transform.position = transform.position + new Vector3(X, 0.05f, -0.3f);
            Fx.ScrollBack(dust, speed * 0.9f);
        }

        void Animate(float dt, float speed, float targetX)
        {
            float sx = 1f, sy = 1f, rot = 0f, bob = 0f;
            float px = X;

            if (mode == Mode.Run)
            {
                animT += dt * (9f + speed * 0.2f);
                if (Y > 0.01f)
                {
                    float st = Mathf.Clamp(Vy * 0.013f, -0.12f, 0.16f);
                    sy = 1f + st; sx = 1f - st * 0.7f;
                    rot = Mathf.Sin(animT * 0.5f) * 2f;
                }
                else if (Sliding)
                {
                    sy = 0.5f; sx = 1.28f; rot = -4f;
                    bob = 0f;
                }
                else
                {
                    float s = Mathf.Sin(animT);
                    bob = Mathf.Abs(s) * 0.11f;
                    float sq = Mathf.Sin(animT * 2f + 1f);
                    sy = 1f + 0.06f * sq; sx = 1f - 0.05f * sq;
                    rot = s * 3.5f;
                }
                rot += (X - targetX) * 6f;
            }
            else if (mode == Mode.Menu)
            {
                animT += dt * 2.4f;
                float s = Mathf.Sin(animT);
                sy = 1f + 0.025f * s; sx = 1f - 0.02f * s;
                bob = Mathf.Abs(Mathf.Sin(animT * 0.5f)) * 0.04f;
                rot = Mathf.Sin(animT * 0.5f) * 1.5f;
            }
            else
            {
                float t = Mathf.Clamp01(deadT / 0.35f);
                if (fell)
                {
                    sy = Mathf.Lerp(1f, 0.6f, t); sx = Mathf.Lerp(1f, 0.6f, t);
                    rot = Mathf.Lerp(0f, 160f, t);
                }
                else
                {
                    sy = Mathf.Lerp(1f, 0.75f, t); sx = Mathf.Lerp(1f, 1.2f, t);
                    rot = Mathf.Lerp(0f, 80f, t);
                }
            }

            visual.root.localPosition = new Vector3(px, Y + bob, 0f);
            visual.root.localRotation = Quaternion.Euler(0f, 0f, rot);
            visual.root.localScale = new Vector3(sx, sy, 1f);

            float h = Mathf.Clamp01(Y / 2.6f);
            float sc = Mathf.Lerp(1.5f, 0.8f, h);
            if (fell && mode == Mode.Dead) sc *= Mathf.Clamp01(1f - deadT * 2f);
            shadow.localPosition = new Vector3(px, 0.03f, 0.1f);
            shadow.localScale = new Vector3(sc * (Sliding ? 1.3f : 1f), 1f, sc * 0.8f);
        }
    }
}
