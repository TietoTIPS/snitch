using System;
using System.Collections.Generic;
using System.Linq;

using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;

namespace Snitch.Analysis
{
    internal sealed class ProjectAnalyzer
    {
        public ProjectAnalyzerResult Analyze(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            // Analyze the project.
            var packageResult = new List<PackageToRemove>();
            AnalyzeProject(project, project, packageResult);

            var projectReferenceResult = new List<ProjectReferenceToRemove>();
            AnalyzeProjectReferences(project, project, projectReferenceResult);

            if (project.LockFilePath != null)
            {
                // Now prune stuff that we're not interested in removing
                // such as private package references and analyzers.
                packageResult = PruneResults(project, packageResult);
            }

            return new ProjectAnalyzerResult(project, packageResult, projectReferenceResult);
        }

        private List<ProjectPackage> AnalyzeProject(Project root, Project project, List<PackageToRemove> result)
        {
            var accumulated = new List<ProjectPackage>();
            result ??= new List<PackageToRemove>();

            if (project.ProjectReferences.Count > 0)
            {
                // Iterate through all project references.
                foreach (var child in project.ProjectReferences)
                {
                    // Analyze the project recursively.
                    foreach (var item in AnalyzeProject(root, child, result))
                    {
                        // Didn't exist previously in the list of accumulated packages?
                        if (!accumulated.ContainsPackage(item.Package))
                        {
                            accumulated.Add(new ProjectPackage(item.Project, item.Package));
                        }
                    }
                }

                // Was any package in the current project references
                // by one of the projects referenced by the project?
                foreach (var package in project.Packages)
                {
                    var found = accumulated.FindProjectPackage(package);
                    if (found != null)
                    {
                        if (!result.ContainsPackage(found.Package))
                        {
                            if (project.Name.Equals(root.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Add(new PackageToRemove(project, package, found));
                            }
                        }
                    }
                    else
                    {
                        AddToAccumulated(package);
                    }
                }
            }
            else
            {
                foreach (var item in project.Packages)
                {
                    if (!accumulated.ContainsPackage(item))
                    {
                        AddToAccumulated(item);
                    }
                }
            }

            void AddToAccumulated(Package package)
            {
                if (package.PrivateAssets != null && (package.PrivateAssets.Contains("compile", StringComparison.InvariantCultureIgnoreCase) || package.PrivateAssets.Contains("all", StringComparison.InvariantCultureIgnoreCase)))
                {
                    return;
                }

                // Add the package to the list of accumulated packages.
                accumulated.Add(new ProjectPackage(project, package));
            }

            return accumulated;
        }

        private List<ProjectReferencedProject> AnalyzeProjectReferences(Project root, Project project, List<ProjectReferenceToRemove> projectReferenceResult)
        {
            var accumulatedProjects = new List<ProjectReferencedProject>();
            projectReferenceResult ??= new List<ProjectReferenceToRemove>();

            if (project.IsSdkProject && project.ProjectReferences.Count > 0)
            {
                // Iterate through all project references.
                foreach (var child in project.ProjectReferences)
                {
                    // Analyze the project recursively.
                    foreach (var item in AnalyzeProjectReferences(root, child, projectReferenceResult))
                    {
                        accumulatedProjects.Add(new ProjectReferencedProject(item.Project, item.ReferencedProject));
                    }
                }

                foreach (var referenceProject in project.ProjectReferences)
                {
                    if (project == root)
                    {
                        var found = accumulatedProjects.FindProjectReference(referenceProject);
                        if (found != null)
                        {
                            if (!projectReferenceResult.ContainsProjectReference(found.ReferencedProject))
                            {
                                projectReferenceResult.Add(new ProjectReferenceToRemove(project, referenceProject, found));
                            }
                        }
                    }
                    else
                    {
                        accumulatedProjects.Add(new ProjectReferencedProject(project, referenceProject));
                    }
                }
            }

            return accumulatedProjects;
        }

        private static List<PackageToRemove> PruneResults(Project project, List<PackageToRemove> packages)
        {
            // Read the lockfile.
            var lockfile = new LockFileFormat().Read(project.LockFilePath);

            // Find the expected target.
            var framework = NuGetFramework.Parse(project.TargetFramework);
            var target = lockfile.PackageSpec.TargetFrameworks.FirstOrDefault(
                x => x.FrameworkName.Framework.Equals(framework.Framework, StringComparison.OrdinalIgnoreCase));

            // Could we not find the target?
            if (target == null)
            {
                throw new InvalidOperationException("Could not determine target framework");
            }

            var result = new List<PackageToRemove>();
            foreach (var package in packages)
            {
                // Try to find the dependency.
                var dependency = target.Dependencies.FirstOrDefault(
                    x => x.Name.Equals(package.Package.Name, StringComparison.OrdinalIgnoreCase));

                if (dependency != null)
                {
                    // Auto referenced or private package?
                    if (dependency.AutoReferenced ||
                        dependency.SuppressParent == LibraryIncludeFlags.All)
                    {
                        continue;
                    }
                }

                result.Add(package);
            }

            return result;
        }
    }
}
