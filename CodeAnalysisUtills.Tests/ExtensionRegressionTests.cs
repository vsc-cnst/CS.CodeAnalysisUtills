using System.Collections;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using vsc.CodeAnalysisUtills.Extensions;

namespace CodeAnalysisUtills.Tests
{
    [TestClass]
    public sealed class ExtensionRegressionTests
    {
        // Verifies type predicates for nullable, collection, enumerable and List<T> detection, including null inputs.
        [TestMethod]
        public void TypePredicatesDetectNullableAndCollectionTypes()
        {
            Check(typeof(int?).IsBasicType(), "Nullable primitive is basic");
            Check(typeof(decimal?).IsBasicType(), "Nullable decimal is basic");
            Check(typeof(DayOfWeek?).IsBasicType(), "Nullable enum is basic");
            Check(!typeof(List<int>).IsBasicType(), "Generic collection is not basic");
            Check(!((Type?)null).IsBasicType(), "Null type is not basic");
            Check(typeof(int?).IsNullableValueType(), "Nullable value type is detected");
            Check(!typeof(string).IsNullableValueType(), "Reference type is not Nullable<T>");
            Check(typeof(int[]).IsIEnumerable(), "Array implements generic enumerable");
            Check(typeof(int[]).IsICollection(), "Array implements generic collection");
            Check(typeof(StringList).IsIEnumerable(), "Non-generic list subclass is enumerable");
            Check(typeof(StringList).IsICollection(), "Non-generic list subclass is a collection");
            Check(typeof(IEnumerable<>).IsIEnumerable(), "Open enumerable interface is detected");
            Check(typeof(ICollection<>).IsICollection(), "Open collection interface is detected");
            Check(typeof(IMultipleCollections).IsIEnumerable(), "Multiple enumerable interfaces are supported");
            Check(typeof(IMultipleCollections).IsICollection(), "Multiple collection interfaces are supported");
            Check(!typeof(Impostor<int>).IsIEnumerable(), "Unrelated same-named interface is rejected");
            Check(!typeof(Impostor<int>).IsICollection(), "Unrelated collection interface is rejected");
            Check(typeof(List<int>).IsList(), "List<T> is detected");
            Check(typeof(List<>).IsList(), "Open List<T> is detected");
            Check(!typeof(StringList).IsList(), "List subclasses are not exact List<T>");
            Check(!((Type?)null).IsIEnumerable(), "Null is not enumerable");
            Check(!((Type?)null).IsICollection(), "Null is not a collection");
            Check(!((Type?)null).IsList(), "Null is not a list");
        }

        // Verifies inheritance and base-type helpers, including self inclusion, filters, and single-pass input handling.
        [TestMethod]
        public void HierarchyHelpersHandleInclusionAndSinglePassInputs()
        {
            Check(typeof(Derived).InheritsFrom(typeof(Base)), "Derived type inherits from base");
            Check(typeof(Derived).InheritsFrom(typeof(Base), true), "Including self still matches derived types");
            Check(!typeof(Base).InheritsFrom(typeof(Base)), "Self is excluded by default");
            Check(typeof(Base).InheritsFrom(typeof(Base), true), "Self is included when requested");
            Check(typeof(Base).IsBaseTypeOf(typeof(Derived), true), "Including self still matches child types");
            Check(!typeof(Base).IsBaseTypeOf(typeof(Base)), "Base check excludes self by default");
            Check(typeof(Base).IsBaseTypeOf(typeof(Base), true), "Base check can include self");
            Check(!typeof(Unrelated).InheritsFrom(typeof(Base), true), "Unrelated type is excluded");
            Check(typeof(StringList).InheritsFrom(typeof(IEnumerable<string>)), "Interface assignability is preserved");

            var hierarchy = new[] { typeof(Base), typeof(Derived), typeof(Leaf), typeof(Unrelated), typeof(Derived) };
            Check(hierarchy.WhereInheritsFrom(typeof(Base), true).ToHashSet().SetEquals([typeof(Base), typeof(Derived), typeof(Leaf)]), "Inheritance filter includes self and descendants without duplicates");
            Check(hierarchy.WhereIsBaseTypeOf(typeof(Base), true).ToHashSet().SetEquals([typeof(Base), typeof(Derived), typeof(Leaf)]), "Base-type filter forwards inclusion flag");
            Check(hierarchy.WhereIsBaseTypeOf(typeof(Base)).ToHashSet().SetEquals([typeof(Derived), typeof(Leaf)]), "Base-type filter excludes self by default");

            var roots = new SinglePassTypes(hierarchy).RootElements().ToArray();
            Check(roots.ToHashSet().SetEquals([typeof(Base), typeof(Unrelated)]), "Roots enumerate their source once");
            var ancestors = typeof(Leaf).BaseTypes(new SinglePassTypes([typeof(Base), typeof(Derived)])).ToArray();
            Check(ancestors.SequenceEqual([typeof(Derived), typeof(Base)]), "Filtered ancestors enumerate their universe once");
            var groups = new SinglePassTypes(hierarchy).GroupByRootElements();
            Check(groups[typeof(Base)].SequenceEqual([typeof(Derived), typeof(Leaf)]), "Grouping uses a single source snapshot");
            Check(groups[typeof(Base)].SequenceEqual([typeof(Derived), typeof(Leaf)]), "Grouped results can be enumerated again");
            Check(!groups[typeof(Unrelated)].Any(), "Root with no descendants has an empty group");
            Check(new[] { typeof(Base), typeof(Leaf) }.RootElements().Count() == 2, "Root detection retains immediate-base membership semantics");
        }

