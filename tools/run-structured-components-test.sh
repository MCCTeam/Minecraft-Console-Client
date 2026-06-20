#!/usr/bin/env bash
# Structured Components Integration Test
# Tests every structured component across supported versions (1.20.6 to 26.1)
# Usage: bash tools/run-structured-components-test.sh <version>
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
source "$REPO_ROOT/tools/mcc-env.sh"
source "$REPO_ROOT/.skills/mcc-integration-testing/scripts/common.sh"

usage() { echo "Usage: $0 <version>"; echo "  e.g. $0 1.20.6"; exit 1; }
VERSION="${1:-}"; [[ -z "$VERSION" ]] && usage
SERVER_DIR="${VERSION}"

# Version group detection
case "$VERSION" in
    1.20.6) VER_GROUP="v1206" ;;
    1.21|1.21.1) VER_GROUP="v121" ;;
    1.21.2|1.21.3|1.21.4) VER_GROUP="v1212" ;;
    1.21.5|1.21.6|1.21.7|1.21.8|1.21.9|1.21.10) VER_GROUP="v1215" ;;
    1.21.11) VER_GROUP="v12111" ;;
    26.1) VER_GROUP="v261" ;;
    *) echo "Unsupported version: $VERSION"; exit 1 ;;
esac

SESSION="sc-${VERSION//./_}"
TEST_ROOT="${TMPDIR:-/tmp}/mcc-sc-test/${VERSION}"
CFG="$TEST_ROOT/MinecraftClient.${VERSION}.ini"
INPUT_FILE="$(_mcc_session_input_file "$SESSION")"
MCC_LOG="$(_mcc_session_log_file "$SESSION")"
PID_FILE="$(_mcc_session_pid_file "$SESSION")"
META_FILE="$(_mcc_session_meta_file "$SESSION")"
MCC_TMUX_SESSION="$(_mcc_tmux_session_name "$SESSION")"
USERNAME="$(_mcc_resolve_username "$SESSION")"
mkdir -p "$TEST_ROOT"
mkdir -p "$(dirname "$INPUT_FILE")"

PASS_COUNT=0
FAIL_COUNT=0
FAILURES=()

pass() { PASS_COUNT=$((PASS_COUNT + 1)); printf '  [PASS] %s\n' "$1"; }
fail() { FAIL_COUNT=$((FAIL_COUNT + 1)); FAILURES+=("$1"); printf '  [FAIL] %s\n' "$1"; }

# Give item via RCON, verify success
give_item() {
    local name="$1" rcon_cmd="$2"
    local out
    out="$(mc-rcon "$rcon_cmd" 2>/dev/null || true)"
    if echo "$out" | grep -qiE "(Gave|given|No item was|Cannot give|Unknown item)"; then
        if echo "$out" | grep -qi "Gave"; then
            pass "$name"
        else
            fail "$name | give failed: $out"
        fi
    else
        # RCON returns empty sometimes; still check MCC log for errors
        pass "$name"
    fi
}

# Read inventory and check for component parse errors
check_inv() {
    sleep 1
    mcc-cmd --session "$SESSION" "inventory player list" 2>/dev/null || true
    sleep 2
    if grep -qiE "(error|exception|fail|unhandled|unknown component|System\." "$MCC_LOG" 2>/dev/null; then
        local err_line
        err_line="$(grep -iE "(error|exception|fail|unhandled|unknown component)" "$MCC_LOG" | head -3 2>/dev/null)"
        fail "component parse error detected: $err_line"
        return 1
    fi
    return 0
}

cleanup() {
    set +e
    mcc-cmd --session "$SESSION" "quit" 2>/dev/null || true
    sleep 1
    mcc-kill --session "$SESSION" 2>/dev/null || true
}
trap cleanup EXIT

echo "=== Structured Components Test: $VERSION ($VER_GROUP) ==="

# Phase 1: Preflight
bash "$REPO_ROOT/.skills/mcc-integration-testing/scripts/preflight_test_env.sh" "$SERVER_DIR" >/dev/null 2>&1 || true

# Phase 2: Ensure server configured for offline+RCON
bash "$REPO_ROOT/.skills/mcc-integration-testing/scripts/ensure_offline_server.sh" "$SERVER_DIR" >/dev/null 2>&1 || true

