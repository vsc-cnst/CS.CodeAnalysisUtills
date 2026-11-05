# CodeAnalysisUtills

CodeAnalysisUtills is a .NET library of Roslyn helpers for opening and compiling MSBuild projects, discovering C# classes, mapping declarations to runtime types, and inspecting reflection types and members.

> **Disclaimer:** This is an experimental project and is not production-ready. Its public API is unstable and may change or break between releases. Use it at your own risk and evaluate it carefully before relying on it.

## Contents

- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Getting started](#getting-started)
- [Discovering classes](#discovering-classes)
- [Working with reflection types](#working-with-reflection-types)
- [API overview](#api-overview)
- [Notes and limitations](#notes-and-limitations)
- [License](#license)

## Features

- Open an MSBuild project and obtain its Roslyn `Project` and `Compilation`.
- Emit a compilation to memory and load its assembly.
- Find class declarations in a document, optionally matching namespace names.
- Map syntax declarations to runtime `Type` objects, including nested and generic types.
- Retrieve distinct runtime class types from a project.
- Check whether reflection members and types represent basic values, classes, or generic collections.
- Filter and group types by their inheritance relationships.

## Requirements

- .NET 10 or later for the package version described here.
- An installed .NET SDK and MSBuild installation for opening project files.
- The target project's dependencies must be available to MSBuild and, when loading emitted assemblies, to the runtime.

The MSBuild project helpers are conditionally compiled for .NET Framework 4.8 and .NET 7 or later. This package targets .NET 10.

## Installation

Install the package from NuGet:

```powershell
dotnet add package CodeAnalysisUtills
```

The examples below use C# top-level statements. Import the namespaces used by the helpers:

```csharp
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using vsc.CodeAnalysisUtills.Extensions;
using vsc.CodeAnalysisUtills.ProjectAnalysis;
```

## Getting started

Register the installed MSBuild toolset before creating an `MSBuildWorkspace`. Register it once, early in the process, before any MSBuild assemblies have been loaded:

```csharp
MSBuildLocator.RegisterDefaults();

var projectPath = @"C:\src\MyApp\MyApp.csproj";
using var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath);

var compilation = await project.GetCompilationAsync()
	?? throw new InvalidOperationException("Could not create the project compilation.");

Console.WriteLine($"Opened {project.Name}");
Console.WriteLine($"Compilation: {compilation.AssemblyName}");
```

Keep the workspace alive while you use its `Project` and `Document` objects. For end-to-end workflows, managing `MSBuildWorkspace` directly makes its lifetime explicit. The library also provides convenience methods for opening or compiling a project:

```csharp
var project = await CodeAnalysisExtensions.GetProject(projectPath);
var (compiledProject, compilation) = await CodeAnalysisExtensions.GetProjectCompilation(projectPath);
```

`GetProjectCompilationAssemblies` returns the project, compilation, and assemblies loaded from the project's metadata references:

```csharp
var (project, compilation, referencedAssemblies) =
	await CodeAnalysisExtensions.GetProjectCompilationAssemblies(projectPath);
```

## Discovering classes

### Find syntax declarations in a document

`GetClassesAsync` returns `ClassDeclarationSyntax` nodes. Namespace matching is ordinal and case-sensitive. If the namespace filter is `null` or empty, it searches the whole document.

```csharp
var document = project.Documents.First();

var allClasses = await document.GetClassesAsync();
var appClasses = await document.GetClassesAsync(["MyApp"]);
var matchingSubnamespaces = await document.GetClassesAsync(
	["MyApp"],
	NamespaceMatchKind.StartsWith);
```

The available match kinds are `Equals` (the default), `StartsWith`, `EndsWith`, and `Contains`:

```csharp
var classesInExactNamespace = await document.GetClassesAsync(
	["MyApp.Services"],
	NamespaceMatchKind.Equals);

var classesInNamespacesContainingText = await document.GetClassesAsync(
	["Services"],
	NamespaceMatchKind.Contains);
```

The discovery traverses nested classes and nested namespaces, and works with block-scoped and file-scoped namespaces.

### Get runtime types for a project

To inspect runtime `Type` objects, provide a compilation. This overload emits and loads the compilation's assembly:

```csharp
var runtimeTypes = await project.GetAllClassesAsync(compilation);

foreach (var type in runtimeTypes)
{
	Console.WriteLine(type.FullName);
}
```

You can filter by namespace in the same way as `GetClassesAsync`:

```csharp
var serviceTypes = await project.GetAllClassesAsync(
	compilation,
	["MyApp.Services"],
	NamespaceMatchKind.StartsWith);
```

If you already have the emitted assembly, pass it directly to avoid emitting and loading another one:

```csharp
var assembly = compilation.LoadAssembly();
var runtimeTypes = await project.GetAllClassesAsync(assembly);
```

### Map declarations to runtime types

Use a document's semantic model to map selected declarations into an assembly:

```csharp
var document = project.Documents.First();
var declarations = await document.GetClassesAsync(["MyApp.Models"]);
var semanticModel = await document.GetSemanticModelAsync()
	?? throw new InvalidOperationException("Could not get the semantic model.");
var assembly = compilation.LoadAssembly();

var runtimeTypes = declarations.ToTypes(assembly, semanticModel);
```

## Working with reflection types

### Inspect members

The `MemberInfo` extensions recognize properties and fields for data-type checks. `GetMemberDataType` also returns a method's return type.

```csharp
using System.Reflection;

MemberInfo? member = typeof(MyModel).GetProperty(nameof(MyModel.Name));

if (member.MemberIsBasic())
{
	Console.WriteLine(member.GetMemberDataType());
}

var isCollection = member.MemberIsIEnumerable();
var isList = member.MemberIsList();
var isMethod = member.MemberIsMethod();
```

The methods include `MemberIsBasic`, `MemberIsClass`, `MemberIsList`, `MemberIsICollection`, `MemberIsIEnumerable`, and `MemberIsMethod`. The member checks accept `null` and return `false` (or `null` for `GetMemberDataType`).

`MemberInfoNameTypeComparer` compares members by name and declaring type. Overloads with the same name on the same declaring type therefore compare equal; use normal reflection identity/equality if overloads must remain distinct.

### Inspect and group types

Type helpers identify nullable value types, basic types, generic enumerable/collection implementations, and exact `List<T>` types:

```csharp
var isNullableValueType = typeof(int?).IsNullableValueType();
var isBasic = typeof(decimal).IsBasicType();
var isEnumerable = typeof(string[]).IsIEnumerable();
var isCollection = typeof(List<string>).IsICollection();
var isList = typeof(List<string>).IsList();
```

Filter a set of types by assignability. The `includeBaseType` option controls whether the base type itself is included:

```csharp
var derivedTypes = candidateTypes.WhereInheritsFrom(typeof(MyBaseType));
var includingBaseType = candidateTypes.WhereInheritsFrom(
	typeof(MyBaseType),
	includeBaseType: true);
```

`InheritsFrom` and `IsBaseTypeOf` use `Type.IsAssignableFrom`; their behavior includes interface implementation as well as class inheritance. `DescendantTypes` uses `Type.IsSubclassOf` and therefore selects class descendants.

`BaseTypes` returns class base types from nearest to furthest, optionally filtered to a supplied universe. `RootElements` selects types whose immediate base type is not in the supplied set. `GroupByRootElements` groups each selected root with its assignable descendants:

```csharp
var baseTypes = typeof(MyDerivedType).BaseTypes();
var roots = candidateTypes.RootElements();
var hierarchy = candidateTypes.GroupByRootElements();
```

## API overview

| Area | Main APIs |
|---|---|
| MSBuild and compilations | `CodeAnalysisExtensions.GetProject`, `GetProjectCompilation`, `GetProjectCompilationAssemblies`, `Compilation.LoadAssembly` |
| Class discovery | `Document.GetClassesAsync`, `Project.GetAllClassesAsync`, `IEnumerable<ClassDeclarationSyntax>.ToTypes` |
| Namespace matching | `NamespaceMatchKind` |
| Member inspection | `GetMemberDataType`, `MemberIsBasic`, `MemberIsClass`, `MemberIsList`, `MemberIsICollection`, `MemberIsIEnumerable`, `MemberIsMethod` |
| Type inspection | `IsNullableValueType`, `IsBasicType`, `IsIEnumerable`, `IsICollection`, `IsList` |
| Type hierarchies | `WhereInheritsFrom`, `InheritsFrom`, `WhereIsBaseTypeOf`, `IsBaseTypeOf`, `BaseTypes`, `DescendantTypes`, `RootElements`, `GroupByRootElements` |

## Notes and limitations

- Class discovery returns C# class declarations (`ClassDeclarationSyntax`); records, structs, and interfaces are not included.
- Namespace filters use literal ordinal string comparisons, not regular expressions or culture-aware matching.
- `GetAllClassesAsync(Project, Compilation, ...)` emits and loads the compilation. Reuse an existing assembly overload when appropriate.
- `Compilation.LoadAssembly` requires a successful emit. Types that depend on assemblies unavailable to the runtime may not load or execute as expected.
- Opening MSBuild projects requires compatible installed tooling and project dependencies. For custom MSBuild selection, configure `MSBuildLocator` before using `MSBuildWorkspace`.

## License

This project is licensed under the MIT License.
