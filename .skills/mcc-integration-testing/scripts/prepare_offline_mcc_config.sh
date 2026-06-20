#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
# shellcheck source=.skills/mcc-integration-testing/scripts/common.sh
source "$SCRIPT_DIR/common.sh"

usage() {
    cat <<'EOF' >&2
Usage:
  prepare_offline_mcc_config.sh <output-ini> <mc-version> [login]
  prepare_offline_mcc_config.sh <template-ini> <output-ini> <mc-version> [login]
EOF
}

if [[ $# -lt 2 || $# -gt 4 ]]; then
    usage
    exit 1
fi

TEMPLATE_INI=""
OUTPUT_INI=""
MC_VERSION=""
LOGIN_NAME=""

if [[ $# -ge 3 && -f "$1" ]]; then
    TEMPLATE_INI="$1"
    OUTPUT_INI="$2"
    MC_VERSION="$3"
    LOGIN_NAME="${4:-MCCBot}"
else
    OUTPUT_INI="$1"
    MC_VERSION="$2"
    LOGIN_NAME="${3:-MCCBot}"
fi

ACCOUNT_TYPE="${MCC_TEST_ACCOUNT_TYPE:-mojang}"
PASSWORD_VALUE="${MCC_TEST_PASSWORD-}"

if [[ "$ACCOUNT_TYPE" != "mojang" && "$ACCOUNT_TYPE" != "microsoft" && "$ACCOUNT_TYPE" != "yggdrasil" ]]; then
    echo "Unsupported MCC_TEST_ACCOUNT_TYPE: $ACCOUNT_TYPE" >&2
    exit 1
fi

if [[ -z "${MCC_TEST_PASSWORD+x}" ]]; then
    if [[ "$ACCOUNT_TYPE" == "mojang" ]]; then
        PASSWORD_VALUE="-"
    else
        PASSWORD_VALUE=""
    fi
fi

generate_template_ini() {
    local template_root
    template_root="$(mktemp -d "${TMPDIR:-/tmp}/mcc-config-template.XXXXXX")"

    if [[ ! -f "$REPO_ROOT/MinecraftClient/bin/Release/net10.0/MinecraftClient.dll" ]]; then
        dotnet build "$REPO_ROOT/MinecraftClient.sln" -c Release -v quiet --nologo >/dev/null
    fi

    (
        cd "$template_root"
        dotnet run --project "$REPO_ROOT/MinecraftClient" -c Release --no-build -- --help >/dev/null 2>&1
    )

    if [[ ! -f "$template_root/MinecraftClient.ini" ]]; then
        echo "Failed to generate a temporary MCC config template." >&2
        exit 1
    fi

    TEMPLATE_INI="$template_root/MinecraftClient.ini"
}

if [[ -z "$TEMPLATE_INI" ]]; then
    generate_template_ini
fi

mkdir -p "$(dirname "$OUTPUT_INI")"
cp "$TEMPLATE_INI" "$OUTPUT_INI"

sed_in_place \
    -e "s#^Account = .*#Account = { Login = \"$LOGIN_NAME\", Password = \"$PASSWORD_VALUE\" }#" \
    -e "s#^AccountType = .*#AccountType = \"$ACCOUNT_TYPE\"#" \
    -e "s#^MinecraftVersion = \"[^\"]*\"\\(.*\\)\$#MinecraftVersion = \"$MC_VERSION\"\\1#" \
    -e 's#^TerrainAndMovements = false#TerrainAndMovements = true#' \
    -e 's#^InventoryHandling = false#InventoryHandling = true#' \
    -e 's#^EntityHandling = false#EntityHandling = true#' \
    -e 's#^AutoRespawn = false#AutoRespawn = true#' \
    "$OUTPUT_INI"

disable_noisy_bots_in_ini "$OUTPUT_INI"

grep -Fq "AccountType = \"$ACCOUNT_TYPE\"" "$OUTPUT_INI" || {
    echo "Failed to enforce account type $ACCOUNT_TYPE in $OUTPUT_INI" >&2
    exit 1
}

if [[ "$ACCOUNT_TYPE" == "mojang" ]]; then
    grep -Eq '^Account = \{ Login = ".*", Password = "-" \}' "$OUTPUT_INI" || {
        echo "Failed to enforce offline account in $OUTPUT_INI" >&2
        exit 1
    }
fi

printf '%s\n' "$OUTPUT_INI"
