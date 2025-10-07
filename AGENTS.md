# AGENTS.md

This file documents custom commands and agents for the project.

## /format

Command: `/format`

Description: Searches all .cs files in the project. For each file that contains exactly one type (class, record, struct, interface, enum, etc.), checks if the type has XML documentation comments (///). If not, analyzes the context of the type and adds appropriate XML documentation comments above the type. If the file contains the token '##REFORMAT##', removes existing XML documentation comments and regenerates them from scratch.

This ensures that single-type files have meaningful documentation based on their purpose and structure.

## /oco

Command: `/oco`

Description: Analyzes the current git changes, generates a conventional commit message, commits the changes, and pushes to the remote repository.