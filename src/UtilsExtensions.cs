using System.Reflection;
using vsc.CodeAnalysisUtills.ProjectAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace vsc.CodeAnalysisUtills.Extensions
{
    /// <summary>
    /// Specifies how namespace names are compared with the target namespace names.
    /// </summary>
    public enum ComparisonType
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
    public static class Utils
    {
        /// <summary>
        /// Finds all class declarations in the specified namespaces of a document.
        /// </summary>
        /// <param name="document">The document to search.</param>
        /// <param name="targetNamespaces">The namespace names to search. If <see langword="null"/> or empty, all namespaces are searched.</param>
        /// <param name="comparisonType">The method used to compare namespace names with <paramref name="targetNamespaces"/>.</param>
        /// <returns>The class declarations found in the document.</returns>
        public static IEnumerable<ClassDeclarationSyntax> GetClasses(this Document document, string[] targetNamespaces, ComparisonType comparisonType = ComparisonType.Equals)
        {

            var syntaxTree = document.GetSyntaxTreeAsync().Result;
            var root = document.GetSyntaxRootAsync().Result;

            List<SyntaxNode> namespaces;

            if (targetNamespaces == null || targetNamespaces.Length == 0)
            {
                namespaces = root!.ChildNodes().ToList();
            }
            else
            {
                namespaces = root!
                    .ChildNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .Where(n => comparisonType switch
                    {
                        ComparisonType.StartsWith => targetNamespaces.Any(tn => n.Name.ToString().StartsWith(tn)),
                        ComparisonType.EndsWith => targetNamespaces.Any(tn => n.Name.ToString().EndsWith(tn)),
                        ComparisonType.Contains => targetNamespaces.Any(tn => n.Name.ToString().Contains(tn)),
                        ComparisonType.Equals => targetNamespaces.Any(tn => n.Name.ToString() == tn),
                        _ => throw new ArgumentOutOfRangeException(nameof(comparisonType), $"Unsupported comparison type: {comparisonType}")
                    })
                    .SelectMany(n => n.ChildNodes())
                    .ToList();
            }

            var classes = namespaces
                .SelectMany(n => n.ChildNodes())
                .OfType<ClassDeclarationSyntax>()
                .Where(n => n is ClassDeclarationSyntax)
                .ToList();

            return classes;
        }


        /// <summary>
        /// Retrieves all runtime types declared in the project's documents, optionally filtered by namespace.
        /// </summary>
        /// <param name="project">The project that contains the documents to search.</param>
        /// <param name="compilation">The compilation used to load the runtime assembly.</param>
        /// <param name="targetNamespaces">The namespace names to search. If <see langword="null"/> or empty, all namespaces are searched.</param>
        /// <param name="comparisonType">The method used to compare namespace names with <paramref name="targetNamespaces"/>.</param>
        /// <returns>The distinct runtime types declared in the project.</returns>
        public static IEnumerable<Type> GetAllClasses(Project project, Compilation compilation, string[] targetNamespaces, ComparisonType comparisonType = ComparisonType.Equals)
        {
            Assembly assembly = CodeAnalysisExtensions.LoadAssembly(compilation);

            var types = project.Documents
                        .AsParallel()
                        .Select(d => (document: d, semanticModel: d.GetSemanticModelAsync().Result))
                        .Where(i => i.semanticModel != null)
                        .Cast<(Document document, SemanticModel semanticModel)>()
                        .SelectMany(i => ToTypes(GetClasses(i.document, targetNamespaces, comparisonType), assembly, i.semanticModel))
                        .Distinct()
                        .ToList();

            return types!;
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
            return classDeclarationSyntaxes
                .Select(classDeclaration => {

                    var classDeclaredSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                    var classFullyQualifiedName = classDeclaredSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    return assembly.GetType(classFullyQualifiedName?.Replace("global::", "") ?? string.Empty);
                })
                .Where(type => type != null)
                .Cast<Type>()
                .Distinct()
                ;
        }

    }
   
}