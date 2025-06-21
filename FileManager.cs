using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClaudeAI
{
    /// <summary>
    /// Service for creating and modifying files in the Visual Studio solution
    /// </summary>
    public static class FileManager
    {
        /// <summary>
        /// Extracts code blocks from Claude's response and processes them
        /// </summary>
        public static List<CodeBlock> ExtractCodeBlocks(string claudeResponse)
        {
            var codeBlocks = new List<CodeBlock>();

            // Pattern to match code blocks with optional language and filename
            var pattern = @"```(?:(\w+))?\s*(?:\/\/\s*(.+?))?\s*\n(.*?)\n```";
            var matches = Regex.Matches(claudeResponse, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                var language = match.Groups[1].Value;
                var filename = match.Groups[2].Value.Trim();
                var code = match.Groups[3].Value.Trim();

                // If no filename is specified, try to extract from comments
                if (string.IsNullOrEmpty(filename))
                {
                    filename = ExtractFilenameFromCode(code, language);
                }

                codeBlocks.Add(new CodeBlock
                {
                    Language = language,
                    Filename = filename,
                    Code = code,
                    IsComplete = IsCompleteFile(code, language)
                });
            }

            return codeBlocks;
        }

        /// <summary>
        /// Processes code blocks and offers to create/update files
        /// </summary>
        public static string ProcessCodeBlocks(List<CodeBlock> codeBlocks)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (codeBlocks.Count == 0)
                return "";

            var results = new List<string>();
            var validBlocks = codeBlocks.Where(cb => !string.IsNullOrEmpty(cb.Filename) && cb.IsComplete).ToList();

            if (validBlocks.Count == 0)
                return "\n📝 Code blocks detected but no complete files to create/update.\n";

            foreach (var block in validBlocks)
            {
                try
                {
                    var result = ProcessSingleCodeBlock(block);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add($"❌ Error processing {block.Filename}: {ex.Message}");
                }
            }

            return "\n" + string.Join("\n", results) + "\n";
        }

        private static string ProcessSingleCodeBlock(CodeBlock block)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
            if (dte?.Solution == null)
                return $"❌ No solution open for {block.Filename}";

            // Determine the full path for the file
            var fullPath = DetermineFilePath(block.Filename, dte);

            if (File.Exists(fullPath))
            {
                // File exists - update it
                return UpdateExistingFile(fullPath, block, dte);
            }
            else
            {
                // File doesn't exist - create it
                return CreateNewFile(fullPath, block, dte);
            }
        }

        private static string UpdateExistingFile(string fullPath, CodeBlock block, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Create backup
                var backupPath = fullPath + ".claude_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(fullPath, backupPath);

                // Update the file
                File.WriteAllText(fullPath, block.Code);

                // If the file is open in VS, reload it
                ReloadFileInVS(fullPath, dte);

                return $"✅ Updated: {block.Filename} (backup: {Path.GetFileName(backupPath)})";
            }
            catch (Exception ex)
            {
                return $"❌ Failed to update {block.Filename}: {ex.Message}";
            }
        }

        private static string CreateNewFile(string fullPath, CodeBlock block, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create the file
                File.WriteAllText(fullPath, block.Code);

                // Add to the appropriate project
                var project = FindBestProject(fullPath, dte);
                if (project != null)
                {
                    project.ProjectItems.AddFromFile(fullPath);
                    return $"✅ Created: {block.Filename} (added to {project.Name})";
                }
                else
                {
                    return $"✅ Created: {block.Filename} (not added to project - no suitable project found)";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Failed to create {block.Filename}: {ex.Message}";
            }
        }

        private static string DetermineFilePath(string filename, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If it's already a full path, use it
            if (Path.IsPathRooted(filename))
                return filename;

            // Try to find the file in the solution
            var existingFile = FindFileInSolution(filename, dte);
            if (!string.IsNullOrEmpty(existingFile))
                return existingFile;

            // Get the solution directory
            var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);

            // If filename contains path separators, use relative to solution
            if (filename.Contains("\\") || filename.Contains("/"))
            {
                return Path.Combine(solutionDir, filename.Replace("/", "\\"));
            }

            // Try to find the best project for this file type
            var project = FindBestProjectForFileType(filename, dte);
            if (project != null)
            {
                var projectDir = Path.GetDirectoryName(project.FullName);
                return Path.Combine(projectDir, filename);
            }

            // Fallback to solution directory
            return Path.Combine(solutionDir, filename);
        }

        private static string FindFileInSolution(string filename, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Project project in dte.Solution.Projects)
            {
                var found = FindFileInProject(project.ProjectItems, filename);
                if (!string.IsNullOrEmpty(found))
                    return found;
            }

            return null;
        }

        private static string FindFileInProject(ProjectItems items, string filename)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (items == null) return null;

            foreach (ProjectItem item in items)
            {
                try
                {
                    if (item.Name.Equals(filename, StringComparison.OrdinalIgnoreCase))
                    {
                        return item.FileNames[1];
                    }

                    // Recursively search subfolders
                    var found = FindFileInProject(item.ProjectItems, filename);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }
                catch (Exception)
                {
                    // Continue searching
                }
            }

            return null;
        }

        private static Project FindBestProject(string filePath, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Project bestProject = null;
            int bestScore = -1;

            foreach (Project project in dte.Solution.Projects)
            {
                try
                {
                    if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") // Solution folder
                        continue;

                    var projectDir = Path.GetDirectoryName(project.FullName);
                    if (filePath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                    {
                        var score = projectDir.Length; // Prefer more specific (deeper) projects
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestProject = project;
                        }
                    }
                }
                catch (Exception)
                {
                    // Continue searching
                }
            }

            return bestProject;
        }

        private static Project FindBestProjectForFileType(string filename, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var extension = Path.GetExtension(filename).ToLowerInvariant();

            foreach (Project project in dte.Solution.Projects)
            {
                try
                {
                    if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") // Solution folder
                        continue;

                    // Check if project already has files with this extension
                    if (ProjectContainsExtension(project.ProjectItems, extension))
                    {
                        return project;
                    }
                }
                catch (Exception)
                {
                    // Continue searching
                }
            }

            // Fallback to first non-solution-folder project
            foreach (Project project in dte.Solution.Projects)
            {
                try
                {
                    if (project.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                        return project;
                }
                catch (Exception)
                {
                    // Continue searching
                }
            }

            return null;
        }

        private static bool ProjectContainsExtension(ProjectItems items, string extension)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (items == null) return false;

            foreach (ProjectItem item in items)
            {
                try
                {
                    if (Path.GetExtension(item.Name).ToLowerInvariant() == extension)
                        return true;

                    if (ProjectContainsExtension(item.ProjectItems, extension))
                        return true;
                }
                catch (Exception)
                {
                    // Continue searching
                }
            }

            return false;
        }

        private static void ReloadFileInVS(string fullPath, DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Check if file is open
                foreach (Document doc in dte.Documents)
                {
                    if (doc.FullName.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // File is open, reload it
                        doc.Close(vsSaveChanges.vsSaveChangesNo);
                        dte.ItemOperations.OpenFile(fullPath);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                // If reload fails, continue silently
            }
        }

        private static string ExtractFilenameFromCode(string code, string language)
        {
            // Try to extract filename from common patterns in code
            var patterns = new[]
            {
                @"//\s*File:\s*(.+?)(?:\r?\n|$)",           // // File: filename.cs
                @"//\s*(.+?\.\w+)(?:\r?\n|$)",             // // filename.cs  
                @"namespace\s+\w+.*?class\s+(\w+)",        // Extract class name for C#
                @"public\s+class\s+(\w+)",                 // public class ClassName
                @"class\s+(\w+)\s*:",                      // class ClassName: (Python, etc.)
                @"function\s+(\w+)\s*\(",                  // function functionName(
                @"def\s+(\w+)\s*\("                        // def function_name(
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(code, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success)
                {
                    var filename = match.Groups[1].Value.Trim();

                    // If it's just a class/function name, add appropriate extension
                    if (!Path.HasExtension(filename))
                    {
                        filename += GetDefaultExtension(language);
                    }

                    return filename;
                }
            }

            return "";
        }

        private static string GetDefaultExtension(string language)
        {
            switch (language.ToLowerInvariant())
            {
                case "csharp":
                case "c#":
                case "cs": return ".cs";
                case "vb":
                case "vbnet": return ".vb";
                case "cpp":
                case "c++": return ".cpp";
                case "c": return ".c";
                case "javascript":
                case "js": return ".js";
                case "typescript":
                case "ts": return ".ts";
                case "python":
                case "py": return ".py";
                case "java": return ".java";
                case "html": return ".html";
                case "css": return ".css";
                case "xml": return ".xml";
                case "json": return ".json";
                case "sql": return ".sql";
                default: return ".txt";
            }
        }

        private static bool IsCompleteFile(string code, string language)
        {
            // Simple heuristics to determine if this is a complete file vs a snippet
            var lines = code.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            if (lines.Count < 3) return false; // Too short to be a complete file

            var codeString = code.ToLowerInvariant();

            switch (language.ToLowerInvariant())
            {
                case "csharp":
                case "c#":
                case "cs":
                    return codeString.Contains("namespace") ||
                           codeString.Contains("using ") ||
                           (codeString.Contains("class") && codeString.Contains("{") && codeString.Contains("}"));

                case "javascript":
                case "js":
                case "typescript":
                case "ts":
                    return codeString.Contains("function") ||
                           codeString.Contains("const ") ||
                           codeString.Contains("export") ||
                           lines.Count > 10;

                case "python":
                case "py":
                    return codeString.Contains("def ") ||
                           codeString.Contains("class ") ||
                           codeString.Contains("import ") ||
                           lines.Count > 10;

                case "html":
                    return codeString.Contains("<!doctype") ||
                           codeString.Contains("<html") ||
                           codeString.Contains("<body");

                case "css":
                    return codeString.Contains("{") && codeString.Contains("}");

                case "xml":
                case "json":
                    return true; // Usually complete

                default:
                    return lines.Count > 5; // Generic heuristic
            }
        }
    }

    /// <summary>
    /// Represents a code block extracted from Claude's response
    /// </summary>
    public class CodeBlock
    {
        public string Language { get; set; }
        public string Filename { get; set; }
        public string Code { get; set; }
        public bool IsComplete { get; set; }
    }
}