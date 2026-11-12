#!/usr/bin/env bash
# Сборка APK из командной строки. Укажи путь к Unity через переменную UNITY.
# Пример: UNITY="$HOME/Unity/Hub/Editor/2022.3.20f1/Editor/Unity" ./build.sh
set -e
UNITY="${UNITY:-unity-editor}"
"$UNITY" -batchmode -quit -nographics -projectPath "$(pwd)" -buildTarget Android \
  -executeMethod TilkayoRun.EditorTools.BuildScript.BuildApk -logFile build.log
echo "Готово: Builds/TilkayoRun.apk"
