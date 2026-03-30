#!/usr/bin/env bash
set -euo pipefail

sed_in_place() {
    if [[ "$(uname)" == "Darwin" ]]; then
        sed -i '' "$@"
    else
        sed -i "$@"
    fi
}

if [[ $# -lt 3 || $# -gt 4 ]]; then
    echo "Usage: $0 <template-ini> <output-ini> <mc-version> [login]" >&2
    exit 1
fi

TEMPLATE_INI="$1"
OUTPUT_INI="$2"
MC_VERSION="$3"
LOGIN_NAME="${4:-CursorBot}"
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
