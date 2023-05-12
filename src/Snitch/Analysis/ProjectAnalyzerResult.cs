using System;
using System.Collections.Generic;
using System.Linq;

namespace Snitch.Analysis
{
    internal sealed class ProjectAnalyzerResult
    {
        private readonly Project _project;
        private readonly List<PackageToRemove> _packages;
        private readonly List<ProjectReferenceToRemove> _projectReferences;

        public string Project => _project.Name;
        public IReadOnlyList<ProjectReferenceToRemove> CanBeRemovedProjects { get; }
        public IReadOnlyList<PackageToRemove> CanBeRemoved { get; }
        public IReadOnlyList<PackageToRemove> MightBeRemoved { get; }
        public IReadOnlyList<Package> PreReleasePackages { get; }

        public bool NoPackagesToRemove => CanBeRemoved.Count == 0 && MightBeRemoved.Count == 0;

        public bool NothingToRemove => CanBeRemoved.Count == 0 && MightBeRemoved.Count == 0 && CanBeRemovedProjects.Count == 0;

        public bool HasPreReleases => PreReleasePackages.Count > 0;

        public ProjectAnalyzerResult(Project project, IEnumerable<PackageToRemove> packages, IEnumerable<ProjectReferenceToRemove> projectReferences)
        {
            _project = project;
            _packages = new List<PackageToRemove>(packages ?? throw new ArgumentNullException(nameof(packages)));
            _projectReferences = new List<ProjectReferenceToRemove>(projectReferences ?? throw new ArgumentNullException(nameof(projectReferences)));

            CanBeRemoved = new List<PackageToRemove>(packages.Where(p => p.CanBeRemoved));
            MightBeRemoved = new List<PackageToRemove>(packages.Where(p => p.VersionMismatch));
            PreReleasePackages = new List<Package>(project.Packages.Where(p => p.Version != null && p.Version.IsPrerelease));

            CanBeRemovedProjects = new List<ProjectReferenceToRemove>(projectReferences.Where(p => p.CanBeRemoved));
        }

        public ProjectAnalyzerResult Filter(string[]? packages)
        {
            if (packages == null)
            {
                return this;
            }

            var filtered = _packages.Where(p => !packages.Contains(p.Package.Name, StringComparer.OrdinalIgnoreCase));
            return new ProjectAnalyzerResult(_project, filtered, _projectReferences);
        }
    }
}
