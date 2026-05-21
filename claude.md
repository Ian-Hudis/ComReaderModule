Project: ComReaderModule
Repository: https://github.com/Ian-Hudis/ComReaderModule (branch: main)
Workspace root: C:\Users\hudis\source\repos\Visual Studio 2022\Other\ComReaderModule
Solution: ComReaderModule.slnx
Target framework(s): .NET 10
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

Notes for AI agents editing this repo
- Make minimal, surgical changes. Preserve existing structure and intent.
- Before changing DB-related code, ensure queries are parameterized and use QueryAsync.
- For multi-file or cross-cutting changes, log a short plan and update claude.md and FileIndex.md.
- ALWAYS include the definition-of-done checklist in PR descriptions.

Contact/Repo metadata
- Remote origin: https://github.com/Ian-Hudis/ComReaderModule
- Branch: main

Last updated: 2026-05-21
