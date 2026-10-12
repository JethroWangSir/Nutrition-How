#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h}"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity}"
OUTPUT_DIR="${1:-/tmp/nutrition-how-benchmark}"
APP_PATH="${OUTPUT_DIR}/營養 How 解 Development.app"
REPORT_PATH="${OUTPUT_DIR}/performance-benchmark.json"
BUILD_LOG="${OUTPUT_DIR}/build.log"
PLAYER_LOG="${OUTPUT_DIR}/player.log"

mkdir -p "${OUTPUT_DIR}"
rm -rf "${APP_PATH}"
rm -f "${REPORT_PATH}" "${BUILD_LOG}" "${PLAYER_LOG}"

NUTRITION_HOW_BUILD_PATH="${APP_PATH}" "${UNITY_EDITOR}" \
  -batchmode \
  -projectPath "${PROJECT_ROOT}" \
  -executeMethod NutritionHowBuild.BuildMacArm64Development \
  -logFile "${BUILD_LOG}" \
  -quit

EXECUTABLE="${APP_PATH}/Contents/MacOS/營養 How 解"
[[ "$(lipo -archs "${EXECUTABLE}")" == "arm64" ]]
codesign --verify --deep --strict "${APP_PATH}"

"${EXECUTABLE}" \
  --nutrition-how-benchmark \
  --benchmark-output "${REPORT_PATH}" \
  --benchmark-passes 2 \
  -screen-fullscreen 0 \
  -screen-width 1280 \
  -screen-height 800 \
  -logFile "${PLAYER_LOG}"

[[ -s "${REPORT_PATH}" ]]
print "Benchmark report: ${REPORT_PATH}"
