using System.Reflection;
using vsc.CodeAnalysisUtills.ProjectAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace vsc.CodeAnalysisUtills.Extensions
{
    /// <summary>
    /// Specifies how namespace names are compared with the target namespace names.
    /// </summary>
    public enum NamespaceMatchKind
    {
        /// <summary>The namespace name starts with the target name.</summary>
        StartsWith,
        /// <summary>The namespace name ends with the target name.</summary>
        EndsWith,
        /// <summary>The namespace name contains the target name.</summary>
        Contains,
        /// <summary>The namespace name is equal to the target name.</summary>
        Equals,
    }

    /// <summary>
    /// Provides helper methods for finding class declarations and mapping them to runtime types.
    /// </summary>
    public static class ClassDiscoveryExtensions
    {
        /// <summary>
        /// Finds all class declarations in the specified namespaces of a document.
        /// </summary>
        /// <param name="document">The document to search.</param>
        /// <param name="targetNamespaces">The namespace names to search. If <see langword="null"/> or empty, all namespaces are searched.</param>
        /// <param name="comparisonType">The method used to compare namespace names with <paramref name="targetNamespaces"/>.</param>
        /// <returns>The class declarations found in the document.</returns>
        public static async Task<IEnumerable<ClassDeclarationSyntax>> GetClassesAsync(this Document document, string[]? targetNamespaces = null, NamespaceMatchKind comparisonType = NamespaceMatchKind.Equals, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return Array.Empty<ClassDeclarationSyntax>();

            var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            return FindClasses(root, semanticModel, targetNamespaces, comparisonType, cancellationToken);
        }

        /// <summary>
        /// Retrieves all runtime types declared in the project's documents, optionally filtered by namespace.
        /// </summary>
        /// <param name="project">The project that contains the documents to search.</param>
        /// <param name="compilation">The compilation used to load the runtime assembly.</param>
        /// <param name="targetNamespaces">The namespace names to search. If <see langword="null"/> or empty, all namespaces are searched.</param>
        /// <param name="comparisonType">The method used to compare namespace names with <paramref name="targetNamespaces"/>.</param>
        /// <returns>The distinct runtime types declared in the project.</returns>
        public static Task<IEnumerable<Type>> GetAllClassesAsync(this Project project, Compilation compilation, string[]? targetNamespaces = null, NamespaceMatchKind comparisonType = NamespaceMatchKind.Equals, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(compilation);
            cancellationToken.ThrowIfCancellationRequested();

            var assembly = CodeAnalysisExtensions.LoadAssembly(compilation);
            return project.GetAllClassesAsync(assembly, targetNamespaces, comparisonType, cancellationToken);
        }

        public static async Task<IEnumerable<Type>> GetAllClassesAsync(this Project project, Assembly assembly, string[]? targetNamespaces = null, NamespaceMatchKind comparisonType = NamespaceMatchKind.Equals, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(project);
            ArgumentNullException.ThrowIfNull(assembly);

            var types = new HashSet<Type>();
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (semanticModel == null) continue;

                var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var classes = FindClasses(root, semanticModel, targetNamespaces, comparisonType, cancellationToken);
                types.UnionWith(classes.ToTypes(assembly, semanticModel));
            }

            return types.ToList();
        }

        /// <summary>
        /// Maps class declaration syntax nodes to their runtime types in the specified assembly.
        /// </summary>
        /// <param name="classDeclarationSyntaxes">The class declarations to map.</param>
        /// <param name="assembly">The assembly that contains the runtime types.</param>
        /// <param name="semanticModel">The semantic model of the syntax tree that contains the declarations.</param>
        /// <returns>The distinct runtime types that were found in the assembly.</returns>
        public static IEnumerable<Type> ToTypes(this IEnumerable<ClassDeclarationSyntax> classDeclarationSyntaxes, Assembly assembly, SemanticModel semanticModel)
        {
            ArgumentNullException.ThrowIfNull(classDeclarationSyntaxes);
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(semanticModel);

            return classDeclarationSyntaxes
                .Select(classDeclaration => semanticModel.GetDeclaredSymbol(classDeclaration))
                .OfType<INamedTypeSymbol>()
                .Select(symbol => assembly.GetType(GetReflectionName(symbol)))
                .OfType<Type>()
                .Distinct();
        }

        private static List<ClassDeclarationSyntax> FindClasses(SyntaxNode root, SemanticModel semanticModel, string[]? targetNamespaces, NamespaceMatchKind comparisonType, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(comparisonType))
                throw new ArgumentOutOfRangeException(nameof(comparisonType), comparisonType, "Unsupported comparison type.");

            var classes = new List<ClassDeclarationSyntax>();
            foreach (var declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (targetNamespaces == null || targetNamespaces.Length == 0)
                {
                    classes.Add(declaration);
                    continue;
                }

                var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                if (symbol == null) continue;

                var containingNamespace = symbol.ContainingNamespace;
                var namespaceName = GetNamespaceName(containingNamespace);
                var matches = targetNamespaces.Any(target => comparisonType switch
                {
                    NamespaceMatchKind.StartsWith => namespaceName.StartsWith(target, StringComparison.Ordinal),
                    NamespaceMatchKind.EndsWith => namespaceName.EndsWith(target, StringComparison.Ordinal),
                    NamespaceMatchKind.Contains => namespaceName.Contains(target, StringComparison.Ordinal),
                    NamespaceMatchKind.Equals => string.Equals(namespaceName, target, StringComparison.Ordinal),
                    _ => throw new ArgumentOutOfRangeException(nameof(comparisonType))
                });

                if (matches) classes.Add(declaration);
            }

            return classes;
        }

        private static string GetReflectionName(INamedTypeSymbol symbol)
        {
            var names = new Stack<string>();
            for (var current = symbol; current != null; current = current.ContainingType)
            {
                names.Push(current.MetadataName);
            }

            var typeName = string.Join("+", names);
            var namespaceName = GetNamespaceName(symbol.ContainingNamespace);
            return namespaceName.Length == 0
                ? typeName
                : namespaceName + "." + typeName;
        }

        private static string GetNamespaceName(INamespaceSymbol symbol)
        {
            var names = new Stack<string>();
            for (var current = symbol; !current.IsGlobalNamespace; current = current.ContainingNamespace)
            {
                names.Push(current.MetadataName);
            }

            return string.Join(".", names);
        }
    }
}
