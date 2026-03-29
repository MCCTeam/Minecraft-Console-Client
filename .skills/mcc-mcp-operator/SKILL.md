---
name: mcc-mcp-operator
description: Operate Minecraft Console Client through the built-in MCP server. Use this whenever the user wants an agent to inspect MCC state, move, search the world, interact with players or entities, dig, pick up items, manage containers, or carry out Minecraft tasks through MCP tools, even if they do not explicitly say "use MCP" or "control MCC". Prefer this skill over ad hoc tool guessing for agentic MCC and Minecraft control work.
---

# MCC MCP Operator

Use the MCC MCP toolset as the source of truth for game state and action results.
Do not guess what happened from intent alone.

## Operating Loop

1. Inspect the current situation before acting.
2. Make the shortest plan that can succeed.
3. Use the smallest set of high-signal tools needed to act.
4. Verify the outcome with fresh tool calls.
5. Report only what is verified, and clearly label anything inferred or still unknown.

If the request is purely conversational and does not require MCC state, answer directly instead of wasting tool calls.

## Tool Selection Rules

- Start with `mcc_session_status` whenever connection state, enabled capabilities, or feature availability is uncertain.
- Prefer direct inspection tools such as `mcc_player_state`, `mcc_players_list`, `mcc_entities_list`, `mcc_blocks_find`, `mcc_items_list`, and `mcc_inventory_snapshot` before taking physical actions.
- Prefer purpose-built action tools over low-level escape hatches.
- Prefer `mcc_container_open_at`, `mcc_container_deposit_item`, and `mcc_container_withdraw_item` over `mcc_inventory_window_action` for chest or container work.
- Use `mcc_can_reach_position` or a locating tool before pathing when reachability is uncertain.
- Use `mcc_run_internal_command` only when no purpose-built MCP tool covers the task cleanly.
- Treat `success=false`, `action_incomplete`, `capability_disabled`, `feature_disabled`, and `invalid_args` as failed or partial observations, not success.
- After `invalid_args`, simplify the call and try at most one nearby variant. Do not spam near-duplicate guesses.

## Verification Rules

- Movement is not complete just because a move request was accepted. Confirm `arrived=true` or verify the new location with a fresh state read.
- Digging is not complete just because `mcc_dig_block` was invoked. Re-check the target block or nearby block search results.
- Item pickup is not complete just because the bot moved over an item. Re-check inventory state or nearby dropped-item entities.
- Container transfers are not complete just because a click or transfer request was accepted. Verify the resulting counts after the transfer.
- Chat or command effects should be verified through state changes, chat history, or another direct observation when possible.
- When evidence is partial, say exactly what was verified and what remains unverified.

## Best Practices

- Query first, act second, verify third.
- Keep plans short and concrete. Long speculative tool chains usually make the result worse.
- Prefer high-signal tools that answer the real question directly.
- Use structured inventory and container tools instead of raw slot manipulation whenever possible.
- Do not claim success from acceptance alone. Always pair actions with a follow-up observation.
- Distinguish verified facts, reasonable inferences, and unknowns in the final answer.
- If a tool says a capability or feature is disabled, stop using tools from that category and explain the limitation.
- If a path fails or arrives short, revise the plan using the latest position instead of blindly retrying the same action.
- Use `mcc_quit_client` to stop MCC. Do not send bare `quit` or `exit` through chat.
- Keep the final response concise and grounded in the evidence you actually collected.

## Example Scenarios

### Move to a player and confirm proximity

User intent: "Find Zarko and move near them."

Good flow:
- call `mcc_player_locate` or `mcc_players_list` to confirm the player is known
- if needed, call `mcc_can_reach_position` for the target area
- call `mcc_move_to_player`
- verify `arrived=true` or confirm the new position with `mcc_player_state`
- report whether proximity was verified or only partially achieved

### Open a chest, move an exact item count, and verify the result

User intent: "Put 5 diamonds in the chest at 11000 64 11021."

Good flow:
- call `mcc_container_open_at`
- inspect current state with `mcc_inventory_snapshot` if item availability is unclear
- call `mcc_container_deposit_item` or `mcc_container_withdraw_item`
- verify the resulting counts from the transfer result and, when useful, a fresh inventory snapshot
- report the exact verified delta, not just that the action was attempted

### Collect nearby dropped items or dig target blocks and verify the outcome

User intent: "Pick up nearby apples" or "Break those logs and collect them."

Good flow:
- call `mcc_items_list` or `mcc_blocks_find` to locate the target
- move only if the target is not already reachable from the current position
- call `mcc_items_pickup` for dropped items, or `mcc_dig_block` in a sensible order for blocks
- verify the result with `mcc_items_list`, `mcc_inventory_snapshot`, or a fresh block query
- if the result is partial, say what changed and what still remains

## Output Style

- Lead with the outcome the user cares about.
- Include the small set of observations that justify the answer.
- If something failed, say what failed, what was verified anyway, and the next sensible step.
- Do not embellish uncertain results.
