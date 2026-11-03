# CodeAnalysisUtills

Utilities for loading, compiling, and inspecting .NET projects and types with Roslyn.

## Installation

```powershell
dotnet add package CodeAnalysisUtills
```

## Features

- Open an MSBuild project and get its Roslyn `Project`.
- Compile a project and get its `Compilation`.
- Emit a compilation to memory and load the resulting `Assembly`.
- Find class declarations in a document, optionally filtered by namespace.
- Map class declarations to their runtime `Type`.

## Usage

```csharp
using vsc.Mappings;
using vsc.ProjectAnalysis;

var (project, compilation) = await CodeAnalysisExtensions.CompileProject("path/to/MyProject.csproj");

// All classes in the project
IEnumerable<Type> allTypes = TypeDiscovery.AllClasses(

// Only classes in specific namespaces
IEnumerable<Type> filtered = TypeDiscovery.AllClasses(
```

## API

### `vsc.ProjectAnalysis.CodeAnalysisExtensions`

| Method | Description |
|---|---|
| `GetProject(string projectPath)` | Opens the MSBuild project at the specified path. |
| `CompileProject(string projectPath)` | Opens and compiles the project at the specified path. |
| `CompileProject(Project project)` | Compiles the project and loads its referenced assemblies. |

### `vsc.Mappings.TypeDiscovery`

| Method | Description |
|---|---|
| `LoadAssembly(Compilation compilation)` | Emits the compilation to memory and loads the assembly. |
| `GetClasses(this Document document, string[] targetNamespaces)` | Finds class declarations in a document. |
| `ToTypes(this IEnumerable<ClassDeclarationSyntax>, Assembly, SemanticModel)` | Maps class declarations to runtime types. |
| `AllClasses(Project, Compilation, string[] targetNamespaces)` | Retrieves all runtime types declared in the project. |

## Requirements

- .NET 10

## License

MIT
