
#if NET48 || NET7_0 || NET8_0_OR_GREATER

using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace vsc.CodeAnalysisUtills.ProjectAnalysis
{
    /// <summary>
    /// Provides helper methods for opening, compiling, and loading MSBuild projects with Roslyn.
    /// </summary>
    /// <remarks>
    /// Available only when targeting .NET Framework 4.8, .NET 7, or .NET 8 and later.
    /// </remarks>
    public static class CodeAnalysisExtensions
    {
        /// <summary>
        /// Opens the MSBuild project at the specified path.
        /// </summary>
        /// <param name="projectPath">The path to the MSBuild project file.</param>
        /// <returns>The <see cref="Project"/> representing the opened project.</returns>
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
        /// Opens the MSBuild project at the specified path and gets its compilation.
        /// </summary>
        /// <param name="projectPath">The path to the MSBuild project file.</param>
        /// <returns>A tuple containing the <see cref="Project"/> and its <see cref="Compilation"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the project compilation cannot be created.</exception>
        public static async Task<(Project project, Compilation compilation)> GetProjectCompilation(string projectPath)
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
        /// Emits the compilation to memory and loads the resulting assembly.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> to emit.</param>
        /// <returns>The loaded <see cref="Assembly"/>.</returns>
        /// <exception cref="Exception">Thrown when the compilation fails to emit.</exception>
        public static Assembly LoadAssembly(this Compilation compilation)
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

        /// <summary>
        /// Opens the MSBuild project at the specified path, gets its compilation, and loads the assemblies referenced by its metadata references.
        /// </summary>
        /// <param name="projPath">The path to the MSBuild project file.</param>
        /// <returns>A tuple containing the <see cref="Project"/>, its <see cref="Compilation"/>, and the loaded assemblies.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the project compilation cannot be created.</exception>
        public static async Task<(Project project, Compilation compilation, List<Assembly> assemblies)> GetProjectCompilationAssemblies(string projPath)
        { 
            var (proj, comp) = await GetProjectCompilation(projectPath: projPath);
            var assemblies = proj.MetadataReferences.Where(m => m.Display is not null).Select(m => Assembly.LoadFrom(m.Display!)).ToList();
            return (proj, comp, assemblies);
        }

        /// <summary>
        /// Gets the compilation for the specified Roslyn project and loads the assemblies referenced by its metadata references.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to compile.</param>
        /// <returns>A tuple containing the <see cref="Project"/>, its <see cref="Compilation"/>, and the loaded assemblies.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the project compilation cannot be created.</exception>
        public static async Task<(Project project, Compilation compilation, List<Assembly> assemblies)> GetProjectCompilationAssemblies(this Project project)
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

    }
}

#endif