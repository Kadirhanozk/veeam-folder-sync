# FolderSync

A command-line tool that keeps a replica folder in sync with a source folder by 
creating an exact one-way copy at regular intervals.

## How it works

On each sync cycle, the tool checks the source folder and:

Creates directories that are missing from the replica.
Copies files that exist in the source but are missing from the replica ("File created").
Replaces files in the replica when their content is different from the source ("File updated").
Removes files from the replica that no longer exist in the source ("File deleted").
Removes directories from the replica that no longer exist in the source ("Directory deleted").

Change detection: A file is treated as unchanged when both its size and
SHA-256 hash match the source file. Timestamps are not used for comparison,
as they may be preserved during copies or affected by differences between
system clocks. The file size is checked first to avoid unnecessary hashing,
and the SHA-256 hash is calculated only when the sizes are the same.

Each create, update, and delete operation is written to the console and the
log file along with a timestamp.

The sync loop uses a PeriodicTimer. The first sync runs immediately when
the application starts, followed by another sync every intervalSeconds.
When Ctrl+C is pressed, the application completes the current sync cycle and
then shuts down cleanly.


## Usage

```
dotnet run --project FolderSync -- <sourcePath> <replicaPath> <intervalSeconds> <logFilePath>
```

Example:

```
dotnet run --project FolderSync -- ./source ./replica 30 ./sync.log
```

Or, after building:

```
dotnet build -c Release
./FolderSync/bin/Release/net8.0/FolderSync ./source ./replica 30 ./sync.log
```

### Arguments

| Argument | Description |
|---|---|
| `sourcePath` | Folder to mirror from. Must already exist. |
| `replicaPath` | Folder to mirror into. Created automatically if missing. |
| `intervalSeconds` | How often to re-synchronize, in seconds. |
| `logFilePath` | File that log lines are appended to. |

The tool refuses to start if source and replica resolve to the same path,
or if one is nested inside the other.

## Building and testing

```
dotnet build
dotnet test
```

## Project layout

```
FolderSync/ Console application
Program.cs Entry point, CLI wiring, periodic loop
SyncOptions.cs Argument parsing and validation
FolderSynchronizer.cs Core one-way sync algorithm
ISyncLogger.cs / FileLogger.cs Console + file logging

FolderSync.Tests/ xUnit tests (run against real temp folders)
```

## Design notes / assumptions

- No third-party library is used for folder synchronization. The sync logic is
  implemented directly in the application. The only external dependency is
  xUnit, which is used for testing.
- SHA-256 from System.Security.Cryptography is used to compare file contents
  instead of relying on file timestamps.
- Synchronization runs at fixed intervals rather than using a filesystem
  watcher. This follows the periodic sync requirement and avoids problems
  that can occur with file system events, such as missed events, batched
  events, or buffer overflows.
- If an operation fails for a specific file, for example because the file is
  locked or access is denied, the error is logged and the file is skipped.
  The rest of the sync cycle continues normally.

## Known limitations

- Files with the same size are fully read and hashed on each sync cycle to
  check whether their contents have changed. For very large files or short
  sync intervals, this may have a noticeable performance impact. Chunked
  hashing or checking timestamps first could improve performance, but would
  introduce different trade-offs in change detection.
- Files are processed sequentially during each sync cycle; no parallel file
  operations are used.
