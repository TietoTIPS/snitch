using System;
using System.Diagnostics;

namespace Snitch.Analysis
{
    [DebuggerDisplay("{ProjectReferenceDescription(),nq}")]
    internal class ProjectReferenceToRemove
    {
        public Project Project { get; }
        public Project ReferencedProject { get; }

        public ProjectReferencedProject Original { get; }

        public bool CanBeRemoved => true;

        public ProjectReferenceToRemove(Project project, Project referencedProject, ProjectReferencedProject original)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            ReferencedProject = referencedProject ?? throw new ArgumentNullException(nameof(referencedProject));
            Original = original ?? throw new ArgumentNullException(nameof(original));
        }

        private string ProjectReferenceDescription()
        {
            return $"{Project.Name}: {ReferencedProject.Name} ({Original.Project.Name})";
        }
    }
}