        // Verifies member predicates, member data type resolution, null tolerance, and MemberInfoNameTypeComparer behavior.
        [TestMethod]
        public void MemberPredicatesAndComparerHandleNullsAndCollections()
        {
            var nullableMember = typeof(Members).GetProperty(nameof(Members.NullableValue))!;
            var arrayMember = typeof(Members).GetProperty(nameof(Members.ArrayValue))!;
            var listMember = typeof(Members).GetProperty(nameof(Members.ListValue))!;
            var objectMember = typeof(Members).GetProperty(nameof(Members.ObjectValue))!;
            var fieldMember = typeof(Members).GetField(nameof(Members.FieldValue))!;
            var method = typeof(Members).GetMethod(nameof(Members.Overloaded), Type.EmptyTypes)!;
            var overload = typeof(Members).GetMethod(nameof(Members.Overloaded), [typeof(int)])!;
            Check(nullableMember.MemberIsBasic(), "Nullable member is basic");
            Check(nullableMember.GetMemberDataType() == typeof(int?), "Property data type is returned");
            Check(fieldMember.MemberIsBasic(), "Nullable field is basic");
            Check(fieldMember.GetMemberDataType() == typeof(int?), "Field data type is returned");
            Check(method.GetMemberDataType() == typeof(int), "Method return type is returned");
            Check(arrayMember.MemberIsIEnumerable() && arrayMember.MemberIsICollection(), "Array member is a collection");
            Check(!arrayMember.MemberIsClass(), "Array member is not a non-collection class");
            Check(objectMember.MemberIsClass(), "Ordinary object member is a class");
            Check(listMember.MemberIsList(), "List member is detected");
            Check(method.MemberIsMethod(), "Method member is detected");
            Check(!method.MemberIsBasic() && !method.MemberIsClass() && !method.MemberIsList(), "Method predicates remain distinct from return-type predicates");
            MemberInfo? nullMember = null;
            Check(nullMember.GetMemberDataType() == null, "Null member has no data type");
            Check(!nullMember.MemberIsBasic() && !nullMember.MemberIsClass() && !nullMember.MemberIsList()
                && !nullMember.MemberIsICollection() && !nullMember.MemberIsIEnumerable() && !nullMember.MemberIsMethod(), "Member predicates tolerate null");

            var comparer = new MemberInfoNameTypeComparer();
            Check(comparer.Equals(null, null), "Comparer considers two nulls equal");
            Check(!comparer.Equals(method, null), "Comparer rejects one null");
            Check(comparer.Equals(method, overload), "Comparer intentionally groups overloads by name and declaring type");
            Check(comparer.GetHashCode(method) == comparer.GetHashCode(overload), "Equal members have equal hash codes");
            Check(!comparer.Equals(method, typeof(OtherMembers).GetMethod(nameof(Members.Overloaded))), "Declaring type participates in equality");
        }

