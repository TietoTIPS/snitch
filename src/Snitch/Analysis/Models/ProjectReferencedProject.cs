using System;
using System.Diagnostics;

namespace Snitch.Analysis
{
    [DebuggerDisplay("{ProjectReferenceDescription(),nq}")]
    internal sealed class ProjectReferencedProject
    {
        public Project Project { get; }
        public Project ReferencedProject { get; }

        public ProjectReferencedProject(Project project, Project referencedProject)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            ReferencedProject = referencedProject ?? throw new ArgumentNullException(nameof(referencedProject));
        }

        private string ProjectReferenceDescription()
        {
            return $"{Project.Name}: {ReferencedProject.Name}";
        }
    }
}
