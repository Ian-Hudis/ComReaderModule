# FileIndex

Map of the source files in ComReaderModule and what each one owns.

## Entry point

| File | Role |
|---|---|
| `Program.cs` | Console entry point. Starts the USB sniffer, then loops on operator commands (`test`, `run <file>`, `exit`). Owns the sniffer lifecycle, including the one-shot `snifferActive` flag that stops capture before a replay and prevents a second `Process.Dispose`. |

## Capture

| File | Role |
|---|---|
| `USBSniff.cs` | Launches `USBPcapCMD.exe`, reads the raw pcap stream, determines traffic direction, and prints/logs each message. Reads `Config.ini`. Defines the `DataDirection` enum. |
| `DataConstructor.cs` | Extracts the printable payload from a captured USBPcap frame: skips the pcap header, drops `USBS`/`USBC` wrappers, truncates at `\r`/`\n`, rejects empty protocol packets. |
| `DataLogging.cs` | Writes rotating `data/logN.txt` files when `DebugMode` is on. |

## Interpretation and output

| File | Role |
|---|---|
| `DataInterpreter.cs` | Core parser. Filters noisy characters, finds the `#9921` marker, decodes the X/Y/Z axes (little-endian, scaled by 512), tracks `PartCount` and `MachineStatus`, and maintains the rolling 10-entry `Debuglog`. `ResetState()` clears the logs, the counters and the write cache. |
| `UpdateJSon.cs` | Writes `UIData.json` next to the executable, skipping the write when no tracked value changed. Holds `FileLocation` and the `ControllerData` shape. |

## Test support

| File | Role |
|---|---|
| `SampleReplay.cs` | Feeds a captured log file through `DataInterpreter` line by line so the pipeline can be verified without live hardware. Strips the timestamp/label prefix, maps the label to a direction, prints only value changes, and ends with a summary. |

## Data and config

| File | Role |
|---|---|
| `Config.ini` | `USBPcap_Location`, `USBID`, `DebugMode`. Copied to the output directory. |
| `UIData.json` | The machine-readable status file the dashboard reads. Copied to the output directory and rewritten at runtime. |
| `sampledata/*.txt` | Captured reference logs in `<timestamp> <label> <payload>` form, used as replay input. |
| `ComReaderModule.csproj` | net8.0 console app. |

## Conventions

- Replay input lines are `<timestamp> <label> <payload>`. The label sets direction: `[PC]` is outbound (the parser
  skips it), `[DEV n EP m]` is inbound (the parser reads it).
- Because the label is sliced *including* its closing bracket, direction checks must match on the complete `[PC]` token.
- `sampledata` is resolved relative to the working directory, so run the exe from the project root.
