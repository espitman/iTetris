#!/bin/zsh
set -e
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_BIN="/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_BIN" -batchmode -nographics -projectPath "$PROJECT_DIR" -executeMethod iTetris.Editor.ProjectBuilder.BuildMac -quit -logFile "$PROJECT_DIR/Documentation/Build.log"
open "$PROJECT_DIR/Builds"
