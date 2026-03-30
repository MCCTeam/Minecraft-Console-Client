#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: summarize_achievements_matrix.sh <matrix-run-dir>" >&2
    exit 1
fi

MATRIX_DIR="$1"
RESULTS_TSV="$MATRIX_DIR/results.tsv"
PRECHECK_TXT="$MATRIX_DIR/preflight.txt"
BUILD_LOG="$MATRIX_DIR/build.log"

if [[ ! -f "$RESULTS_TSV" ]]; then
    echo "Missing results file: $RESULTS_TSV" >&2
    exit 1
fi

echo "# Achievements Matrix Report"
echo
echo "## Executed"
echo
if [[ -f "$PRECHECK_TXT" ]]; then
    echo '```text'
    cat "$PRECHECK_TXT"
    echo '```'
fi
echo
echo "- Matrix artifacts: \`$MATRIX_DIR\`"
echo "- Results TSV: \`$RESULTS_TSV\`"
echo "- Build log: \`$BUILD_LOG\`"
echo "- Execution mode: sequential"
echo "- Auth mode: offline"
echo
echo "## Observed"
echo
echo "| Version | Port | Family | Initial snapshot | Grant | Revoke | API callback | Verdict |"
echo "|---|---:|---|---|---|---|---|---|"
awk -F '\t' 'NR > 1 {
    printf("| `%s` | `%s` | %s | %s | %s | %s | %s | %s |\n",
        $1, $3, $4, $5, $6, $7, $8, $9);
}' "$RESULTS_TSV"

echo
echo "## Artifact Links"
echo
awk -F '\t' 'NR > 1 {
    printf("- `%s`: run=`%s`, mcc=`%s`, server=`%s`, commands=`%s`\n", $1, $11, $12, $13, $14);
    printf("  note: %s\n", $10);
}' "$RESULTS_TSV"

echo
echo "## Inferred"
echo
echo "- Only rows with real MCC and server-log artifacts count as executed proof."
echo "- Rows blocked by missing Java, tmux, or server directories are environment-limited, not product pass results."
echo "- Legacy rows remain the highest-risk bucket because static inspection suggests pre-1.12 \`Statistics\` packets may not currently reach the achievements handler."
