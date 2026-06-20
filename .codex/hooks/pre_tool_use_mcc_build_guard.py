#!/usr/bin/env python3
import json
import re
import sys


RAW_DOTNET_BUILD_RE = re.compile(r"(^|[\s;&|()])dotnet\s+build(\s|$)")
ABSOLUTE_DOTNET_BUILD_RE = re.compile(r"(^|[\s;&|()])/\S*dotnet\s+build(\s|$)")
RAW_DOTNET_PUBLISH_RE = re.compile(r"(^|[\s;&|()])dotnet\s+publish(\s|$)")
ABSOLUTE_DOTNET_PUBLISH_RE = re.compile(r"(^|[\s;&|()])/\S*dotnet\s+publish(\s|$)")


def main() -> int:
    try:
        payload = json.load(sys.stdin)
    except json.JSONDecodeError:
        return 0

    command = payload.get("tool_input", {}).get("command", "")
    if not isinstance(command, str) or not command:
        return 0

    if ABSOLUTE_DOTNET_BUILD_RE.search(command) or ABSOLUTE_DOTNET_PUBLISH_RE.search(command):
        return 0

    if RAW_DOTNET_BUILD_RE.search(command):
        response = {
            "hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "deny",
                "permissionDecisionReason": (
                    "Raw 'dotnet build' is blocked in this repository. "
                    "Use 'source tools/mcc-env.sh && mcc-build' instead so MCC temp-build routing stays active. "
                    "If you intentionally need the raw .NET CLI, call it by absolute path such as '/usr/bin/dotnet build ...' to bypass this guard."
                ),
            },
            "systemMessage": (
                "Blocked raw 'dotnet build'. Use 'source tools/mcc-env.sh && mcc-build'. "
                "If you intentionally need raw .NET CLI behavior, call '/usr/bin/dotnet build ...' explicitly."
            ),
        }
        json.dump(response, sys.stdout)
        sys.stdout.write("\n")
    elif RAW_DOTNET_PUBLISH_RE.search(command):
        response = {
            "hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "deny",
                "permissionDecisionReason": (
                    "Raw 'dotnet publish' is blocked in this repository. "
                    "Use 'source tools/mcc-env.sh && mcc-publish --rid <RID>' instead so MCC publish defaults stay aligned with the repo workflow. "
                    "If you intentionally need the raw .NET CLI, call it by absolute path such as '/usr/bin/dotnet publish ...' to bypass this guard."
                ),
            },
            "systemMessage": (
                "Blocked raw 'dotnet publish'. Use 'source tools/mcc-env.sh && mcc-publish --rid <RID>'. "
                "If you intentionally need raw .NET CLI behavior, call '/usr/bin/dotnet publish ...' explicitly."
            ),
        }
        json.dump(response, sys.stdout)
        sys.stdout.write("\n")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
