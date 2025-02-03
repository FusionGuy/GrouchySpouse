﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NAudio.Wave;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace GrouchySpouse
{
    class Program
    {
        private static readonly HttpClient _openAIClient = new();
        private static readonly HttpClient _replicateClient = new();

        private static string? SYSTEM_PROMPT;

        /// <summary>
        /// Using this for the API timeout for Replicate API. If it doesn't response within X seconds, only the LLM response will be used - no audio.
        /// </summary>
        private static short _apiTimeout = 8;  // API timeout in seconds

        static async Task Main(string[] args)
        {
            Console.Clear();
            InitializeClients();
            SYSTEM_PROMPT = await File.ReadAllTextAsync("system_prompt.txt"); // read the system prompt from a flat text file

            Console.WriteLine("\nSYSTEM PROMPT:\n" + SYSTEM_PROMPT + "\n");
            await Chat();        
        }

        static void InitializeClients()
        {
            // DeepSeek or other "Open" AI API
            //_openAIClient.BaseAddress = new Uri("https://api.deepseek.com/");

            _openAIClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            //_openAIClient.DefaultRequestHeaders.Authorization = 
            //    new AuthenticationHeaderValue("Bearer", "sk-XXXS");

            _openAIClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", "gsk_XXX");
            
            // Replicate API
            _replicateClient.DefaultRequestHeaders.Authorization = 
               new AuthenticationHeaderValue("Bearer", "r8_XXX");
        }

        /// <summary>
        /// Chat loop
        /// </summary>
        /// <returns></returns>
        static async Task Chat()
        {
            // This keeps the context for the LLM
            var history = new List<ChatMessage>();
            if (SYSTEM_PROMPT != null)
            {
                history.Add(new ChatMessage { Role = "system", Content = SYSTEM_PROMPT });
            }

            // This keeps the chat going and feeds back the history to maintain context for the LLM
            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();
                if (!string.IsNullOrEmpty(userInput))
                {
                    history.Add(new ChatMessage { Role = "user", Content = userInput });
                }
                else
                {
                    Console.WriteLine("If you want her to talk, you have to give me something to say!");
                }

                //var response = await _openAIClient.PostAsJsonAsync("chat/completions", new
                //{
                //    model = "deepseek-chat",
                //    messages = history,
                //    stream = false
                //});


                var response = await _openAIClient.PostAsJsonAsync("chat/completions", new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = history,
                    stream = false
                });

                var completion = await response.Content.ReadFromJsonAsync<OpenAIResponse>();

                // check for a null completion...
                var answer = completion?.Choices?[0]?.Message?.Content ?? "No response from the model...";
                
                Console.WriteLine(answer);
                history.Add(new ChatMessage { Role = "assistant", Content = answer });
                
                await SynthesizeAndPlayAudio(answer);
            }
        }

        /// <summary>
        /// The talking part of the bot
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        static async Task SynthesizeAndPlayAudio(string text)
        {
            try
            {
                var prediction = await CreatePrediction(text);
                var outputUrl = await WaitForPredictionCompletion(prediction.Id);
    
                if (String.IsNullOrEmpty(outputUrl))
                {
                    Console.WriteLine("No audio will be played.");
                    return;
                }
                else
                {
                    var tempFile = Path.GetTempFileName();
    
    #if DEBUG
                    Console.WriteLine($"\nWriting audio file \"{outputUrl}\" to \"{tempFile}\"");  // Debug-only logging
    #endif
                    await DownloadAudioFile(outputUrl, tempFile);
                    await PlayAudioAsync(tempFile);
    #if DEBUG
                    Console.WriteLine("Deleting audio file \"{0}\"\n", tempFile);
    #endif
                    File.Delete(tempFile);  // Clean up the temp file after playing
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating audio: {ex.Message}");
            }
        }

    /// <summary>
    /// Obtains a prediction from the Replicate API
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
        static async Task<ReplicateCreateResponse> CreatePrediction(string text)
        {
            var response = await _replicateClient.PostAsJsonAsync("https://api.replicate.com/v1/predictions", new
            {
                version = "dfdf537ba482b029e0a761699e6f55e9162cfd159270bfe0e44857caa5f275a6",
                input = new
                {
                    text = text,
                    language = "en",
                    temperature = 0.5,
                    length = 1.0,
                    speed = 1.1,
                    voice = "af_bella" // af_bella, af_zoe, af_lisa, af_mia, af_samantha, af_olivia, af_isabella
                }
            });

            var result = await response.Content.ReadFromJsonAsync<ReplicateCreateResponse>() ?? throw new InvalidOperationException("Failed to deserialize the response.");
            return result;
        }

        /// <summary>
        /// Waits for the prediction to complete
        /// </summary>
        /// <param name="predictionId"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <summary>
        /// Waits for prediction completion with timeout handling
        /// </summary>
        static async Task<string> WaitForPredictionCompletion(string predictionId)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_apiTimeout));
            var statusResponse = new ReplicateStatusResponse { Status = "starting", Output = string.Empty };
            var apiUrl = $"https://api.replicate.com/v1/predictions/{predictionId}";
            
            try
            {
                // Poll the API until the prediction is complete or the _apiTimeout value is reached
                while (statusResponse.IsProcessing() && !cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Polling Replicate API status...");
                    await Task.Delay(1000, cts.Token);
                    
                    var response = await _replicateClient.GetAsync(apiUrl, cts.Token);
                    response.EnsureSuccessStatusCode();
                    
                    statusResponse = await response.Content.ReadFromJsonAsync<ReplicateStatusResponse>()
                        ?? throw new InvalidOperationException("Invalid API response format");
                }
            }
            catch (TaskCanceledException) when (cts.IsCancellationRequested)
            {
                Console.WriteLine("API request timed out after {0} seconds", _apiTimeout);
                return ""; // return an empty string to the caller so it knows no audio is to be played
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error: {ex.StatusCode}");
                throw;
            }

            return statusResponse.SucceededWithOutput();
        }

        /// <summary>
        /// Downloads the audio stream to a file
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        static async Task DownloadAudioFile(string url, string filePath)
        {
            using var httpClient = new HttpClient();
            var audioBytes = await httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(filePath, audioBytes);
        }

        /// <summary>
        /// Give her a voice. Multi-platform audio playback.
        /// </summary>
        /// <param name="filePath"></param>
        private static async Task PlayAudioAsync(string filePath)
        {
            // Different OS's handle audio differently. 
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // MacOS
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "afplay";
                process.StartInfo.Arguments = filePath;
                process.Start();
                await process.WaitForExitAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows
                var player = new System.Media.SoundPlayer(filePath);
                player.PlaySync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "aplay";
                process.StartInfo.Arguments = filePath;
                process.Start();
                await process.WaitForExitAsync();
            }
            else
            {
                throw new NotSupportedException("Your OS is not supported for audio playback.");
            }
        }
    }

    public class ChatMessage
    {
        public required string Role { get; set; }
        public required string Content { get; set; }
    }

    public class OpenAIResponse
    {
        public required List<Choice> Choices { get; set; }

        public class Choice
        {
            public required Message Message { get; set; }
        }

        public class Message
        {
            public required string Content { get; set; }
        }
    }

    public class ReplicateCreateResponse
    {
        [JsonPropertyName("id")]
        [JsonRequired]
        public required string Id { get; set; }
    }

    public class ReplicateStatusResponse
    {
        public required string Status { get; set; }
        public required string Output { get; set; }

        public bool IsProcessing() => Status == "starting" || Status == "processing";
        
        /// <summary>
        /// Helper function to indicate process status. Useful for API timeout handling.
        /// </summary>
        /// <returns></returns>
        public string SucceededWithOutput() =>
            Status == "succeeded" && !string.IsNullOrEmpty(Output) ? Output : string.Empty;
    }
}