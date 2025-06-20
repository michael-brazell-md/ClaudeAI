using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

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
                    model = "claude-3-sonnet-20240229",
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

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(API_BASE_URL, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<ClaudeResponse>(responseJson);

                    if (responseData?.Content?.Length > 0)
                    {
                        return responseData.Content[0].Text;
                    }

                    return "Sorry, I couldn't process your request.";
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

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }

    // Data models for Claude API response
    public class ClaudeResponse
    {
        [JsonProperty("content")]
        public ClaudeContent[] Content { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("usage")]
        public ClaudeUsage Usage { get; set; }
    }

    public class ClaudeContent
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class ClaudeUsage
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }
}