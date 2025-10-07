# AGENTS.md

This file documents custom commands and agents for the project.

## /format

Command: `/format`

Description: Searches all .cs files in the project. For each file that contains exactly one type (class, record, struct, interface, enum, etc.), checks if the type has XML documentation comments (///). If not, adds a standard XML documentation comment above the type.

Standard comment format:
```csharp
/// <summary>
/// [Type name].
/// </summary>
```

This ensures that single-type files have basic documentation.

## /oco

Command: `/oco`

Description: Analyzes the current git changes, generates a conventional commit message, commits the changes, and pushes to the remote repository.