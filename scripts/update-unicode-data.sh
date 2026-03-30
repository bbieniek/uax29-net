#!/usr/bin/env bash
set -euo pipefail

# Updates Unicode emoji-data.txt used by the source generator.
# Usage:
#   ./scripts/update-unicode-data.sh            # defaults to 14.0.0
#   ./scripts/update-unicode-data.sh 15.1.0     # specific Unicode version

UNICODE_VERSION="${1:-14.0.0}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
TARGET_FILE="${REPO_ROOT}/src/Uax29.Net/UnicodeData/emoji-data.txt"
TEMP_FILE="$(mktemp)"
URL="https://unicode.org/Public/${UNICODE_VERSION}/ucd/emoji/emoji-data.txt"

cleanup() {
    rm -f "${TEMP_FILE}"
}
trap cleanup EXIT

echo "Downloading ${URL}"
curl -fLsS "${URL}" -o "${TEMP_FILE}"

if [[ ! -s "${TEMP_FILE}" ]]; then
    echo "Downloaded file is empty: ${URL}" >&2
    exit 1
fi

mkdir -p "$(dirname "${TARGET_FILE}")"
cp "${TEMP_FILE}" "${TARGET_FILE}"

echo "Updated ${TARGET_FILE}"
echo "Unicode version: ${UNICODE_VERSION}"
wc -l "${TARGET_FILE}" | awk '{print "Line count: "$1}'
