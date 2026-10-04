#!/bin/zsh
set -e
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_BIN="/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_BIN" -batchmode -nographics -projectPath "$PROJECT_DIR" -buildTarget WebGL -executeMethod iTetris.Editor.ProjectBuilder.BuildWeb -quit -logFile "$PROJECT_DIR/Documentation/WebBuild.log"
