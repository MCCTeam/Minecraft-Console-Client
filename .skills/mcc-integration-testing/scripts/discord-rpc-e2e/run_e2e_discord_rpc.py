#!/usr/bin/env python3
"""
End-to-end test orchestrator for Discord RPC ChatBot.
Starts all components, waits for MCC to connect, and verifies RPC presence is sent.
"""
import os
import signal
import subprocess
import sys
import time

MCC_DIR = "/home/runner/work/Minecraft-Console-Client/Minecraft-Console-Client"
TEST_DIR = "/tmp/e2e-test"
MC_LOG = f"{TEST_DIR}/fake_mc_server.log"
RPC_LOG = f"{TEST_DIR}/discord_rpc.log"
MCC_LOG = f"{TEST_DIR}/mcc_output.log"


def log(msg):
    print(f"\033[1;36m[E2E-TEST]\033[0m {msg}", flush=True)


def wait_for_log(log_file, marker, timeout=30, label=""):
    """Wait for a specific string to appear in a log file."""
    start = time.time()
    while time.time() - start < timeout:
        try:
            with open(log_file, "r") as f:
                content = f.read()
                if marker in content:
                    return True
        except FileNotFoundError:
            pass
        time.sleep(0.5)
    log(f"TIMEOUT waiting for '{marker}' in {label or log_file}")
    return False


def read_log(log_file):
    try:
        with open(log_file, "r") as f:
            return f.read()
    except FileNotFoundError:
        return ""


