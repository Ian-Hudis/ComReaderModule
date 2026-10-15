Project: ComReaderModule
Repository: https://github.com/Ian-Hudis/ComReaderModule (branch: main)
Workspace root: C:\Users\hudis\source\repos\Visual Studio 2022\Other\ComReaderModule
Solution: ComReaderModule.slnx
Target framework(s): .NET 8 (net8.0, per ComReaderModule.csproj)
Primary language: C# (WinForms UI + services)

High-level summary
- ComReaderModule is a .NET 10 solution that implements a Windows Forms-based application with business logic decoupled into service classes and Dapper-based data access. The UI uses WinForms forms and designers; database access must use Dapper QueryAsync with parameterized queries.

Caller-facing rules (for any AI or human contributor)
- NEVER edit any *.Designer.cs files manually. Designer code must remain designer-safe.
- Data access must use Dapper only. Use parameterized queries and QueryAsync for all DB operations.
- Use async/await or Task.Run for any IO or database work; avoid blocking calls on the UI thread.
- Keep logic out of UI event handlers; move complex logic into Service classes.
- Use C# 14 language features where appropriate (file-scoped namespaces, primary constructors, collection expressions).
- WinForms is the UI technology unless explicitly instructed otherwise.

Definition of Done (project-level)
- A change is complete when: (1) C# code compiles and follows the rules above, (2) relevant tracking files are updated: claude.md and FileIndex.md, (3) any unit tests (if present) pass locally or the failure is documented and intentional.

Important files & locations to check first
- Program.cs (startup)
- *.csproj files (target frameworks)
- Any folder named Services or DataAccess (Dapper usage)
- Forms: *.cs and corresponding *.Designer.cs (DO NOT EDIT Designer manually)
- .github/copilot-instructions.md (project rules; authoritative guide)

Build & run
- Preferred dev environment: Visual Studio Community 2026 (18.7.0-insiders)
- Typical steps: open solution in Visual Studio and run; or use 'dotnet build' at the solution root to compile
- Debug builds are .NET 8: 'dotnet build -c Debug', then run '.\bin\Debug\net8.0\ComReaderModule.exe'
- Run the exe from the project root so that the relative 'sampledata' folder resolves

Console commands
- 'test'  - pushes a single "test" string through DataInterpreter to prove the UIData.json pipeline works
- 'run <file>' - replays a captured log line by line through DataInterpreter, as if it came from the USB sniffer
  * The file name is resolved against the working directory, then against the 'sampledata' folder, so
    'run DataReference09012026.txt' works from the project root
  * Each line is '<timestamp> <label> <payload>'. The label decides direction: '[PC]' is PC -> machine and is
    skipped by the parser, '[DEV n EP m]' is machine -> PC and is parsed
  * 'run' stops the USB sniffer first so live traffic cannot pollute the results
  * SampleReplay.Run calls DataInterpreter.ResetState() first, so every run starts from the same clean state
  * Results are printed as a summary and written to UIData.json next to the executable
- 'exit'  - stops the sniffer and quits
- Expected replay results (useful as a regression check):
  * sampledata/DataReference09012026.txt -> V1=784.001953125, V2=256.00390625, V3=0, PartCount=14, RUNNING
  * sampledata/DataReference09252026.txt -> V1=784.001953125, V2=0, V3=0, PartCount=0, RUNNING
  * V3 is legitimately 0 for both files: the marker payloads never reach 36 hex characters, so DataInterpreter's
    Findaxis caps at 2 axes and the Z branch is never taken. A V3 of 0 is not a failure.

Notes for AI agents editing this repo
- Make minimal, surgical changes. Preserve existing structure and intent.
- Before changing DB-related code, ensure queries are parameterized and use QueryAsync.
- For multi-file or cross-cutting changes, log a short plan and update claude.md and FileIndex.md.
- ALWAYS include the definition-of-done checklist in PR descriptions.

Last updated: 2026-09-28
