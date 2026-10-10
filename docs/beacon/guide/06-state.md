# Chapter 6: Remember settings and state

A variable disappears when the script reloads. Saved state survives a restart. Settings are values that the user chooses.

## Declare a setting

1. Create `scripts/settings.bcn`.
2. Copy this script into the file.

```beacon
# beacon 1
# setting prefix = "Hello" ; Greeting text
show settings.prefix
```

3. Load the script in MCC.

```text
/scripts run settings
```

The default output is `Hello`. The header comment describes the setting.

4. Change the setting in MCC.

```text
/scripts config settings prefix Welcome
```

5. Reload the script.

```text
/scripts reload settings
```

The output is now `Welcome`. MCC stores the user's choice in `configurations/beacon/settings.settings.toml`.

Do not place passwords in script settings. Settings files are ordinary local files.

## Save a counter

1. Create `scripts/saved-counter.bcn` with this complete script.

```beacon
# beacon 1
set count to saved("count") or 0
set count to count + 1
save "count" to count
show "Run count: {count}"
```

2. Run the script in MCC.

```text
/scripts run saved-counter
```

3. Check for `Run count: 1` on its first run.
4. Reload the script.

```text
/scripts reload saved-counter
```

5. Check for `Run count: 2`.

MCC stores this counter in `configurations/beacon/saved-counter.toml`. The file is separate from editable settings.

The offline `run` command does not provide persistent configuration. Use the normal MCC session for this exercise.

A previous run can leave a higher count. Use a new script ID for a fresh exercise. Keep your original state file if its progress matters.

## Choose the right lifetime

| Value | Visible to | Survives script reload | Survives MCC restart |
| --- | --- | --- | --- |
| Function local | One function call | No | No |
| Script global | One script instance | No | No |
| `shared` value | Scripts in the same MCC client | Yes | No |
| Saved value | The same script ID and configuration directory | Yes | Yes |
| Setting | The script that declares it | Yes | Yes |

Use `lock shared` for a shared read-modify-write operation. The lock prevents two handlers from changing the same value at once.

```beacon
# beacon 1
lock shared
  set shared["guide.loads"] to (shared["guide.loads"] or 0) + 1
end lock
set current to shared["guide.loads"]
show "Shared loads: {current}"
```

Keep the shared key specific to your feature. Avoid slow network operations inside a shared lock.

Next: [Chapter 7: Add commands and integrations](07-integrations.md).
