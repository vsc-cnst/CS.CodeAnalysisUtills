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
- Retrieve all runtime types declared in a project.
- Inspect `MemberInfo` instances (basic, class, list, collection, and method checks).
- Inspect `Type` instances and type hierarchies (collection checks, inheritance filtering, root grouping).

## Usage

```csharp
using CodeAnalysisUtills.Extensions;
using CodeAnalysisUtills.ProjectAnalysis;

var (project, compilation) = await CodeAnalysisExtensions.CompileProject("path/to/MyProject.csproj");

// All classes in the project
IEnumerable<Type> allTypes = Utils.GetAllClasses(project, compilation, Array.Empty<string>());

// Only classes in specific namespaces
IEnumerable<Type> filtered = Utils.GetAllClasses(project, compilation, ["MyApp.Models"]);
```

## API

### `CodeAnalysisUtills.ProjectAnalysis.CodeAnalysisExtensions`

| Method | Description |
|---|---|
| `GetProject(string projectPath)` | Opens the MSBuild project at the specified path and returns its Roslyn `Project`. |
| `CompileProject(string projectPath)` | Opens and compiles the project at the specified path. |
| `CompileProject(Project project)` | Compiles the project and loads its referenced assemblies. |
| `LoadAssembly(Compilation compilation)` | Emits the compilation to memory and loads the assembly. |

### `CodeAnalysisUtills.Extensions.Utils`

| Method | Description |
|---|---|
| `GetClasses(this Document document, string[] targetNamespaces)` | Finds class declarations in a document, optionally filtered by namespace. |
| `ToTypes(this IEnumerable<ClassDeclarationSyntax>, Assembly, SemanticModel)` | Maps class declarations to runtime types. |
| `GetAllClasses(Project, Compilation, string[] targetNamespaces)` | Retrieves all runtime types declared in the project, optionally filtered by namespace. |

### `CodeAnalysisUtills.Extensions.MemberInfoExtensions`

| Method | Description |
|---|---|
| `GetMemberDataType()` | Gets the property/field type or method return type. |
| `MemberIsBasic()` | Checks whether a field or property is a basic type. |
| `MemberIsClass()` | Checks whether a field or property is a class type that is not `IEnumerable`. |
| `MemberIsList()` | Checks whether a field or property is a `List<T>`. |
| `MemberIsICollection()` | Checks whether a field or property implements `ICollection<T>`. |
| `MemberIsIEnumerable()` | Checks whether a field or property implements `IEnumerable<T>`. |
| `MemberIsMethod()` | Checks whether a member is a method. |

`MemberInfoNameTypeComparer` is an `IEqualityComparer<MemberInfo>` that compares members by name and declaring type.

### `CodeAnalysisUtills.Extensions.SystemTypeExtensions`

| Method | Description |
|---|---|
| `IsNullable()` | Checks whether a type is `Nullable<T>`. |
| `IsBasicType()` | Checks whether a type is a primitive, enum, `string`, `float`, or `decimal`, including nullable forms. |
| `IsIEnumerable()` | Checks whether a type is or implements `IEnumerable<T>`. |
| `IsICollection()` | Checks whether a type is or implements `ICollection<T>`. |
| `IsList()` | Checks whether a type is `List<T>`. |
| `WhereInheritsFrom(Type baseType, bool includeBaseType)` | Filters types to those assignable to a base type. |
| `InheritsFrom(Type baseType, bool includeBaseType)` | Checks whether a type inherits from a base type. |
| `WhereIsBaseTypeOf(Type parentType, bool includeBaseType)` | Filters types to those for which a type is a base type. |
| `IsBaseTypeOf(Type childType, bool includeChildType)` | Checks whether a type is a base type of another type. |
| `BaseTypes(IEnumerable<Type> universe)` | Returns base types, from nearest to furthest ancestor. |
| `DescendantTypes(IEnumerable<Type> universe)` | Returns types in the universe that derive from a type. |
| `RootElements(IEnumerable<Type>)` | Returns types whose base type is not in the collection. |

## Requirements

- .NET 10

## License

MIT
