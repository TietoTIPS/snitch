using System;
using System.Collections.Generic;
using System.Linq;

namespace Snitch.Analysis;

internal static class ProjectReferenceExtensions
{
    public static bool ContainsReference(this IEnumerable<ProjectReferencedProject> source, Project project)
    {
        return source.Any(x => x.ReferencedProject.Name.Equals(project.Name, StringComparison.OrdinalIgnoreCase));
    }

    public static ProjectReferencedProject? FindProjectReference(this IEnumerable<ProjectReferencedProject> source, Project project)
    {
        return source.FirstOrDefault(p => p.ReferencedProject.Name.Equals(project.Name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsProjectReference(this IEnumerable<ProjectReferenceToRemove> source, Project project)
    {
        return source.Any(x => x.ReferencedProject.Name.Equals(project.Name, StringComparison.OrdinalIgnoreCase));
    }
}