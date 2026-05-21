# ComReaderModule Project Rules

## Architectural Constraints
- **Pattern:** Logic must be decoupled from UI. Move Delphi event logic into Service classes.
- **Threading:** Strictly use `async/await` and `Task.Run` for database/IO.
- **Designer Safety:** NEVER modify `*.Designer.cs` files manually.

## Data Access
- **Technology:** Strictly use **Dapper**. No Entity Framework.
- **Protocol:** All queries must be parameterized. Use `QueryAsync`.

## Coding Style
- **C# 14:** Use Primary Constructors, collection expressions, and file-scoped namespaces.

## Form Standards
- **Use WinForms to make UI forms unless explicitly told otherwise.
- **Generate FormName.cs and FormName.Designer.cs as separate code blocks.
- **Keep the Designer code clean (all control init inside InitializeComponent) so the Visual Studio drag-and-drop editor remains functional.

## Documentation Protocol
- **Definition of Done:** A task is only complete once the C# code is generated AND the relevant tracking files (Claude.md and FileIndex.md) are updated.

## Guardrails 
- **Think Before Coding:** Before translating a driver or unit, explicitly state your assumptions regarding hardware memory structures, pointer conversions, and native types. If uncertain or alternative interpretations exist, halt and ask.
- **Simplicity First:** Implement the exact logic requested. Do not introduce speculative abstractions, wrappers, or custom interfaces unless explicitly requested or required for DLL dynamic linking.
- **Surgical Changes:** Match the structural design intent of the legacy code to preserve parity. Do not "improve" adjacent methods or refactor functional logic that isn't broken.