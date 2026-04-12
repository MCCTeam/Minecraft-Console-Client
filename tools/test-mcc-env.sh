#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"

assert_eq() {
    local expected="$1"
    local actual="$2"
    local label="$3"
    if [[ "$expected" != "$actual" ]]; then
        echo "FAIL: $label" >&2
        echo "  expected: $expected" >&2
        echo "  actual:   $actual" >&2
        exit 1
    fi
}

assert_regex() {
    local regex="$1"
    local actual="$2"
    local label="$3"
    if [[ ! "$actual" =~ $regex ]]; then
        echo "FAIL: $label" >&2
        echo "  regex:  $regex" >&2
        echo "  actual: $actual" >&2
        exit 1
    fi
}

tmpfs_build_base() {
    if [[ -d /dev/shm && -w /dev/shm ]]; then
        printf '/dev/shm'
    else
        printf '%s' "${TMPDIR:-/tmp}"
    fi
}

session="$(_mcc_resolve_session "demo-branch")"
assert_eq "demo-branch" "$session" "explicit session"

short_name="$(_mcc_resolve_username "feature-ai")"
assert_eq "mcc_feature_ai" "$short_name" "short derived username"

long_name="$(_mcc_resolve_username "very-long-worktree-name")"
assert_regex '^mcc_[a-z0-9_]{7}_[0-9a-f]{4}$' "$long_name" "long derived username shape"
assert_eq "16" "${#long_name}" "long derived username length"

assert_eq "${TMPDIR:-/tmp}/mcc-debug/demo-branch" "$(_mcc_session_root "demo-branch")" "session root"
assert_eq "mcc-demo-branch" "$(_mcc_tmux_session_name "demo-branch")" "tmux session name"

MCC_BUILD_MODE=tmpfs

original_repo_root="$MCC_REPO_ROOT"
fallback_root="$REPO_ROOT/nonexistent-worktree"
MCC_REPO_ROOT="$fallback_root"

fallback_session="$(_mcc_resolve_session)"
assert_eq "$(basename "$fallback_root")" "$fallback_session" "session fallback without git"

fallback_build_root="$(_mcc_build_root)"
tmpfs_base="$(tmpfs_build_base)"
expected_fallback_root="$tmpfs_base/mcc-build/$(basename "$fallback_root")"
assert_eq "$expected_fallback_root" "$fallback_build_root" "tmpfs build root fallback"

MCC_REPO_ROOT="$original_repo_root"

build_root="$(_mcc_build_root)"
expected_prefix="$(tmpfs_build_base)/mcc-build/"
if [[ "$build_root" != "$expected_prefix"* ]]; then
    echo "FAIL: tmpfs build root" >&2
    echo "  expected prefix: $expected_prefix" >&2
    echo "  actual: $build_root" >&2
    exit 1
fi

echo "PASS"