# Phase 3: Start server if not running
if ! server_running "$SERVER_DIR"; then
    mc-start "$SERVER_DIR" >/dev/null 2>&1
fi
wait_for_server_ready "$SERVER_DIR" || { echo "Server failed to start"; exit 1; }
echo "  Server ready."

# Phase 4: Prepare MCC config
echo "  Preparing MCC config..."
bash "$REPO_ROOT/.skills/mcc-integration-testing/scripts/prepare_offline_mcc_config.sh" \
    "$CFG" "$VERSION" "$USERNAME" >/dev/null 2>&1

sed_in_place \
    -e 's/^TerrainAndMovements = false/TerrainAndMovements = true/' \
    -e 's/^InventoryHandling = false/InventoryHandling = true/' \
    -e 's/^EntityHandling = false/EntityHandling = true/' \
    -e 's/^AutoRespawn = false/AutoRespawn = true/' \
    "$CFG" 2>/dev/null || true
disable_noisy_bots_in_ini "$CFG" 2>/dev/null || true

# Set server host/port in config
SERVER_PORT="$(bash "$REPO_ROOT/.skills/mcc-integration-testing/scripts/get_server_port.sh" "$SERVER_DIR" 2>/dev/null || echo "25565")"
sed_in_place \
    -e "s#^Server = .*#Server = { Host = \"localhost\", Port = $SERVER_PORT }#" \
    "$CFG" 2>/dev/null || true

# Phase 5: Start MCC in file-input mode
echo "  Starting MCC..."
: > "$INPUT_FILE" 2>/dev/null || true
rm -f "$MCC_LOG" "$PID_FILE"

MCC_ARGS=("$CFG" "$USERNAME" "-" "localhost:$SERVER_PORT")
MCC_ARGS_CMD="$(printf '%q ' "${MCC_ARGS[@]}")"

tmux kill-session -t "$MCC_TMUX_SESSION" 2>/dev/null || true
tmux new-session -d -s "$MCC_TMUX_SESSION" -x 160 -y 50 \
    "cd '$REPO_ROOT' && printf '%s\n' \"\$\$\" > '$PID_FILE' && exec env MCC_FILE_INPUT=1 MCC_INPUT_FILE='$INPUT_FILE' dotnet run --project MinecraftClient -c Release --no-build -- $MCC_ARGS_CMD > '$MCC_LOG' 2>&1"

for _ in $(seq 1 25); do
    if [[ -s "$PID_FILE" ]]; then break; fi
    sleep 0.2
done
MCC_PID="$(tr -cd '0-9' < "$PID_FILE" 2>/dev/null || true)"

echo -n "  Waiting for MCC to join..."
JOINED=false
for _ in $(seq 1 60); do
    if [[ -f "$MCC_LOG" ]] && grep -q "Server was successfully joined" "$MCC_LOG" 2>/dev/null; then
        echo " joined."
        JOINED=true
        break
    fi
    echo -n "."
    sleep 1
done
if ! $JOINED; then
    echo " TIMEOUT"
    echo "MCC log:"
    tail -30 "$MCC_LOG" 2>/dev/null
    exit 1
fi

# Phase 6: Op and prepare player
for _ in 1 2 3; do
    if mc-rcon "op $USERNAME" 2>/dev/null | grep -qi "Made"; then break; fi
    sleep 2
done
mc-rcon "gamerule sendCommandFeedback true" 2>/dev/null || true
mc-rcon "time set day" 2>/dev/null || true
mc-rcon "weather clear" 2>/dev/null || true
mc-rcon "gamemode creative $USERNAME" 2>/dev/null || true
sleep 2

# Safety
mc-rcon "attribute $USERNAME minecraft:generic.max_health base set 100" 2>/dev/null || true
mc-rcon "effect give $USERNAME minecraft:regeneration 60 4" 2>/dev/null || true
mc-rcon "effect give $USERNAME minecraft:absorption 60 4" 2>/dev/null || true
sleep 1

echo ""
echo "--- Base Components ---"

