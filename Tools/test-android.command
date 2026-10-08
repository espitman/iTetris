#!/bin/zsh
set -e
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
TEST_SERIAL="${1:?Usage: Tools/test-android.command DEVICE_SERIAL}"
ADB_BIN="${ITETRIS_ADB_PATH:-/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb}"
"$ADB_BIN" -s "$TEST_SERIAL" install -r "$PROJECT_DIR/Builds/Android/iTetrisMobile.apk"
"$ADB_BIN" -s "$TEST_SERIAL" shell am force-stop com.itetris.mobile
"$ADB_BIN" -s "$TEST_SERIAL" logcat -c
"$ADB_BIN" -s "$TEST_SERIAL" shell am start -W -n com.itetris.mobile/com.unity3d.player.UnityPlayerGameActivity --es crystalTest smoke
for attempt in {1..60}; do
  if ! "$ADB_BIN" -s "$TEST_SERIAL" logcat -d -s Unity > "$PROJECT_DIR/Documentation/AndroidSmokeTest.log"; then
    sleep 2
    continue
  fi
  if rg -q 'SMOKE FAILED|Failed to store screen shot|NullReferenceException|ArgumentException' "$PROJECT_DIR/Documentation/AndroidSmokeTest.log"; then
    cat "$PROJECT_DIR/Documentation/AndroidSmokeTest.log"
    exit 1
  fi
  if rg -q 'ALL RUNTIME SMOKE TESTS PASSED' "$PROJECT_DIR/Documentation/AndroidSmokeTest.log"; then
    "$ADB_BIN" -s "$TEST_SERIAL" pull "/sdcard/Android/data/com.itetris.mobile/files/AndroidTetris.png" "$PROJECT_DIR/Documentation/iTetrisMobile.png"
    for screen in MobileHome MobileHomeReference MobilePause MobilePauseSoundOff MobileGameOver MobileGameOverReference MobileGameOverDigits MobileLaunch MobileHelp; do
      "$ADB_BIN" -s "$TEST_SERIAL" pull "/sdcard/Android/data/com.itetris.mobile/files/$screen.png" "$PROJECT_DIR/Documentation/$screen.png"
    done
    print 'ANDROID_RUNTIME_TESTS_PASSED'
    exit 0
  fi
  sleep 2
done
print -u2 'Android checks timed out; inspect Documentation/AndroidSmokeTest.log.'
exit 1
