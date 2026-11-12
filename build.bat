@echo off
rem Пример: set UNITY="C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe"
if "%UNITY%"=="" set UNITY="C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe"
%UNITY% -batchmode -quit -nographics -projectPath "%cd%" -buildTarget Android -executeMethod TilkayoRun.EditorTools.BuildScript.BuildApk -logFile build.log
echo Готово: Builds\TilkayoRun.apk