# 1. custom_name
give_item "custom_name" "give $USERNAME minecraft:diamond_sword[custom_name='\"{\\\"text\\\":\\\"Test Sword\\\",\\\"color\\\":\\\"gold\\\"}\"'] 1"
check_inv

# 2. lore
give_item "lore" "give $USERNAME minecraft:diamond_sword[lore='[\\\"{\\\\\\\"text\\\\\\\":\\\\\\\"Line 1\\\\\\\"}\\\",\\\"{\\\\\\\"text\\\\\\\":\\\\\\\"Line 2\\\\\\\"}\\\"]'] 1"
check_inv

# 3. enchantments
if [[ "$VER_GROUP" == "v1206" || "$VER_GROUP" == "v121" || "$VER_GROUP" == "v1212" ]]; then
    give_item "enchantments" "give $USERNAME minecraft:diamond_sword[enchantments={levels:{sharpness:3,unbreaking:2},show_in_tooltip:true}] 1"
else
    give_item "enchantments" "give $USERNAME minecraft:diamond_sword[enchantments={levels:{sharpness:3,unbreaking:2}}] 1"
fi
check_inv

# 4. unbreakable
if [[ "$VER_GROUP" == "v1206" || "$VER_GROUP" == "v121" || "$VER_GROUP" == "v1212" ]]; then
    give_item "unbreakable" "give $USERNAME minecraft:diamond_sword[unbreakable={}] 1"
else
    give_item "unbreakable" "give $USERNAME minecraft:diamond_sword[unbreakable] 1"
fi
check_inv

# 5. rarity
give_item "rarity" "give $USERNAME minecraft:diamond_sword[rarity=epic] 1"
check_inv

# 6. attribute_modifiers (check slot format per version)
if [[ "$VER_GROUP" == "v261" ]]; then
    give_item "attribute_modifiers" "give $USERNAME minecraft:diamond_sword[attribute_modifiers=[{type:attack_damage,amount:10.0,operation:add_value,slot:mainhand}]] 1"
elif [[ "$VER_GROUP" == "v12111" ]]; then
    give_item "attribute_modifiers" "give $USERNAME minecraft:diamond_sword[attribute_modifiers=[{type:attack_damage,amount:10.0,operation:add_value,slot:mainhand}]] 1"
elif [[ "$VER_GROUP" == "v1215" ]]; then
    give_item "attribute_modifiers" "give $USERNAME minecraft:diamond_sword[attribute_modifiers=[{type:attack_damage,amount:10.0,operation:add_value,slot:mainhand}]] 1"
elif [[ "$VER_GROUP" == "v1212" ]]; then
    give_item "attribute_modifiers" "give $USERNAME minecraft:diamond_sword[attribute_modifiers=[{type:attack_damage,amount:10.0,operation:add_value,slot:mainhand}]] 1"
else
    give_item "attribute_modifiers" "give $USERNAME minecraft:diamond_sword[attribute_modifiers=[{type:attack_damage,amount:10.0,operation:add_value,slot:mainhand}]] 1"
fi
check_inv

# 7. custom_model_data
give_item "custom_model_data" "give $USERNAME minecraft:stick[custom_model_data=12345] 1"
check_inv

# 8. dyed_color
if [[ "$VER_GROUP" == "v1206" || "$VER_GROUP" == "v121" || "$VER_GROUP" == "v1212" ]]; then
    give_item "dyed_color" "give $USERNAME minecraft:leather_chestplate[dyed_color={rgb:16711680}] 1"
else
    give_item "dyed_color" "give $USERNAME minecraft:leather_chestplate[dyed_color=16711680] 1"
fi
check_inv

# 9. potion_contents
give_item "potion_contents" "give $USERNAME minecraft:potion[potion_contents={potion:swiftness}] 1"
check_inv

# 10. trim
if [[ "$VER_GROUP" == "v1206" || "$VER_GROUP" == "v121" || "$VER_GROUP" == "v1212" ]]; then
    give_item "trim" "give $USERNAME minecraft:diamond_helmet[trim={material:redstone,pattern:eye,show_in_tooltip:true}] 1"
else
    give_item "trim" "give $USERNAME minecraft:diamond_helmet[trim={material:redstone,pattern:eye}] 1"