        // Compiles an in-memory project and verifies class discovery by namespace filters, match kinds, cancellation, and mapping to runtime types.
        [TestMethod]
        public async Task ClassDiscoveryFindsAndMapsAllSupportedDeclarations()
        {
            using var workspace = new AdhocWorkspace();
            var trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
            var references = trustedAssemblies.Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
            var project = workspace.AddProject(ProjectInfo.Create(
                ProjectId.CreateNewId(), VersionStamp.Create(), "Discovery", "DiscoveryChecks", LanguageNames.CSharp,
                parseOptions: new CSharpParseOptions(LanguageVersion.CSharp14),
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                metadataReferences: references));
            project = project.AddDocument("Global.cs", SourceText.From("public class GlobalClass {} public partial class GlobalPartial {}")).Project;
            project = project.AddDocument("Block.cs", SourceText.From("namespace A { public class Outer<T> { public class Inner<U> {} } public class Direct {} namespace B { public class NestedNamespace {} } }")).Project;
            project = project.AddDocument("Scoped.cs", SourceText.From("namespace A.B; public class Scoped {} public partial class Partial {}")).Project;
            project = project.AddDocument("Partial.cs", SourceText.From("namespace A.B; public partial class Partial {}")).Project;
            project = project.AddDocument("GlobalPartial.cs", SourceText.From("public partial class GlobalPartial {}")).Project;
            project = project.AddDocument("Escaped.cs", SourceText.From("namespace @event; public class @class {}")).Project;

            var blockDocument = project.Documents.Single(document => document.Name == "Block.cs");
            Check((await blockDocument.GetClassesAsync()).Count() == 4, "Unfiltered discovery includes nested classes and namespaces");
            Check((await blockDocument.GetClassesAsync(["A"])).Count() == 3, "Exact namespace filter finds direct and nested classes");
            Check((await blockDocument.GetClassesAsync(["A.B"])).Single().Identifier.ValueText == "NestedNamespace", "Nested namespace resolves to its full name");
            Check((await blockDocument.GetClassesAsync(["A"], NamespaceMatchKind.StartsWith)).Count() == 4, "StartsWith namespace matching works");
            Check((await blockDocument.GetClassesAsync(["B"], NamespaceMatchKind.EndsWith)).Count() == 1, "EndsWith namespace matching works");
            Check((await blockDocument.GetClassesAsync(["."], NamespaceMatchKind.Contains)).Count() == 1, "Contains namespace matching works");
            Check(!(await blockDocument.GetClassesAsync(["a"], NamespaceMatchKind.StartsWith)).Any(), "Namespace matching is case-sensitive");
            Check((await blockDocument.GetClassesAsync([])).Count() == 4, "Empty namespace filter searches all namespaces");
            var scopedDocument = project.Documents.Single(document => document.Name == "Scoped.cs");
            Check((await scopedDocument.GetClassesAsync(["A.B"])).Count() == 2, "File-scoped namespaces are supported");
            var globalDocument = project.Documents.Single(document => document.Name == "Global.cs");
            Check((await globalDocument.GetClassesAsync([string.Empty])).Count() == 2, "Global namespace can be filtered explicitly");
            var escapedDocument = project.Documents.Single(document => document.Name == "Escaped.cs");
            Check((await escapedDocument.GetClassesAsync(["event"])).Count() == 1, "Escaped namespace uses its metadata name");
            await CheckThrowsAsync<ArgumentOutOfRangeException>(async () => await blockDocument.GetClassesAsync(comparisonType: (NamespaceMatchKind)999), "Invalid match kind is rejected");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await CheckThrowsAsync<OperationCanceledException>(async () => await blockDocument.GetClassesAsync(cancellationToken: cancellation.Token), "Discovery supports cancellation");

            var compilation = (await project.GetCompilationAsync())!;
            using var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream);
            Check(emitResult.Success, "Regression source compiles: " + string.Join(Environment.NewLine, emitResult.Diagnostics));
            var assembly = Assembly.Load(stream.ToArray());
            var runtimeTypes = (await project.GetAllClassesAsync(assembly)).ToArray();
            string[] expectedNames = ["GlobalClass", "GlobalPartial", "A.Outer`1", "A.Outer`1+Inner`1", "A.Direct", "A.B.NestedNamespace", "A.B.Scoped", "A.B.Partial", "event.class"];
            Check(runtimeTypes.Select(type => type.FullName).ToHashSet().SetEquals(expectedNames), "Runtime lookup handles global, nested, generic, file-scoped, partial and escaped types");
            Check(runtimeTypes.Length == expectedNames.Length, "Partial declarations map to distinct runtime types");
            Check((await project.GetAllClassesAsync(assembly, ["A.B"])).Count() == 3, "Project discovery applies namespace filters");
            Check((await project.GetAllClassesAsync(compilation)).Count() == expectedNames.Length, "Compilation overload emits and discovers runtime types");
            var semanticModel = (await blockDocument.GetSemanticModelAsync())!;
            var mappedTypes = (await blockDocument.GetClassesAsync()).ToTypes(assembly, semanticModel).ToArray();
            Check(mappedTypes.Any(type => type.FullName == "A.Outer`1+Inner`1"), "ToTypes resolves nested generic metadata names");
        }

        // Asserts a condition, using the given name as the failure message.
        private static void Check(bool condition, string name)
        {
            Assert.IsTrue(condition, name);
        }

        // Asserts that the awaited action throws TException; fails with the given name otherwise.
        private static async Task CheckThrowsAsync<TException>(Func<Task> action, string name) where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException)
            {
                return;
            }

            Assert.Fail(name);
        }

        private class Base;
        private class Derived : Base;
        private class Leaf : Derived;
        private class Unrelated;
        private class StringList : List<string>;
        private interface IMultipleCollections : ICollection<int>, ICollection<string>;
        private class Impostor<T> : UnrelatedInterfaces.IEnumerable<T>, UnrelatedInterfaces.ICollection<T>;

        private class Members
        {
            public int? NullableValue { get; set; }
            public int[] ArrayValue { get; set; } = [];
            public List<int> ListValue { get; set; } = [];
            public object ObjectValue { get; set; } = new();
            public int? FieldValue = 1;
            public int Overloaded() => 0;
            public int Overloaded(int value) => value;
        }

        private class OtherMembers
        {
            public int Overloaded() => 0;
        }

        // Test double that throws if enumerated more than once, used to detect repeated enumeration.
        private sealed class SinglePassTypes(IEnumerable<Type> types) : IEnumerable<Type>
        {
            private bool enumerated;

            public IEnumerator<Type> GetEnumerator()
            {
                if (enumerated) throw new InvalidOperationException("Source was enumerated more than once.");
                enumerated = true;
                return types.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static class UnrelatedInterfaces
        {
            internal interface IEnumerable<T>;
            internal interface ICollection<T>;
        }
    }
}
