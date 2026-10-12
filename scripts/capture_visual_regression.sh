#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h}"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity}"
OUTPUT_DIR="${1:-/tmp/nutrition-how-visual-regression}"
APP_PATH="${OUTPUT_DIR}/營養 How 解 Visual Validation.app"
BUILD_LOG="${OUTPUT_DIR}/build.log"

mkdir -p "${OUTPUT_DIR}"
rm -rf "${APP_PATH}"

NUTRITION_HOW_BUILD_PATH="${APP_PATH}" "${UNITY_EDITOR}" \
  -batchmode \
  -projectPath "${PROJECT_ROOT}" \
  -executeMethod NutritionHowBuild.BuildMacArm64VisualValidation \
  -logFile "${BUILD_LOG}" \
  -quit

EXECUTABLE="${APP_PATH}/Contents/MacOS/營養 How 解"
[[ "$(lipo -archs "${EXECUTABLE}")" == "arm64" ]]
codesign --verify --deep --strict "${APP_PATH}"

for resolution in 1024x768 1280x800 1920x1080; do
  width="${resolution%x*}"
  height="${resolution#*x}"
  capture_dir="${OUTPUT_DIR}/${resolution}"
  player_log="${capture_dir}/player.log"
  mkdir -p "${capture_dir}"
  rm -f "${capture_dir}"/*.png(N) "${capture_dir}/capture-manifest.json" "${player_log}"

  "${EXECUTABLE}" \
    --nutrition-how-visual-capture \
    --capture-output "${capture_dir}" \
    -screen-fullscreen 0 \
    -screen-width "${width}" \
    -screen-height "${height}" \
    -logFile "${player_log}"

  [[ -s "${capture_dir}/capture-manifest.json" ]]
  [[ "$(find "${capture_dir}" -maxdepth 1 -name '*.png' | wc -l | tr -d ' ')" == "23" ]]
done

print "Visual regression captures: ${OUTPUT_DIR}"
