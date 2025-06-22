using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaudeAI
{
    /// <summary>
    /// Handles file upload and processing operations
    /// </summary>
    public class FileUploadHandler
    {
        private readonly ChatWindowControl chatControl;
        private readonly ClaudeApiService apiService;

        public FileUploadHandler(ChatWindowControl chatControl, ClaudeApiService apiService)
        {
            this.chatControl = chatControl;
            this.apiService = apiService;
        }

        public void ShowFileDialog()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select file to analyze",
                Filter = "All Supported Files|*.cs;*.vb;*.cpp;*.c;*.h;*.hpp;*.js;*.ts;*.html;*.css;*.xml;*.json;*.sql;*.py;*.java;*.php;*.rb;*.go;*.rs;*.swift;*.txt;*.md|" +
                        "C# Files|*.cs|VB.NET Files|*.vb|C/C++ Files|*.cpp;*.c;*.h;*.hpp|JavaScript/TypeScript|*.js;*.ts|" +
                        "Web Files|*.html;*.css|Data Files|*.xml;*.json;*.sql|Python Files|*.py|Java Files|*.java|" +
                        "Text Files|*.txt;*.md|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ProcessFiles(openFileDialog.FileNames);
            }
        }

        public async void ProcessFiles(string[] filePaths)
        {
            if (apiService == null)
            {
                chatControl.AppendToChatDisplay("Please configure your API key first by clicking the settings button (⚙).\n\n");
                return;
            }

            var supportedExtensions = new[] {
                ".cs", ".vb", ".cpp", ".c", ".h", ".hpp", ".js", ".ts", ".html", ".css",
                ".xml", ".json", ".sql", ".py", ".java", ".php", ".rb", ".go", ".rs",
                ".swift", ".txt", ".md", ".yml", ".yaml", ".config", ".gitignore"
            };

            var filesToProcess = filePaths.Where(f =>
                supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()) &&
                new System.IO.FileInfo(f).Length < 1024 * 1024
            ).ToArray();

            if (filesToProcess.Length == 0)
            {
                chatControl.AppendToChatDisplay("❌ No supported files found or files are too large (max 1MB per file).\n\n");
                return;
            }

            if (filesToProcess.Length != filePaths.Length)
            {
                var skipped = filePaths.Length - filesToProcess.Length;
                chatControl.AppendToChatDisplay($"⚠️ Skipped {skipped} unsupported or large files.\n\n");
            }

            chatControl.AppendToChatDisplay($"📁 Processing {filesToProcess.Length} uploaded file(s)...\n\n");

            try
            {
                var fileContents = new List<UploadedFileInfo>();

                foreach (var filePath in filesToProcess)
                {
                    try
                    {
                        var content = File.ReadAllText(filePath);
                        var fileName = Path.GetFileName(filePath);
                        var language = GetLanguageFromExtension(Path.GetExtension(filePath));

                        fileContents.Add(new UploadedFileInfo
                        {
                            FileName = fileName,
                            FilePath = filePath,
                            Content = content,
                            Language = language,
                            Size = content.Length
                        });

                        chatControl.AppendToChatDisplay($"✅ Loaded: {fileName} ({language}, {content.Length} chars)\n");
                    }
                    catch (Exception ex)
                    {
                        chatControl.AppendToChatDisplay($"❌ Failed to read {Path.GetFileName(filePath)}: {ex.Message}\n");
                    }
                }

                if (fileContents.Count > 0)
                {
                    await AnalyzeUploadedFiles(fileContents);
                }
            }
            catch (Exception ex)
            {
                chatControl.AppendToChatDisplay($"Error processing files: {ex.Message}\n\n");
            }
        }

        private async Task AnalyzeUploadedFiles(List<UploadedFileInfo> fileContents)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Please analyze the following uploaded file(s):");
            prompt.AppendLine();

            foreach (var file in fileContents)
            {
                prompt.AppendLine($"## File: {file.FileName} ({file.Language})");
                prompt.AppendLine("```" + file.Language.ToLowerInvariant());
                prompt.AppendLine(file.Content);
                prompt.AppendLine("```");
                prompt.AppendLine();
            }

            prompt.AppendLine("Please provide a comprehensive analysis including:");
            prompt.AppendLine("1. Purpose and functionality of each file");
            prompt.AppendLine("2. Code quality and structure assessment");
            prompt.AppendLine("3. Potential improvements and optimizations");
            prompt.AppendLine("4. Best practices recommendations");
            prompt.AppendLine("5. Any issues, bugs, or security concerns");

            if (fileContents.Count > 1)
            {
                prompt.AppendLine("6. Relationships and interactions between the files");
            }

            chatControl.AppendToChatDisplay("\nClaude is analyzing your uploaded files...\n");

            try
            {
                var response = await apiService.SendMessageAsync(prompt.ToString());
                chatControl.AppendToChatDisplay($"Claude's File Analysis:\n{response}\n\n");

                // Process any code blocks in the response
                var codeBlocks = FileManager.ExtractCodeBlocks(response);
                if (codeBlocks.Count > 0)
                {
                    var results = FileManager.ProcessCodeBlocks(codeBlocks);
                    if (!string.IsNullOrEmpty(results))
                    {
                        chatControl.AppendToChatDisplay(results);
                    }
                }
            }
            catch (Exception ex)
            {
                chatControl.AppendToChatDisplay($"Error analyzing uploaded files: {ex.Message}\n\n");
            }
        }

        private string GetLanguageFromExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".cs": return "C#";
                case ".vb": return "VB.NET";
                case ".cpp": case ".cc": case ".cxx": return "C++";
                case ".c": return "C";
                case ".h": case ".hpp": return "C/C++ Header";
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
                case ".md": return "Markdown";
                case ".yml": case ".yaml": return "YAML";
                case ".txt": return "Text";
                default: return "Text";
            }
        }
    }

    /// <summary>
    /// Information about an uploaded file
    /// </summary>
    public class UploadedFileInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Content { get; set; }
        public string Language { get; set; }
        public int Size { get; set; }
    }
}