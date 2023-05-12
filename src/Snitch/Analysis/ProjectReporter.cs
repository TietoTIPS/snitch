using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Spectre.Console;

namespace Snitch.Analysis
{
    internal class ProjectReporter
    {
        private readonly IAnsiConsole _console;

        public ProjectReporter(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public void WriteToConsole([NotNull] List<ProjectAnalyzerResult> results, bool noPreRelease)
        {
            var resultsWithPackageToRemove = results.Where(r => r.CanBeRemoved.Count > 0).ToList();
            var resultsWithProjectToRemove = results.Where(r => r.CanBeRemovedProjects.Count > 0).ToList();
            var resultsWithPackageMayBeRemove = results.Where(r => r.MightBeRemoved.Count > 0).ToList();
            var resultsWithPreReleases = results.Where(r => r.PreReleasePackages.Count > 0).ToList();

            if (results.All(x => x.NothingToRemove) && (!noPreRelease || resultsWithPreReleases.Count == 0))
            {
                // Output the result.
                _console.WriteLine();
                _console.MarkupLine("[green]Everything looks good![/]");
                _console.WriteLine();
                return;
            }

            var report = new Grid();
            report.AddColumn();

            if (resultsWithPackageToRemove.Count > 0)
            {
                foreach (var (_, _, last, result) in resultsWithPackageToRemove.Enumerate())
                {
                    var table = new Table().BorderColor(Color.Grey).Expand();
                    table.AddColumns("[grey]Package[/]", "[grey]Referenced by[/]");
                    foreach (var item in result.CanBeRemoved)
                    {
                        table.AddRow(
                            $"[green]{item.Package.Name}[/]",
                            $"[aqua]{item.Original.Project.Name}[/]");
                    }

                    report.AddRow($" [yellow]Packages that can be removed from[/] [aqua]{result.Project}[/]:");
                    report.AddRow(table);

                    if (!last || (last && resultsWithPackageMayBeRemove.Count > 0))
                    {
                        report.AddEmptyRow();
                    }
                }
            }

            if (resultsWithProjectToRemove.Count > 0)
            {
                foreach (var (_, _, last, result) in resultsWithProjectToRemove.Enumerate())
                {
                    var table = new Table().BorderColor(Color.Grey).Expand();
                    table.AddColumns("[grey]Project[/]", "[grey]Referenced by[/]");
                    foreach (var item in result.CanBeRemovedProjects)
                    {
                        table.AddRow(
                            $"[green]{item.ReferencedProject.Name}[/]",
                            $"[aqua]{item.Original.Project.Name}[/]");
                    }

                    report.AddRow($" [yellow]Projects that can be removed from[/] [aqua]{result.Project}[/]:");
                    report.AddRow(table);

                    if (!last || (last && resultsWithPackageMayBeRemove.Count > 0))
                    {
                        report.AddEmptyRow();
                    }
                }
            }

            if (resultsWithPackageMayBeRemove.Count > 0)
            {
                foreach (var (_, _, last, result) in resultsWithPackageMayBeRemove.Enumerate())
                {
                    var table = new Table().BorderColor(Color.Grey).Expand();
                    table.AddColumns("[grey]Package[/]", "[grey]Version[/]", "[grey]Reason[/]");

                    foreach (var item in result.MightBeRemoved)
                    {
                        if (item.Package.IsGreaterThan(item.Original.Package, out var indeterminate))
                        {
                            var name = item.Original.Project.Name;
                            var version = item.Original.Package.GetVersionString();
                            var verb = indeterminate ? "Might be updated from" : "Updated from";
                            var reason = $"[grey]{verb}[/] [silver]{version}[/] [grey]in[/] [aqua]{name}[/]";

                            table.AddRow(
                                $"[green]{item.Package.Name}[/]",
                                item.Package.GetVersionString(),
                                reason);
                        }
                        else
                        {
                            var name = item.Original.Project.Name;
                            var version = item.Original.Package.GetVersionString();
                            var verb = indeterminate ? "Does not match" : "Downgraded from";
                            var reason = $"[grey]{verb}[/] [silver]{version}[/] [grey]in[/] [aqua]{name}[/]";

                            table.AddRow(
                                $"[green]{item.Package.Name}[/]",
                                item.Package.GetVersionString(),
                                reason);
                        }
                    }

                    report.AddRow($" [yellow]Packages that [u]might[/] be removed from[/] [aqua]{result.Project}[/]:");
                    report.AddRow(table);

                    if (!last)
                    {
                        report.AddEmptyRow();
                    }
                }
            }

            if (noPreRelease && resultsWithPreReleases.Count > 0)
            {
                report.AddEmptyRow();
                report.AddRow(" [yellow]Projects with pre-release package references:[/]");
                var packagesByProject = resultsWithPreReleases.SelectMany(x => x.PreReleasePackages, (project, package) => new
                                                              {
                                                                  Project = project.Project,
                                                                  PackageName = package.Name,
                                                                  Version = package.Version,
                                                              })
                                                              .OrderBy(o => o.Project)
                                                              .ToList();

                var table = new Table().BorderColor(Color.Grey).Expand();
                table.AddColumns("[grey]Project[/]", "[grey]Package[/]", "[grey]Version[/]");
                foreach (var item in packagesByProject)
                {
                    table.AddRow(
                        $"[green]{item.Project}[/]",
                        $"[yellow]{item.PackageName}[/]",
                        $"{item.Version}");
                }

                report.AddRow(table);
            }

            _console.WriteLine();
            _console.Write(
                new Panel(report)
                    .RoundedBorder()
                    .BorderColor(Color.Grey));
        }
    }
}
