using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace ClaudeAI
{
    /// <summary>
    /// Service for communicating with Claude AI API
    /// </summary>
    public class ClaudeApiService
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private const string API_BASE_URL = "https://api.anthropic.com/v1/messages";

        public ClaudeApiService(string apiKey)
        {
            this.apiKey = apiKey;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            this.httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        }

        /// <summary>
        /// Send a message to Claude and get response
        /// </summary>
        /// <param name="message">User message</param>
        /// <returns>Claude's response</returns>
        public async Task<string> SendMessageAsync(string message)
        {
            try
            {
                var requestData = new
                {
                    model = "claude-3-5-sonnet-20241022",
                    max_tokens = 1000,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = message
                        }
                    }
                };

                var serializer = new JavaScriptSerializer();
                var json = serializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(API_BASE_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();

                    // Manual JSON parsing to avoid Newtonsoft.Json dependency
                    var responseText = ExtractTextFromResponse(responseJson);
                    return !string.IsNullOrEmpty(responseText) ? responseText : "Sorry, I couldn't process your request.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Error communicating with Claude: {ex.Message}";
            }
        }

        /// <summary>
        /// Extract text from Claude API response using simple string parsing
        /// </summary>
        private string ExtractTextFromResponse(string jsonResponse)
        {
            try
            {
                // Look for the text content in the response
                // This is a simple parser for the expected Claude API response format
                var serializer = new JavaScriptSerializer();
                var response = serializer.DeserializeObject(jsonResponse) as Dictionary<string, object>;

                if (response != null && response.ContainsKey("content"))
                {
                    var content = response["content"] as object[];
                    if (content != null && content.Length > 0)
                    {
                        var firstContent = content[0] as Dictionary<string, object>;
                        if (firstContent != null && firstContent.ContainsKey("text"))
                        {
                            return firstContent["text"].ToString();
                        }
                    }
                }

                return "Unable to parse response.";
            }
            catch (Exception)
            {
                return "Error parsing response.";
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }

    // Simple data models for reference (not used with JavaScriptSerializer)
    public class ClaudeResponse
    {
        public ClaudeContent[] Content { get; set; }
        public string Id { get; set; }
        public string Model { get; set; }
        public string Role { get; set; }
        public string Type { get; set; }
        public ClaudeUsage Usage { get; set; }
    }

    public class ClaudeContent
    {
        public string Text { get; set; }
        public string Type { get; set; }
    }

    public class ClaudeUsage
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}