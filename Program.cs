﻿using System;
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
using System.Net;


namespace GrouchySpouse
{
    class Program
    {
        private static readonly string OPENAI_API_TOKEN = "gsk_XXX";
        private static readonly string AUDIO_API_TOKEN = "sk-XXXs";

        private static readonly HttpClient _openAIClient = new();

        private static string? SYSTEM_PROMPT;

        private static readonly string[] VOICES = { "alloy", "ash", "coral", "echo", "fable", "onyx", "nova", "sage", "shimmer" };

        private static string MODEL = "llama-3.3-70b-versatile";

        private static string TTS_VOICE = "sage"; // Available voices: alloy, ash, coral, echo, fable, onyx, nova, sage, shimmer

        /// <summary>
        /// Using this for the API timeout for TTS audio download. (In seconds)
        /// </summary>
        private static short _apiTimeout = 10;  // OpenAI TTS API timeout in seconds

        static async Task Main(string[] args)
        {
            Console.Clear();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-model")
                {
                    if (i + 1 < args.Length)
                    {
                        MODEL = args[i + 1];
                        i++; // Skip next argument since it's used for the model.
                    }
                    else
                    {
                        Console.WriteLine("No model specified after -model.");
                        return;
                    }
                }
                else if (args[i] == "-voice")
                {
                    if (i + 1 < args.Length)
                    {
                        if (Array.Exists(VOICES, v => v.Equals(args[i + 1], StringComparison.OrdinalIgnoreCase)))
                        {
                            TTS_VOICE = args[i + 1];
                            i++; // Skip next argument since it's used for the voice.
                        }
                        else
                        {
                            Console.WriteLine("Unknown voice specified after -voice.");
                            Console.WriteLine("Available voices: alloy, ash, coral, echo, fable, onyx, nova, sage, shimmer");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("No voice specified after -voice.");
                        Console.WriteLine("Available voices: alloy, ash, coral, echo, fable, onyx, nova, sage, shimmer");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    return;
                }
            }

            InitializeClients();
            SYSTEM_PROMPT = await File.ReadAllTextAsync("system_prompt.txt"); // read the system prompt from a flat text file

            Console.WriteLine("\nSYSTEM PROMPT:\n" + SYSTEM_PROMPT + "\n");
            await Chat();        
        }

        static void InitializeClients()
        {
            _openAIClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _openAIClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            _openAIClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", OPENAI_API_TOKEN);
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
                    continue;
                }

                var response = await _openAIClient.PostAsJsonAsync("chat/completions", new
                {
                    model = MODEL,
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
        /// <param name="text">The spoken text</param>
        /// <returns></returns>
        static async Task SynthesizeAndPlayAudio(string text)
        {
            var inputBody = new InputBody("tts-1", text, TTS_VOICE, 1.0m);

            var httpClient = new HttpClient();
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.openai.com/v1/audio/speech"),
                Headers =
                {
                    { HttpRequestHeader.Authorization.ToString(), "Bearer " + AUDIO_API_TOKEN },
                    { HttpRequestHeader.Accept.ToString(), "application/json" }
                },

                // Formats the input as JSON
                Content = JsonContent.Create(inputBody)
            };

            var audioDownloadTask = httpClient.SendAsync(httpRequestMessage);
            Console.Write("Downloading audio");

            var timeoutTask = Task.Delay(_apiTimeout * 1000);
            while (!audioDownloadTask.IsCompleted)
            {
                if (timeoutTask.IsCompleted)
                {
                    Console.WriteLine("\nTimed out while waiting for audio to download...");
                    return;
                }
                Console.Write(".");
                await Task.Delay(200);
            }

            Console.WriteLine();
            var response = await audioDownloadTask;
            var byteArray = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync("tts.mp3", byteArray);
            //Console.WriteLine("Audio downloaded successfully.");

            PlayAudioAsync("tts.mp3").Wait();
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
                try
                {
                    using var audioFile = new AudioFileReader(filePath);
                    using var outputDevice = new WaveOutEvent();
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing audio: {ex.Message}");
                    Console.WriteLine("The file format may be unsupported or there was an issue with playback.");
                }
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
        /// <summary>
        /// Input body for the request
        /// </summary>
        public class InputBody
        {
            /// <summary>
            /// LLM to use for the request
            /// </summary>
            public string Model { get; set; }
            /// <summary>
            /// Text to be converted to speech
            /// </summary>
            public string Input { get; set; }
            /// <summary>
            /// Available voices: alloy, ash, coral, echo, fable, onyx, nova, sage, shimmer
            /// </summary>
            public string Voice { get; set; }
            /// <summary>
            /// Playback speed of the audio
            /// </summary>
            public decimal Speed { get; set; }

            public InputBody(string model, string input, string voice, decimal speed)
            {
                Model = model;
                Input = input;
                Voice = voice;
                Speed = speed;
            }
        }
}
