using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClaudeAI
{
    /// <summary>
    /// Service for analyzing Visual Studio solution and project files
    /// </summary>
    public static class SolutionAnalyzer
    {
        private static readonly string[] SupportedExtensions = {
            ".cs", ".vb", ".cpp", ".c", ".h", ".hpp", ".js", ".ts", ".html", ".css", ".xml",
            ".json", ".sql", ".py", ".java", ".php", ".rb", ".go", ".rs", ".swift"
        };

        /// <summary>
        /// Gets the current solution structure and file information
        /// </summary>
        public static SolutionInfo GetSolutionInfo()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                if (dte?.Solution == null)
                {
                    return new SolutionInfo { ErrorMessage = "No solution is currently open." };
                }

                var solutionInfo = new SolutionInfo
                {
                    SolutionName = Path.GetFileNameWithoutExtension(dte.Solution.FullName),
                    SolutionPath = dte.Solution.FullName,
                    Projects = new List<ProjectInfo>()
                };

                // Analyze each project in the solution
                foreach (Project project in dte.Solution.Projects)
                {
                    if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") // Solution folder
                        continue;

                    var projectInfo = AnalyzeProject(project);
                    if (projectInfo != null)
                    {
                        solutionInfo.Projects.Add(projectInfo);
                    }
                }

                return solutionInfo;
            }
            catch (Exception ex)
            {
                return new SolutionInfo { ErrorMessage = $"Error analyzing solution: {ex.Message}" };
            }
        }

        /// <summary>
        /// Gets the content of the currently active document
        /// </summary>
        public static FileContentInfo GetActiveDocumentContent()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                var activeDocument = dte?.ActiveDocument;

                if (activeDocument == null)
                {
                    return new FileContentInfo { ErrorMessage = "No active document." };
                }

                var textDocument = activeDocument.Object("TextDocument") as TextDocument;
                if (textDocument == null)
                {
                    return new FileContentInfo { ErrorMessage = "Active document is not a text document." };
                }

                var startPoint = textDocument.StartPoint;
                var endPoint = textDocument.EndPoint;
                var content = startPoint.CreateEditPoint().GetText(endPoint);

                return new FileContentInfo
                {
                    FileName = activeDocument.Name,
                    FilePath = activeDocument.FullName,
                    Content = content,
                    Language = GetLanguageFromExtension(Path.GetExtension(activeDocument.Name))
                };
            }
            catch (Exception ex)
            {
                return new FileContentInfo { ErrorMessage = $"Error reading active document: {ex.Message}" };
            }
        }

        /// <summary>
        /// Gets the currently selected text in the active document
        /// </summary>
        public static string GetSelectedText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
                var activeDocument = dte?.ActiveDocument;

                if (activeDocument == null)
                    return null;

                var textDocument = activeDocument.Object("TextDocument") as TextDocument;
                var selection = textDocument?.Selection;

                return selection?.Text;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets content of a specific file by path
        /// </summary>
        public static FileContentInfo GetFileContent(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new FileContentInfo { ErrorMessage = "File not found." };
                }

                if (!IsSupportedFile(filePath))
                {
                    return new FileContentInfo { ErrorMessage = "File type not supported for analysis." };
                }

                var content = File.ReadAllText(filePath);
                var fileName = Path.GetFileName(filePath);

                return new FileContentInfo
                {
                    FileName = fileName,
                    FilePath = filePath,
                    Content = content,
                    Language = GetLanguageFromExtension(Path.GetExtension(filePath))
                };
            }
            catch (Exception ex)
            {
                return new FileContentInfo { ErrorMessage = $"Error reading file: {ex.Message}" };
            }
        }

        private static ProjectInfo AnalyzeProject(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var projectInfo = new ProjectInfo
                {
                    Name = project.Name,
                    FullName = project.FullName,
                    Kind = project.Kind,
                    Files = new List<FileInfo>()
                };

                // Get project items
                AnalyzeProjectItems(project.ProjectItems, projectInfo.Files, "");

                return projectInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void AnalyzeProjectItems(ProjectItems items, List<FileInfo> fileList, string relativePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (items == null) return;

            try
            {
                foreach (ProjectItem item in items)
                {
                    var itemPath = Path.Combine(relativePath, item.Name);

                    if (item.Kind == "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}") // Physical file
                    {
                        var fullPath = item.FileNames[1];
                        if (IsSupportedFile(fullPath))
                        {
                            fileList.Add(new FileInfo
                            {
                                Name = item.Name,
                                FullPath = fullPath,
                                RelativePath = itemPath,
                                Language = GetLanguageFromExtension(Path.GetExtension(fullPath))
                            });
                        }
                    }
                    else if (item.Kind == "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}") // Physical folder
                    {
                        AnalyzeProjectItems(item.ProjectItems, fileList, itemPath);
                    }
                }
            }
            catch (Exception)
            {
                // Continue processing other items
            }
        }

        private static bool IsSupportedFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedExtensions.Contains(extension);
        }

        private static string GetLanguageFromExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".cs": return "C#";
                case ".vb": return "VB.NET";
                case ".cpp":
                case ".cc":
                case ".cxx": return "C++";
                case ".c": return "C";
                case ".h":
                case ".hpp": return "C/C++ Header";
                case ".js": return "JavaScript";
                case ".ts": return "TypeScript";
                case ".html": return "HTML";
                case ".css": return "CSS";
                case ".xml": return "XML";
                case ".json": return "JSON";
                case ".sql": return "SQL";
                case ".py": return "Python";
                case ".java": return "Java";
                case ".php": return "PHP";
                case ".rb": return "Ruby";
                case ".go": return "Go";
                case ".rs": return "Rust";
                case ".swift": return "Swift";
                default: return "Text";
            }
        }
    }

    /// <summary>
    /// Information about the current solution
    /// </summary>
    public class SolutionInfo
    {
        public string SolutionName { get; set; }
        public string SolutionPath { get; set; }
        public List<ProjectInfo> Projects { get; set; } = new List<ProjectInfo>();
        public string ErrorMessage { get; set; }

        public string GetSummary()
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                return ErrorMessage;

            var sb = new StringBuilder();
            sb.AppendLine($"Solution: {SolutionName}");
            sb.AppendLine($"Projects: {Projects.Count}");

            foreach (var project in Projects)
            {
                sb.AppendLine($"  - {project.Name} ({project.Files.Count} files)");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Information about a project
    /// </summary>
    public class ProjectInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Kind { get; set; }
        public List<FileInfo> Files { get; set; } = new List<FileInfo>();
    }

    /// <summary>
    /// Information about a file
    /// </summary>
    public class FileInfo
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
        public string Language { get; set; }
    }

    /// <summary>
    /// Content of a specific file
    /// </summary>
    public class FileContentInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Content { get; set; }
        public string Language { get; set; }
        public string ErrorMessage { get; set; }
    }
}