using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TilkayoRun
{
    /// <summary>Главный контроллер: собирает сцену из кода и ведёт состояния игры.</summary>
    public class GameController : MonoBehaviour
    {
        public enum State { Loading, Menu, Shop, Countdown, Running, Paused, Dead, GameOver }

        public static GameController I;
        public State state = State.Loading;

        public PlayerController player;
        public WorldRunner world;
        public CameraRig camRig;
        public UIManager ui;
        public SkinPreview preview;
        public InputHandler input;
        AudioManager audioMgr;

        ParticleSystem ambient, coinFx, crashFx;
        float speed, distance, runTime, deadTimer, menuSpeed;
        int runCoins;
        int nativeW, nativeH;
        bool loadingTheme;
        int builtLevel = -1, builtQuality = -1;
        float resumeCountdown;
        Camera cam;

        ThemeDef Theme { get { return Themes.All[SaveSystem.Data.level]; } }

        void Awake()
        {
            I = this;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            nativeW = Screen.width; nativeH = Screen.height;

            var d = SaveSystem.Data;
            if (d.quality < 0)
            {
                d.quality = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3000 ? 0 : 1;
                SaveSystem.Commit();
            }
            UIKit.Init();
            ApplyQualityInternal();
            Fx.InitMaterials();
            Fx.SetThemeGlobals(Theme);

            // камера
            cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera");
                cgo.tag = "MainCamera";
                cam = cgo.AddComponent<Camera>();
            }
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            camRig = cam.gameObject.GetComponent<CameraRig>();
            if (camRig == null) camRig = cam.gameObject.AddComponent<CameraRig>();
            camRig.Init(cam);
            cam.backgroundColor = Theme.skyBottom;

            // мир
            var wgo = new GameObject("World");
            wgo.transform.SetParent(transform, false);
            world = wgo.AddComponent<WorldRunner>();
            world.Init();
            camRig.sky = world.SkyTransform;
            world.onCoin = OnCoinCollected;

            // игрок
            player = PlayerController.Create(transform);
            camRig.player = player;
            player.visual.SetSkin(SkinDatabase.Get(d.selectedSkin));

            // студия предпросмотра
            preview = SkinPreview.Create();
            preview.transform.SetParent(transform, true);

            // эффекты
            ambient = Fx.MakeSystem("AmbientGold", transform, Fx.GlowParticle, 90, false);
            SetupAmbient();
            coinFx = Fx.MakeSystem("CoinFx", transform, Fx.GlowParticle, 120, true);
            crashFx = Fx.MakeSystem("CrashFx", transform, Fx.GlowParticle, 80, true);

            // ввод
            input = gameObject.AddComponent<InputHandler>();
            input.onSwipe = OnSwipe;

            // звук
            var ago = new GameObject("Audio");
            ago.transform.SetParent(transform, false);
            audioMgr = ago.AddComponent<AudioManager>();
            audioMgr.Init();

            // UI
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            var uigo = new GameObject("UIManager");
            uigo.transform.SetParent(transform, false);
            ui = uigo.AddComponent<UIManager>();
            ui.Build(this);

            StartCoroutine(Boot());
        }

        void SetupAmbient()
        {
            var main = ambient.main;
            main.startLifetime = 6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startColor = Theme.dust;
            main.gravityModifier = -0.01f;
            var em = ambient.emission; em.enabled = true; em.rateOverTime = 14f * Fx.ParticleScale;
            var sh = ambient.shape;
            sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(22f, 7f, 50f);
            Fx.FadeInOut(ambient, 0.25f, 0.7f);
            ambient.Play();
        }

        IEnumerator Boot()
        {
            state = State.Loading;
            ui.ShowLoading(true);
            yield return null;
            yield return StartCoroutine(RebuildTheme());
            ui.ShowLoading(false);
            EnterMenu();
        }

        IEnumerator RebuildTheme()
        {
            loadingTheme = true;
            var th = Theme;
            int q = SaveSystem.Data.quality;
            cam.backgroundColor = th.skyBottom;
            var main = ambient.main; main.startColor = th.dust;
            world.SetDifficulty(GameConfig.StartSpeed + th.speedBonus, GameConfig.MaxSpeed + th.speedBonus * 0.6f);
            yield return StartCoroutine(world.BuildTheme(th, q));
            builtLevel = SaveSystem.Data.level; builtQuality = q;
            world.ResetWorld(false);
            loadingTheme = false;
        }

        // ============================================================ состояния
        void EnterMenu()
        {
            Time.timeScale = 1f;
            state = State.Menu;
            player.ResetPlayer(PlayerController.Mode.Menu);
            world.ResetWorld(false);
            camRig.SetRunMode(false);
            camRig.SetSpeedT(0f);
            input.Active = false;
            audioMgr.PlayMusic(false);
            ui.ShowMenu();
        }

        public void StartRun()
        {
            if (state != State.Menu && state != State.GameOver && state != State.Paused) return;
            StartCoroutine(StartRunRoutine());
        }

        IEnumerator StartRunRoutine()
        {
            if (builtLevel != SaveSystem.Data.level || builtQuality != SaveSystem.Data.quality)
            {
                ui.ShowLoading(true);
                yield return null;
                yield return StartCoroutine(RebuildTheme());
                ui.ShowLoading(false);
            }
            Time.timeScale = 1f;
            var th = Theme;
            speed = GameConfig.StartSpeed + th.speedBonus;
            distance = 0f; runTime = 0f; runCoins = 0;
            player.ResetPlayer(PlayerController.Mode.Run);
            world.SetDifficulty(GameConfig.StartSpeed + th.speedBonus, GameConfig.MaxSpeed + th.speedBonus * 0.6f);
            world.ResetWorld(true);
            camRig.SetRunMode(true);
            camRig.SetSpeedT(0f);
            ui.ShowHud();
            input.Active = true;
            state = State.Running;
            audioMgr.PlayMusic(true);
            SaveSystem.Data.totalRuns++;
        }

        public void Pause()
        {
            if (state != State.Running) return;
            state = State.Paused;
            Time.timeScale = 0f;
            input.Active = false;
            ui.ShowPause(true);
        }

        public void Resume()
        {
            if (state != State.Paused) return;
            ui.ShowPause(false);
            state = State.Countdown;
            resumeCountdown = 3f;
        }

        public void Restart()
        {
            ui.ShowPause(false);
            Time.timeScale = 1f;
            state = State.Menu;
            StartCoroutine(StartRunRoutine());
        }

        public void GoMenu()
        {
            ui.ShowPause(false);
            SaveSystem.Commit();
            EnterMenu();
        }

        public void OpenShop()
        {
            if (state != State.Menu) return;
            state = State.Shop;
            ui.ShowShop(true);
        }

        public void CloseShop()
        {
            state = State.Menu;
            player.visual.SetSkin(SkinDatabase.Get(SaveSystem.Data.selectedSkin));
            ui.ShowShop(false);
        }

        public void EquipSkin(int id)
        {
            if (!SaveSystem.Owns(id)) return;
            SaveSystem.Data.selectedSkin = id;
            SaveSystem.Commit();
            player.visual.SetSkin(SkinDatabase.Get(id));
        }

        public void ChangeLevel(int dir)
        {
            var d = SaveSystem.Data;
            int nl = d.level + dir;
            if (nl < 0 || nl >= Themes.All.Length) return;
            if (nl > d.unlockedLevel)
            {
                ui.Toast("Уровень откроется после " + Themes.All[nl].unlockDistance + " м (лучший: " + d.bestDistance + " м)");
                return;
            }
            d.level = nl;
            SaveSystem.Commit();
            ui.RefreshMenu();
            StartCoroutine(SwitchThemeRoutine());
        }

        IEnumerator SwitchThemeRoutine()
        {
            if (loadingTheme) yield break;
            ui.ShowLoading(true);
            yield return null;
            yield return StartCoroutine(RebuildTheme());
            ui.ShowLoading(false);
        }

        // ============================================================ настройки
        public void ApplyAudio() { audioMgr.ApplySettings(); }

        public void SetQuality(int q)
        {
            SaveSystem.Data.quality = Mathf.Clamp(q, 0, 2);
            SaveSystem.Commit();
            ApplyQualityInternal();
            // меши пересоберутся при старте забега (плотность леса зависит от качества)
        }

        void ApplyQualityInternal()
        {
            int q = SaveSystem.Data.quality;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = q == 0 ? 30 : 60;
            QualitySettings.antiAliasing = q == 2 ? 2 : 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.pixelLightCount = 0;
            Fx.ParticleScale = q == 0 ? 0.5f : (q == 1 ? 0.8f : 1f);
#if !UNITY_EDITOR
            if (nativeW > 0 && nativeH > 0)
            {
                // целевая короткая сторона: 540 / 720 / 1080 пикселей (не выше родного разрешения)
                float targetShort = q == 0 ? 540f : (q == 1 ? 720f : 1080f);
                float s = Mathf.Min(1f, targetShort / Mathf.Min(nativeW, nativeH));
                Screen.SetResolution(Mathf.RoundToInt(nativeW * s), Mathf.RoundToInt(nativeH * s), true);
            }
#endif
        }

        // ============================================================ ввод
        void OnSwipe(InputHandler.Swipe s)
        {
            if (state != State.Running) return;
            switch (s)
            {
                case InputHandler.Swipe.Left: player.MoveLane(-1); break;
                case InputHandler.Swipe.Right: player.MoveLane(1); break;
                case InputHandler.Swipe.Up: player.Jump(); break;
                case InputHandler.Swipe.Down: player.Slide(); break;
            }
        }

        void OnCoinCollected(Vector3 pos)
        {
            runCoins++;
            AudioManager.I.Play(Sfx.Coin, 1f + Mathf.Min(0.5f, runCoins % 8 * 0.04f));
            Fx.Burst(coinFx, pos, 6, new Color(1f, 0.85f, 0.25f, 1f), 2.2f, 0.16f);
        }

        // ============================================================ цикл
        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 camPos = cam.transform.position;
            ambient.transform.position = new Vector3(camPos.x, 3f, camPos.z + 28f);

            switch (state)
            {
                case State.Menu:
                case State.Shop:
                    menuSpeed = GameConfig.MenuSpeed;
                    world.Tick(dt, menuSpeed, false);
                    player.Tick(dt, menuSpeed);
                    Fx.ScrollBack(ambient, menuSpeed * 0.9f);
                    break;

                case State.Running:
                    TickRun(dt);
                    break;

                case State.Countdown:
                    {
                        // после паузы: 3-2-1, мир стоит
                        Time.timeScale = 1f;
                        float rdt = Time.unscaledDeltaTime;
                        resumeCountdown -= rdt;
                        ui.SetCountdown(resumeCountdown > 0 ? Mathf.CeilToInt(resumeCountdown).ToString() : "");
                        player.Tick(0f, 0f);
                        if (resumeCountdown <= 0f)
                        {
                            ui.SetCountdown("");
                            state = State.Running;
                            input.Active = true;
                        }
                    }
                    break;

                case State.Dead:
                    deadTimer += dt;
                    float slow = Mathf.Clamp01(1f - deadTimer / 0.7f);
                    world.Tick(dt, speed * slow * slow, false);
                    player.Tick(dt, 0f);
                    if (deadTimer > 1.1f) ShowResults();
                    break;
            }
        }

        void TickRun(float dt)
        {
            var th = Theme;
            float maxSpd = GameConfig.MaxSpeed + th.speedBonus * 0.6f;
            runTime += dt;
            speed = Mathf.Min(maxSpd, GameConfig.StartSpeed + th.speedBonus + GameConfig.SpeedGain * runTime);
            distance += speed * dt;
            camRig.SetSpeedT((speed - GameConfig.StartSpeed) / Mathf.Max(1f, maxSpd - GameConfig.StartSpeed));
            Fx.ScrollBack(ambient, speed * 0.9f);

            world.Tick(dt, speed, true);
            player.Tick(dt, speed);

            world.CollectCoins(player);
            Hit h = world.CheckHit(player);
            if (h != Hit.None) { Crash(h == Hit.Fall); return; }

            ui.UpdateHud(Mathf.FloorToInt(distance), runCoins);
        }

        void Crash(bool fall)
        {
            state = State.Dead;
            deadTimer = 0f;
            input.Active = false;
            player.Die(fall);
            AudioManager.I.Play(Sfx.Crash);
            AudioManager.Vibrate();
            camRig.Shake(0.45f);
            Fx.Burst(crashFx, player.transform.position + new Vector3(player.X, 0.8f, 0), 24, new Color(1f, 0.6f, 0.25f, 1f), 5f, 0.3f);
        }

        void ShowResults()
        {
            state = State.GameOver;
            var d = SaveSystem.Data;
            int dist = Mathf.FloorToInt(distance);
            int score = dist + runCoins * GameConfig.CoinScore;
            bool isNew = score > d.bestScore;
            if (isNew) d.bestScore = score;
            if (dist > d.bestDistance) d.bestDistance = dist;
            int before = d.unlockedLevel;
            SaveSystem.AddCoins(runCoins);
            SaveSystem.RecomputeUnlocked();
            SaveSystem.Commit();
            ui.ShowGameOver(score, dist, runCoins, d.bestScore, isNew);
            if (isNew) AudioManager.I.Play(Sfx.NewRecord);
            if (d.unlockedLevel > before) ui.Toast("Открыт новый уровень: " + Themes.All[d.unlockedLevel].name + "!");
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                if (state == State.Running) Pause();
                SaveSystem.Commit();
            }
        }

        void OnApplicationQuit() { SaveSystem.Commit(); }
    }

    /// <summary>Создаёт игру автоматически при старте любой сцены.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindObjectOfType<GameController>() != null) return;
            var go = new GameObject("TilkayoGame");
            go.AddComponent<GameController>();
        }
    }
}
