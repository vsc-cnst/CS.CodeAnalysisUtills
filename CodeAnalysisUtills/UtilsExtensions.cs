using System.Reflection;
using CodeAnalysisUtills.ProjectAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalysisUtills.Extensions
{
    public enum ComparisonType
    {
        StartsWith,
        EndsWith,
        Contains,
        Equals,
    }

    public static class Utils
    {
        /// <summary>
        /// Finds all classes in the specified namespaces of a document.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="targetNamespaces">namespaces to search for classes in. If null or empty, all namespaces will be searched.</param>
        /// <returns></returns>
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