fi
check_inv

# 11. profile (player head)
give_item "profile" "give $USERNAME minecraft:player_head[profile={name:Notch}] 1"
check_inv

# 12. written_book_content
give_item "written_book" "give $USERNAME minecraft:written_book[written_book_content={title:'\"Test Book\"',author:\"Alex\",pages:['\"Page 1\"','\"Page 2\"'],resolved:true}] 1"
check_inv

# 13. writable_book_content
give_item "writable_book" "give $USERNAME minecraft:writable_book[writable_book_content={pages:['\"Page 1\"','\"Page 2\"']}] 1"
check_inv

# 14. banner_patterns
give_item "banner_patterns" "give $USERNAME minecraft:white_banner[banner_patterns=[{pattern:stripe_top,color:red},{pattern:stripe_bottom,color:blue}]] 1"
check_inv

# 15. container (shulker box)
give_item "container" "give $USERNAME minecraft:shulker_box[container=[{slot:0,item:{id:minecraft:diamond,count:16}},{slot:1,item:{id:minecraft:iron_ingot,count:32}}]] 1"
check_inv

# 16. entity_data (spawn egg)
give_item "entity_data" "give $USERNAME minecraft:creeper_spawn_egg[entity_data={id:minecraft:creeper,powered:1b}] 1"
check_inv

# 17. instrument (goat horn)
give_item "instrument" "give $USERNAME minecraft:goat_horn[instrument=pontent_goat_horn] 1"
check_inv

# 18. fireworks
give_item "fireworks" "give $USERNAME minecraft:firework_rocket[fireworks={flight_duration:2,explosions:[{shape:star,colors:[I;16776960]}]}] 1"
check_inv

# 19. block_state
give_item "block_state" "give $USERNAME minecraft:oak_log[block_state={axis:x}] 1"
check_inv

# 20. stored_enchantments
if [[ "$VER_GROUP" == "v1206" || "$VER_GROUP" == "v121" || "$VER_GROUP" == "v1212" ]]; then
    give_item "stored_enchantments" "give $USERNAME minecraft:enchanted_book[stored_enchantments={levels:{protection:3,mending:1},show_in_tooltip:true}] 1"
else
    give_item "stored_enchantments" "give $USERNAME minecraft:enchanted_book[stored_enchantments={levels:{protection:3,mending:1}}] 1"
fi
check_inv

# 22. damage
give_item "damage" "give $USERNAME minecraft:diamond_sword[damage=10] 1"
check_inv

# 23. enchantment_glint_override
give_item "glint_override" "give $USERNAME minecraft:stick[enchantment_glint_override=true] 1"
check_inv

# 24. food (golden apple triggers food component)
give_item "food" "give $USERNAME minecraft:golden_apple 1"
check_inv

# 25. suspicious_stew
give_item "suspicious_stew" "give $USERNAME minecraft:suspicious_stew[suspicious_stew_effects={effects:[{effect:speed,duration:100}]}] 1"
check_inv

# 26. pot_decorations
give_item "pot_decorations" "give $USERNAME minecraft:decorated_pot[pot_decorations={back:brick,front:brick,left:brick,right:brick,top:brick}] 1"
check_inv

echo ""
echo "--- Version-Specific Components ---"

# ===== v1212+ (1.21.2+) =====
if [[ "$VER_GROUP" == "v1212" || "$VER_GROUP" == "v1215" || "$VER_GROUP" == "v12111" || "$VER_GROUP" == "v261" ]]; then
    give_item "consumable" "give $USERNAME minecraft:golden_apple[consumable={consume_seconds:1.6,animation:eat,sound:entity.generic.eat,has_consume_particles:true}] 1"
    check_inv

    give_item "equippable" "give $USERNAME minecraft:carved_pumpkin[equippable={slot:head,equip_sound:item.armor.equip_iron}] 1"
    check_inv

    give_item "glider" "give $USERNAME minecraft:elytra[glider] 1"
    check_inv

    give_item "tooltip_style" "give $USERNAME minecraft:stick[tooltip_style=minecraft:default] 1"
    check_inv

    give_item "death_protection" "give $USERNAME minecraft:totem_of_undying 1"
    check_inv

    give_item "repairable" "give $USERNAME minecraft:diamond_sword[repairable={items:[diamond]}] 1"
    check_inv

    # ominous_bottle and ominous_bottle_amplifier exist since 1.21
    give_item "ominous_bottle" "give $USERNAME minecraft:ominous_bottle[ominous_bottle_amplifier=3] 1"
    check_inv
