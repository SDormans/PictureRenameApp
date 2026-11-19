# Rename Patterns

Batch rename builds each new name from a pattern. The original extension is always kept.

## Placeholders

| Placeholder | Becomes | Example |
|---|---|---|
| `{n}` | Running number, zero-padded to the chosen width | `07` |
| `{name}` | Original file name without extension | `IMG_1234` |
| `{date}` | Date taken (EXIF), or the modified date when there is none, as `yyyy-MM-dd` | `2024-05-01` |
| `{date:FORMAT}` | Same date in any [.NET date format](https://learn.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings) | `{date:yyyyMMdd}` → `20240501` |

- Placeholders are case-insensitive.
- If the pattern has no `{n}`, the number is appended directly: `Holiday` → `Holiday01`.
- Placeholders are expanded in one pass, so a `{date}` inside an original file name is left as text.
- In `{date:FORMAT}`, `:` and `/` become `-`, and other characters not allowed in file names are replaced.
- If a new name is already taken, a number is added, for example `Holiday 01 (2).jpg`.

Example: `{date} {name}` → `2024-05-01 IMG_1234 01.jpg`.

## Subfolder

With **Move into subfolder** ticked (the default, see [Settings](Settings.md)), the renamed files move into a folder named after the pattern with its placeholders removed. For example, `Holiday {n}` moves them into `Holiday/`.

- Leading and trailing spaces and separators such as `-`, `_`, `.`, `(`, `#` are trimmed from the folder name.
- Names Windows reserves (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`) get a `_` prefix.
- Running the same batch again on files already inside that subfolder does not create a nested folder.

## Undo

Each rename or batch rename is recorded. **Undo rename** (Ctrl+Z) reverses the last one. Files are moved back in reverse order, and the subfolder is deleted if it is empty afterwards.
