
#if NET48 || NET7_0 || NET8_0_OR_GREATER

using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeAnalysisUtills.ProjectAnalysis
{
    /// <summary>
    /// Provides extension methods for opening and compiling MSBuild projects with Roslyn.
    /// </summary>
    /// <remarks>
    /// Not available on .NET 6.
    /// </remarks>
    public static class CodeAnalysisExtensions
    {
        /// <summary>
        /// Opens the MSBuild project at the specified path and returns the Roslyn project.
        /// </summary>
        /// <remarks>
        /// Not available on .NET 6.
        /// </remarks>
        public static async Task<Project> GetProject(string projectPath)
        {
            using var workspace = MSBuildWorkspace.Create();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var project = await workspace.OpenProjectAsync(projectPath);
            Console.WriteLine($"Project opened in {sw.ElapsedMilliseconds / 1000d}s");
            sw.Reset();

            return project;
        }

        /// <summary>
        /// Opens the MSBuild project at the specified path and compiles it.
        /// </summary>
        /// <remarks>
        /// Not available on .NET 6.
        /// </remarks>
        public static async Task<(Project project, Compilation compilation)> CompileProject(string projectPath)
        {
            var project = await GetProject(projectPath);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var compilation = await project.GetCompilationAsync();

            Console.WriteLine($"Project compiled in {sw.ElapsedMilliseconds / 1000d}s");

            if (compilation == null)
            {
                throw new InvalidOperationException("Failed to get project compilation.");
            }

            return (project, compilation);
        }

        /// <summary>
        /// Compiles the specified Roslyn project and loads the assemblies referenced by its metadata.
        /// </summary>
        /// <remarks>
        /// Not available on .NET 6.
        /// </remarks>
        public static async Task<(Project project, Compilation compilation, List<Assembly> assemblies)> CompileProject(Project project)
        {
            using var workspace = MSBuildWorkspace.Create();

            Stopwatch sw = new Stopwatch();

            var compilation = await project.GetCompilationAsync();
            Console.WriteLine($"Project compiled in {sw.ElapsedMilliseconds / 1000d}s");

            if (compilation == null)
                throw new InvalidOperationException("Failed to get project compilation.");

            return (
                project,
                compilation,
                assemblies: project.MetadataReferences.Where(m => m.Display is not null).Select(m => Assembly.LoadFrom(m.Display!)
            ).ToList());
        }

        /// <summary>
        /// Emits the compilation to memory and loads the resulting assembly.
        /// </summary>
        public static Assembly LoadAssembly(Compilation compilation)
        {
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();

            Assembly assembly;

            using (var ms = new MemoryStream())
            {
                var emitResult = compilation.Emit(ms);

                if (emitResult.Success)
                {
                    // Load the emitted assembly
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }
                else
                {
                    throw new Exception("No Assembly!");
                }
            }

            Console.WriteLine($"Assembly loaded in {sw.ElapsedMilliseconds / 1000m}s");
            sw.Stop();

            return assembly;
        }
    }
}

#endif