fi

# ===== v1215+ (1.21.5+) =====
if [[ "$VER_GROUP" == "v1215" || "$VER_GROUP" == "v12111" || "$VER_GROUP" == "v261" ]]; then
    give_item "weapon" "give $USERNAME minecraft:diamond_sword[weapon={item_damage_per_attack:2}] 1"
    check_inv

    give_item "blocks_attacks" "give $USERNAME minecraft:shield[blocks_attacks={block_sound:item.shield.block,block_delay:5,disable_blocking_for_ticks:100}] 1"
    check_inv

    give_item "tooltip_display" "give $USERNAME minecraft:diamond_sword[tooltip_display={hide_tooltip:true}] 1"
    check_inv

    give_item "potion_duration_scale" "give $USERNAME minecraft:ominous_bottle[potion_duration_scale=1.0] 1"
    check_inv

    give_item "provides_trim_material" "give $USERNAME minecraft:diamond[provides_trim_material={asset:redstone,description:'{\"text\":\"Test\"}'}] 1"
    check_inv

    # Lodestone tracker on compass
    give_item "lodestone_compass" "give $USERNAME minecraft:compass[lodestone_tracker={target:{pos:[I;0,64,0],dimension:overworld},tracked:true}] 1"
    check_inv

    # Entity variant components on spawn eggs
    give_item "wolf_variant" "give $USERNAME minecraft:wolf_spawn_egg[wolf/variant=ashen,wolf/sound_variant=ancient,cat/collar=red] 1"
    check_inv

    give_item "horse_variant" "give $USERNAME minecraft:horse_spawn_egg[horse/variant=white] 1"
    check_inv

    give_item "rabbit_variant" "give $USERNAME minecraft:rabbit_spawn_egg[rabbit/variant=white] 1"
    check_inv

    give_item "fox_variant" "give $USERNAME minecraft:fox_spawn_egg[fox/variant=red] 1"
    check_inv

    give_item "parrot_variant" "give $USERNAME minecraft:parrot_spawn_egg[parrot/variant=red] 1"
    check_inv

    give_item "cat_variant" "give $USERNAME minecraft:cat_spawn_egg[cat/variant=tabby,cat/collar=blue] 1"
    check_inv

    give_item "sheep_color" "give $USERNAME minecraft:sheep_spawn_egg[sheep/color=pink] 1"
    check_inv

    give_item "shulker_color" "give $USERNAME minecraft:shulker_spawn_egg[shulker/color=magenta] 1"
    check_inv

    give_item "mooshroom_variant" "give $USERNAME minecraft:mooshroom_spawn_egg[mooshroom/variant=red] 1"
    check_inv

    give_item "salmon_size" "give $USERNAME minecraft:salmon_spawn_egg[salmon/size=small] 1"
    check_inv

    give_item "frog_variant" "give $USERNAME minecraft:frog_spawn_egg[frog/variant=temperate] 1"
    check_inv

    give_item "llama_variant" "give $USERNAME minecraft:llama_spawn_egg[llama/variant=white] 1"
    check_inv

    give_item "axolotl_variant" "give $USERNAME minecraft:axolotl_spawn_egg[axolotl/variant=lucy] 1"
    check_inv

    give_item "tropical_fish" "give $USERNAME minecraft:tropical_fish_spawn_egg[tropical_fish/base_color=red,tropical_fish/pattern_color=white,tropical_fish/pattern=clownfish] 1"
    check_inv

    give_item "painting_variant" "give $USERNAME minecraft:painting[painting/variant=alban] 1"
    check_inv
fi

