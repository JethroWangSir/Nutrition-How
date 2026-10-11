#!/bin/zsh
set -euo pipefail

SCRIPT_DIR="${0:A:h}"
PROJECT_ROOT="${SCRIPT_DIR:h}"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity}"
OUTPUT_DIR="${1:-${PROJECT_ROOT}/Builds/macOS}"
APP_PATH="${OUTPUT_DIR}/營養 How 解.app"
DMG_PATH="${OUTPUT_DIR}/營養-How-解-macOS-arm64.dmg"
LOG_PATH="${OUTPUT_DIR}/Nutrition-How-build.log"

mkdir -p "${OUTPUT_DIR}"
rm -rf "${APP_PATH}"
rm -f "${DMG_PATH}"

NUTRITION_HOW_BUILD_PATH="${APP_PATH}" "${UNITY_EDITOR}" \
  -batchmode \
  -projectPath "${PROJECT_ROOT}" \
  -executeMethod NutritionHowBuild.BuildMacArm64 \
  -logFile "${LOG_PATH}" \
  -quit

EXECUTABLE="${APP_PATH}/Contents/MacOS/營養 How 解"
ARCHITECTURES="$(lipo -archs "${EXECUTABLE}")"
if [[ "${ARCHITECTURES}" != "arm64" ]]; then
  print -u2 "Unexpected architectures: ${ARCHITECTURES}"
  exit 1
fi

codesign --verify --deep --strict "${APP_PATH}"

hdiutil create \
  -volname "營養 How 解" \
  -srcfolder "${APP_PATH}" \
  -format UDZO \
  -ov \
  "${DMG_PATH}"

hdiutil verify "${DMG_PATH}"
shasum -a 256 "${DMG_PATH}"
