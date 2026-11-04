# CodeAnalysisUtills

CodeAnalysisUtills is a .NET library built on Roslyn that helps you load, compile, and inspect MSBuild projects and their types. 
It has some common utilities frequently needed when working with Roslyn.

## Features

- Open an MSBuild project and get its Roslyn `Project`.
- Compile a project and get its `Compilation`.
- Emit a compilation to memory and load the resulting `Assembly`.
- Find class declarations in a document, optionally filtered by namespace.
- Map class declarations to their runtime `Type`.
- Retrieve all runtime types declared in a project.
- Inspect `MemberInfo` instances (basic, class, list, collection, and method checks).
- Inspect `Type` instances and type hierarchies (collection checks, inheritance filtering, root grouping).

## Installation

```powershell
dotnet add package CodeAnalysisUtills
```


## License

MIT
