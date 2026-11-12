using UnityEngine;

namespace TilkayoRun
{
    /// <summary>Камера от третьего лица: плавное следование, зум по скорости, тряска при ударе.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public Transform sky;
        public PlayerController player;

        float runBlend;       // 0 = меню, 1 = забег
        float targetBlend;
        float speedT;
        float shake;
        Vector3 pos;
        Quaternion rot = Quaternion.identity;
        bool inited;

        public void Init(Camera c)
        {
            cam = c;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 420f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.allowHDR = false;
            cam.allowMSAA = true;
        }

        public void SetRunMode(bool run, bool instant = false)
        {
            targetBlend = run ? 1f : 0f;
            if (instant) runBlend = targetBlend;
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        public void SetSpeedT(float t) { speedT = Mathf.Clamp01(t); }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale <= 0f) dt = 0f;
            runBlend = Mathf.MoveTowards(runBlend, targetBlend, dt * 1.3f);
            float e = Mathf.SmoothStep(0f, 1f, runBlend);

            float px = player != null ? player.X : 0f;
            Vector3 runPos = new Vector3(px * 0.55f, Mathf.Lerp(4.5f, 4.0f, speedT), Mathf.Lerp(-6.8f, -5.9f, speedT));
            Vector3 runLook = new Vector3(px * 0.7f, 1.1f, 8f);
            Vector3 menuPos = new Vector3(0.0f, 1.55f, -4.9f);
            Vector3 menuLook = new Vector3(0f, -0.15f, 0f);

            Vector3 wantPos = Vector3.Lerp(menuPos, runPos, e);
            Vector3 wantLook = Vector3.Lerp(menuLook, runLook, e);
            Quaternion wantRot = Quaternion.LookRotation(wantLook - wantPos, Vector3.up);

            if (!inited) { pos = wantPos; rot = wantRot; inited = true; }
            float k = 1f - Mathf.Exp(-9f * dt);
            pos = Vector3.Lerp(pos, wantPos, dt > 0 ? k : 0f);
            rot = Quaternion.Slerp(rot, wantRot, dt > 0 ? k : 0f);

            Vector3 off = Vector3.zero;
            if (shake > 0.001f)
            {
                off = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shake;
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
            }

            transform.position = pos + off;
            transform.rotation = rot;
            cam.fieldOfView = Mathf.Lerp(Mathf.Lerp(58f, 62f, e), 67f, speedT * e);

            if (sky != null)
            {
                // небо следует за камерой по XZ, но не поворачивается
                sky.position = new Vector3(pos.x, 0f, pos.z + 330f);
            }
        }
    }
}