def main():
    os.makedirs(TEST_DIR, exist_ok=True)

    pids = []

    # Clean up old files
    for f in [MC_LOG, RPC_LOG, MCC_LOG]:
        if os.path.exists(f):
            os.remove(f)

    # Remove any leftover mcc_input.txt
    input_file = os.path.join(MCC_DIR, "mcc_input.txt")
    if os.path.exists(input_file):
        os.remove(input_file)

    try:
        # -- Step 1: Start fake Discord IPC --
        log("Starting fake Discord IPC server...")
        discord_proc = subprocess.Popen(
            [sys.executable, f"{TEST_DIR}/fake_discord_ipc.py"],
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            env={**os.environ, "XDG_RUNTIME_DIR": "/tmp"}
        )
        pids.append(discord_proc.pid)
        time.sleep(1)

        if not wait_for_log(RPC_LOG, "Fake Discord IPC listening", timeout=5, label="Discord IPC"):
            log("FAIL: Discord IPC server did not start")
            return 1
        log("  OK: Discord IPC server running")

        # -- Step 2: Start fake Minecraft server --
        log("Starting fake Minecraft server...")
        mc_proc = subprocess.Popen(
            [sys.executable, f"{TEST_DIR}/fake_mc_server.py"],
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT
        )
        pids.append(mc_proc.pid)
        time.sleep(1)

        if not wait_for_log(MC_LOG, "Fake MC Server listening", timeout=5, label="MC Server"):
            log("FAIL: MC server did not start")
            return 1
        log("  OK: Minecraft server running on 127.0.0.1:25565")

        # -- Step 3: Create MCC configuration --
        log("Creating MCC configuration...")
        ini_path = os.path.join(MCC_DIR, "MinecraftClient.ini")
        with open(ini_path, "w") as f:
            f.write("""
[Main]
[Main.General]
Account = { Login = "RpcTestBot", Password = "-" }
ServerIP = "127.0.0.1:25565"
MinecraftVersion = "1.20.1"

[Main.Advanced]
Language = "en_us"
EnableSentry = false

[Logging]
DebugMessages = true

[ChatBot]
[ChatBot.DiscordRpc]
Enabled = true
ApplicationId = "123456789012345678"
PresenceDetails = "Playing on {server_host}:{server_port}"
PresenceState = "{dimension} - HP: {health}/{max_health}"
LargeImageKey = "mcc_icon"
LargeImageText = "Minecraft Console Client"
SmallImageKey = ""
SmallImageText = ""
ShowServerAddress = true
ShowCoordinates = true
ShowHealth = true
ShowDimension = true
ShowGamemode = true
ShowElapsedTime = true
ShowPlayerCount = true
UpdateIntervalSeconds = 5
""")
        log("  OK: MinecraftClient.ini created")

        # -- Step 4: Start MCC --
        log("Starting MCC...")
        mcc_env = {
            **os.environ,
            "MCC_FILE_INPUT": "1",
            "XDG_RUNTIME_DIR": "/tmp"  # So DiscordRPC library finds our fake IPC socket
        }

        mcc_proc = subprocess.Popen(
            ["dotnet", "run", "--project", "MinecraftClient", "-c", "Release",
             "--no-build", "--", "RpcTestBot", "-", "127.0.0.1:25565"],
            cwd=MCC_DIR,
            stdout=open(MCC_LOG, "w"),
            stderr=subprocess.STDOUT,
            env=mcc_env
        )
        pids.append(mcc_proc.pid)

        # -- Step 5: Wait for MCC to join the server --
        log("Waiting for MCC to connect to server...")
        mc_joined = wait_for_log(MC_LOG, "has logged in successfully", timeout=30, label="MC join")

        if mc_joined:
            log("  OK: MCC connected to fake Minecraft server")
        else:
            log("  WARN: Could not confirm MC join in server log")
            # Check MCC log for more info
            mcc_content = read_log(MCC_LOG)
            if "Server was successfully joined" in mcc_content:
                log("  OK: MCC reports successful join in its own log")
                mc_joined = True

        # -- Step 6: Wait for Discord RPC presence --
        log("Waiting for Discord RPC presence update...")
        # Give MCC time to initialize the Discord RPC bot and set presence
        rpc_activity = wait_for_log(RPC_LOG, "SET_ACTIVITY received", timeout=30, label="RPC activity")

        if rpc_activity:
            log("  OK: Discord RPC presence received!")
        else:
            log("  INFO: Checking if RPC client attempted connection...")
            rpc_content = read_log(RPC_LOG)
            if "HANDSHAKE received" in rpc_content:
                log("  OK: RPC handshake succeeded, waiting longer for activity...")
                rpc_activity = wait_for_log(RPC_LOG, "SET_ACTIVITY received", timeout=20, label="RPC activity (extended)")
            elif "Client connected" in rpc_content:
                log("  PARTIAL: RPC client connected but no activity sent yet")

        # -- Step 7: Collect and display results --
        log("")
        log("=" * 60)
        log("END-TO-END TEST RESULTS")
        log("=" * 60)

        # Check all criteria
        mc_server_ok = "Fake MC Server listening" in read_log(MC_LOG)
        discord_ipc_ok = "Fake Discord IPC listening" in read_log(RPC_LOG)
        mcc_content = read_log(MCC_LOG)
        rpc_content = read_log(RPC_LOG)

        mcc_connected = "has logged in successfully" in read_log(MC_LOG) or "Server was successfully joined" in mcc_content
        rpc_handshake = "HANDSHAKE received" in rpc_content
        rpc_ready = "Sent READY response" in rpc_content
        rpc_presence = "SET_ACTIVITY received" in rpc_content

        # Extract presence details from RPC log
        presence_details = ""
        presence_state = ""
        for line in rpc_content.split("\n"):
            if "Details :" in line:
                presence_details = line.split("Details :")[1].strip()
            if "State   :" in line:
                presence_state = line.split("State   :")[1].strip()

        results = [
            ("Fake MC Server started",     mc_server_ok),
            ("Fake Discord IPC started",   discord_ipc_ok),
            ("MCC connected to server",    mcc_connected),
            ("RPC handshake completed",    rpc_handshake),
            ("RPC READY sent to client",   rpc_ready),
            ("RPC presence set",           rpc_presence),
        ]

        all_pass = True
        for label, ok in results:
            status = "\033[1;32mPASS\033[0m" if ok else "\033[1;31mFAIL\033[0m"
            log(f"  [{status}] {label}")
            if not ok:
                all_pass = False

        if presence_details:
            log(f"\n  Presence Details: {presence_details}")
        if presence_state:
            log(f"  Presence State  : {presence_state}")

        log("")

        # Print relevant logs
        log("--- MCC Output (last 30 lines) ---")
        mcc_lines = mcc_content.strip().split("\n")
        for line in mcc_lines[-30:]:
            log(f"  {line}")

        log("")
        log("--- Discord RPC Log ---")
        rpc_lines = rpc_content.strip().split("\n")
        for line in rpc_lines:
            log(f"  {line}")

        log("")
        log("--- MC Server Log ---")
        mc_content = read_log(MC_LOG)
        mc_lines = mc_content.strip().split("\n")
        for line in mc_lines:
            log(f"  {line}")

        log("")

        if all_pass:
            log("\033[1;32m*** ALL TESTS PASSED - Discord RPC integration is fully working! ***\033[0m")
            return 0
        else:
            log("\033[1;33m*** SOME TESTS DID NOT PASS ***\033[0m")
            return 1

    finally:
        # Clean up all processes
        log("\nCleaning up processes...")
        for pid in pids:
            try:
                os.kill(pid, signal.SIGTERM)
            except ProcessLookupError:
                pass

        time.sleep(1)

        for pid in pids:
            try:
                os.kill(pid, signal.SIGKILL)
            except ProcessLookupError:
                pass

        # Clean up config
        ini_path = os.path.join(MCC_DIR, "MinecraftClient.ini")
        if os.path.exists(ini_path):
            os.remove(ini_path)


if __name__ == "__main__":
    sys.exit(main())
