# Тилкайо run

Оригинальный 3D-раннер для Android (Unity 2022.3 LTS). Три полосы, свайпы, монеты, 30 скинов, кейсы с рулеткой,
4 уровня (закат, ночь, рассвет, осень), процедурные звуки и музыка. Без рекламы, без покупок, без интернета.

- Package: `com.sedr.tilkayorun`  ·  Ориентация: портрет  ·  Проект: 720×1280  ·  Язык: русский
- Весь код — открытый (MIT). Все ассеты процедурные, кроме спрайта Тилкайо (`Assets/Resources/tilkayo.png`).

## Открыть проект
1. Unity Hub → Add → папка `TilkayoRun`. Нужна версия 2022.3 LTS (подойдёт любая 2022.3.x) с модулями
   **Android Build Support** (+ OpenJDK и Android SDK & NDK Tools).
2. При первом открытии скрипт `Assets/Editor/ProjectSetup.cs` сам настроит PlayerSettings и создаст
   `Assets/Scenes/Main.unity`. Если нет — меню **Тилкайо → Применить настройки проекта**.
3. Нажми Play. Весь мир, интерфейс и звук создаются из кода (`Bootstrap` → `GameController`).

Управление в редакторе: A/D или ←/→ — полосы, Пробел/↑/W — прыжок, ↓/S — скольжение; мышью можно свайпать.

## Сборка APK
- В редакторе: **Тилкайо → Собрать APK** → `Builds/TilkayoRun.apk` (для Play Market: «Собрать AAB»).
- Из консоли:
  ```
  UNITY="/путь/к/Unity" ./build.sh                 # Linux/macOS
  Unity -batchmode -quit -nographics -projectPath . -buildTarget Android \
        -executeMethod TilkayoRun.EditorTools.BuildScript.BuildApk -logFile build.log
  ```
- Установка на телефон: `adb install -r Builds/TilkayoRun.apk`.
- Для публикации создай свой keystore: Edit → Project Settings → Player → Android → Publishing Settings.

## Структура
```
Assets/
  Resources/tilkayo.png            спрайт персонажа (вырезан из фото, tools/make_sprite.py)
  Resources/Shaders/               World (изгиб мира, туман, покачивание), Sprite (перекраска скинов), Soft, Glow
  Resources/Audio/                 сюда можно класть свои звуки/музыку (см. README.txt внутри)
  Scripts/Core/                    GameConfig (константы, темы), SaveSystem, SkinDatabase (30 скинов, кейсы)
  Scripts/Audio/                   AudioManager, ProceduralAudio (синтез SFX и музыки)
  Scripts/Gfx/                     MeshBuilder, WorldMeshes (лес/препятствия/небо), Accessories, Fx
  Scripts/Game/                    GameController, PlayerController, WorldRunner, CameraRig, InputHandler,
                                   SkinVisual, SkinPreview
  Scripts/UI/                      UIKit, UIManager (меню/HUD/пауза/итоги/настройки), ShopUI (скины и кейсы)
  Editor/                          ProjectSetup (автонастройка), BuildScript (сборка APK/AAB)
```

## Как это устроено
- **Персонаж**: 2D-спрайт на 3D-квадрате. Скины = поворот оттенка/яркость/насыщенность в шейдере + контур-свечение +
  3D-аксессуар (шляпы, очки, корона…) + аура частиц. Анимация (бег, прыжок, скольжение, крен) — процедурная.
- **Дорога**: игрок стоит на месте, мир едет навстречу; 7 сегментов, 36 препятствий и 90 монет переиспользуются из пулов.
- **Честность**: в каждом ряду есть полоса без стены; интервал между рядами не меньше ~1 с, а частота рядов растёт со скоростью.
- **Препятствия**: бревно/камень/разрыв — прыжок; низкая ветка — скольжение; валун/сломанное дерево/дерево — обойти.
- **Очки** = метры + 5 × монеты. Уровни открываются по лучшему пройденному расстоянию (0 / 600 / 1500 / 3000 м).
- **Кейсы**: 300 / 900 / 2500 монет, шансы показаны на карточках. Дубликат возвращает монеты (60/120/300/800).
- **Сохранения**: `PlayerPrefs` (JSON): рекорд, монеты, скины, настройки, уровень. Сеть не используется.
- **Качество**: низкое — 30 FPS, 540p, меньше частиц/деревьев; среднее — 60 FPS, 720p; высокое — 60 FPS, до 1080p + MSAA.

## Свои звуки и шрифт
Положи файлы в `Assets/Resources/Audio/` (имена в README.txt рядом) или `Assets/Resources/Fonts/main.ttf` — они заменят встроенные.