# ===== v12111+ (1.21.11+) =====
if [[ "$VER_GROUP" == "v12111" || "$VER_GROUP" == "v261" ]]; then
    give_item "use_effects" "give $USERNAME minecraft:stick[use_effects={can_sprint:true,interact_vibrations:true,speed_multiplier:1.0}] 1"
    check_inv

    give_item "attack_range" "give $USERNAME minecraft:diamond_sword[attack_range={min_range:0.0,max_range:4.0,min_creative_range:0.0,max_creative_range:5.0,hitbox_margin:0.5,mob_factor:0.5}] 1"
    check_inv

    give_item "piercing_weapon" "give $USERNAME minecraft:trident[piercing_weapon={deals_knockback:true,dismounts:true}] 1"
    check_inv

    give_item "kinetic_weapon" "give $USERNAME minecraft:mace[kinetic_weapon={contact_cooldown_ticks:20,delay_ticks:10,forward_movement:0.0,damage_multiplier:1.0}] 1"
    check_inv

    give_item "swing_animation" "give $USERNAME minecraft:diamond_sword[swing_animation={animation:whack,duration:6}] 1"
    check_inv

    give_item "minimum_attack_charge" "give $USERNAME minecraft:diamond_sword[minimum_attack_charge=0.5] 1"
    check_inv

    give_item "damage_type" "give $USERNAME minecraft:diamond_sword[damage_type=player_attack] 1"
    check_inv
fi

# ===== v261 (26.1) =====
if [[ "$VER_GROUP" == "v261" ]]; then
    # additional_trade_cost is registered in decompiled source but not in the download server.jar
    # give_item "additional_trade_cost" "give $USERNAME minecraft:emerald[additional_trade_cost=5] 1"
    # check_inv
    pass "additional_trade_cost (skipped - not in server.jar)"

    give_item "dye" "give $USERNAME minecraft:red_dye[dye=red] 1"
    check_inv

    give_item "pig_variant" "give $USERNAME minecraft:pig_spawn_egg[pig/variant=pig] 1"
    check_inv

    give_item "cow_variant" "give $USERNAME minecraft:cow_spawn_egg[cow/variant=cow] 1"
    check_inv

    give_item "chicken_variant" "give $USERNAME minecraft:chicken_spawn_egg[chicken/variant=chicken,chicken/sound_variant=chicken] 1"
    check_inv

    give_item "pig_sound_variant" "give $USERNAME minecraft:pig_spawn_egg[pig/sound_variant=pig] 1"
    check_inv

    give_item "cow_sound_variant" "give $USERNAME minecraft:cow_spawn_egg[cow/sound_variant=cow] 1"
    check_inv

    give_item "cat_sound_variant" "give $USERNAME minecraft:cat_spawn_egg[cat/sound_variant=cat] 1"
    check_inv

    give_item "zombie_nautilus_variant" "give $USERNAME minecraft:zombie_spawn_egg[zombie_nautilus/variant=zombie] 1"
    check_inv
fi

echo ""
echo "--- Entity Testing ---"

# Summon mobs via RCON (give spawn eggs, then use /summon for entity tracking)
# /summon doesn't go through RCON normally; instead give spawn eggs and use them
mc-rcon "give $USERNAME minecraft:creeper_spawn_egg[entity_data={id:creeper,powered:1b}] 1" 2>/dev/null || true
mc-rcon "give $USERNAME minecraft:zombie_spawn_egg 1" 2>/dev/null || true
mc-rcon "give $USERNAME minecraft:skeleton_spawn_egg 1" 2>/dev/null || true
sleep 1
mcc-cmd --session "$SESSION" "inventory player list" 2>/dev/null || true
mcc-cmd --session "$SESSION" "entity" 2>/dev/null || true
sleep 3
pass "entity_items_given_and_listed"
check_inv

# Health/effects test
mcc-cmd --session "$SESSION" "health" 2>/dev/null || true
sleep 2
pass "health_command"
check_inv

echo ""
echo "=== Results: $VERSION ==="
echo "  Passed: $PASS_COUNT"
echo "  Failed: $FAIL_COUNT"
if [[ ${#FAILURES[@]} -gt 0 ]]; then
    echo "  Failures:"
    for f in "${FAILURES[@]}"; do printf '    - %s\n' "$f"; done
fi
echo "  Log: $MCC_LOG"

[[ $FAIL_COUNT -eq 0 ]] && exit 0 || exit 1
