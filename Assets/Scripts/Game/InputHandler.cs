using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TilkayoRun
{
    /// <summary>Свайпы на телефоне + клавиатура/мышь в редакторе.</summary>
    public class InputHandler : MonoBehaviour
    {
        public enum Swipe { Left, Right, Up, Down }

        public Action<Swipe> onSwipe;
        public bool Active;

        Vector2 start;
        bool tracking;
        int fingerId = -1;
        bool mouseTracking;

        float Threshold { get { return Mathf.Max(36f, Screen.height * 0.03f); } }

        void Update()
        {
            if (!Active) { tracking = false; mouseTracking = false; return; }

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) Fire(Swipe.Left);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) Fire(Swipe.Right);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Fire(Swipe.Up);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Fire(Swipe.Down);

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) break;
                        tracking = true; fingerId = t.fingerId; start = t.position;
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (tracking && t.fingerId == fingerId) Evaluate(t.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (t.fingerId == fingerId) tracking = false;
                        break;
                }
            }

            // мышь (тест в редакторе)
            if (!Input.touchSupported)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                    mouseTracking = true; start = Input.mousePosition;
                }
                else if (Input.GetMouseButton(0) && mouseTracking) Evaluate(Input.mousePosition);
                if (Input.GetMouseButtonUp(0)) mouseTracking = false;
            }
        }

        void Evaluate(Vector2 now)
        {
            Vector2 d = now - start;
            if (d.magnitude < Threshold) return;
            Swipe s;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) s = d.x > 0 ? Swipe.Right : Swipe.Left;
            else s = d.y > 0 ? Swipe.Up : Swipe.Down;
            start = now;     // можно делать несколько свайпов одним движением пальца
            Fire(s);
        }

        void Fire(Swipe s) { if (onSwipe != null) onSwipe(s); }
    }
}
