# GrouchySpouse

A fun project that leverages multi-platform audio to produce a grouchy version of your spouse or partner. Change the system prompt by modifying system_prompt.txt. Easy! 


![th](https://github.com/user-attachments/assets/36968083-6b90-4130-9c1d-fd47780737de)


## Features

- Chat with an AI-powered assistant
- Maintains conversation history for context-aware responses
- Synthesizes and plays audio responses

## Prerequisites

- .NET SDK
- An API key Groq Cloud
- An API key for OpenAI "tts-1" model for speech systhesis


## Setup

1. Clone the repository:
    ```sh
    git clone https://github.com/FusionGuy/GrouchySpouse.git
    ```

2. Create a [system_prompt.txt](http://_vscodecontentref_/0) file in the project directory with the desired system prompt content.

3. Update the API keys: OpenAI token w/ access to the tts-1 model and Groq Cloud API token
    ```csharp
    _openAIClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", "GROQ CLOUD API TOKEN");
    _replicateClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", "OPENAI API TOKEN");
    ```

4. Build and run the project:
    ```sh
    dotnet build
    dotnet run
    ```

## Usage

1. Run the application: (also with command-line paramters --voice and --model)
    ```
    dotnet run
    dotnet run --voice sage --model llama-3.3-70b-versatile 
    ```
The application can be run with command-line parameters. 

2. The application will read the system prompt from [system_prompt.txt]) and display it.

3. Start chatting with the AI assistant by typing your messages in the console.

4. The AI assistant will respond with text and synthesized audio.

5. The program will delete the audio file once played for cleanup You can comment out the code to prevent that if you wish.

## License

This project is licensed under the MIT License. See the LICENSE file for details.
