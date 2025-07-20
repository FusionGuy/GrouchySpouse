# GrouchySpouse

A conversational AI chatbot with a distinctive personality that combines text and audio responses. GrouchySpouse features a dark, intelligent, and blunt character persona with text-to-speech capabilities, creating an interactive experience where the AI responds both in text and with synthesized voice audio.

![th](https://github.com/user-attachments/assets/36968083-6b90-4130-9c1d-fd47780737de)

## 🎯 Overview

GrouchySpouse is a .NET console application that integrates multiple AI services:
- **Groq Cloud API** for language model inference (default: llama-3.3-70b-versatile)
- **OpenAI Text-to-Speech API** for voice synthesis
- **Multi-platform audio playback** supporting macOS, Windows, and Linux

The AI character has a dark, mysterious persona - intelligent, resourceful, sharp-witted, cold, blunt, and technologically knowledgeable. The personality is fully customizable through the `system_prompt.txt` file.

## ✨ Features

- **Interactive Chat Interface**: Console-based conversation with the AI
- **Conversation Memory**: Maintains full conversation history for context-aware responses
- **Text-to-Speech**: Converts AI responses to natural-sounding speech
- **Multi-platform Audio**: Supports audio playback on macOS (afplay), Windows (NAudio), and Linux (aplay)
- **Customizable Voice**: Choose from 9 different OpenAI voices
- **Flexible Model Selection**: Switch between different Groq Cloud language models
- **Customizable Personality**: Modify the AI's character through system prompts
- **Cross-platform Compatible**: Runs on macOS (ARM64/x64), Windows (x64), and Linux (x64)

## 🔧 Prerequisites

### Software Requirements
- **.NET 9.0 SDK** or later
- **Operating System**: macOS, Windows, or Linux

### API Keys Required
1. **Groq Cloud API Key**: For language model access
   - Sign up at [Groq Cloud](https://console.groq.com/)
   - Generate an API key from your dashboard

2. **OpenAI API Key**: For text-to-speech functionality  
   - Sign up at [OpenAI](https://platform.openai.com/)
   - Ensure access to the `tts-1` model
   - Generate an API key from your dashboard

### System Dependencies
- **macOS**: `afplay` (pre-installed)
- **Windows**: NAudio package (included)
- **Linux**: `aplay` (usually pre-installed with ALSA)

## 🚀 Installation & Setup

### 1. Clone the Repository
```bash
git clone https://github.com/FusionGuy/GrouchySpouse.git
cd GrouchySpouse
```

### 2. Configure API Keys
**⚠️ IMPORTANT SECURITY NOTE**: The current version has API keys hardcoded in `Program.cs`. For security, you should:

1. **Immediate Setup** (for testing):
   - Open `Program.cs`
   - Replace the placeholder API keys on lines 19-20:
   ```csharp
   private static readonly string OPENAI_API_TOKEN = "your-groq-api-key-here";
   private static readonly string AUDIO_API_TOKEN = "your-openai-api-key-here";
   ```

2. **Recommended Setup** (for production):
   - Use environment variables instead:
   ```bash
   export GROQ_API_KEY="your-groq-api-key"
   export OPENAI_API_KEY="your-openai-api-key"
   ```
   - Modify the code to read from environment variables

### 3. Customize the AI Personality (Optional)
Edit `system_prompt.txt` to change the AI's personality and behavior. The default prompt creates a dark, blunt character, but you can customize it to any personality you prefer.

### 4. Build the Project
```bash
dotnet build
```

### 5. Run the Application
```bash
dotnet run
```

## 📖 Usage

### Basic Usage
```bash
# Run with default settings
dotnet run

# The application will:
# 1. Display the current system prompt
# 2. Wait for your input after "You: "
# 3. Generate AI response in text
# 4. Download and play audio response
# 5. Continue the conversation loop
```

### Command-Line Arguments

GrouchySpouse supports two command-line arguments to customize the experience:

#### `-model <model_name>`
Specifies which Groq Cloud language model to use.

```bash
# Examples
dotnet run -model llama-3.3-70b-versatile
dotnet run -model mixtral-8x7b-32768
dotnet run -model gemma2-9b-it
```

**Popular Groq Models:**
- `llama-3.3-70b-versatile` (default) - Best for complex conversations
- `llama-3.1-8b-instant` - Faster responses
- `mixtral-8x7b-32768` - Good balance of speed and quality
- `gemma2-9b-it` - Efficient and capable

#### `-voice <voice_name>`
Selects the OpenAI text-to-speech voice for audio responses.

```bash
# Examples
dotnet run -voice sage
dotnet run -voice alloy
dotnet run -voice nova
```

**Available Voices:**
- `alloy` - Balanced and natural
- `ash` - Clear and articulate  
- `coral` - Warm and friendly
- `echo` - Expressive and dynamic
- `fable` - Smooth and storytelling
- `onyx` - Deep and authoritative
- `nova` - Bright and energetic
- `sage` (default) - Wise and measured
- `shimmer` - Light and ethereal

#### Combining Arguments
```bash
# Use both model and voice customization
dotnet run -model llama-3.1-8b-instant -voice nova

# Arguments can be in any order
dotnet run -voice echo -model mixtral-8x7b-32768
```

### Error Handling

The application includes comprehensive error handling:

```bash
# Invalid model argument
dotnet run -model
# Output: "No model specified after -model."

# Invalid voice argument
dotnet run -voice invalidvoice  
# Output: "Unknown voice specified after -voice."
# Output: "Available voices: alloy, ash, coral, echo, fable, onyx, nova, sage, shimmer"

# Unknown argument
dotnet run -unknown
# Output: "Unknown argument: -unknown"
```

### Interactive Chat Examples

```
You: What's the weather like?
AI: I don't have access to real-time weather data. Check your phone or look outside a window like a normal person.
[Audio plays with the response]

You: How do I code a REST API?
AI: Use a framework. ASP.NET Core for C#, Express for Node.js, Flask for Python. Pick one and stop overthinking it.
[Audio plays with the response]
```

## 🔧 Technical Details

### Architecture
- **Language**: C# (.NET 9.0)
- **Framework**: Console Application
- **Dependencies**: 
  - NAudio (2.2.1) - Windows audio playback
  - System.Windows.Extensions (6.0.0) - Windows compatibility

### Audio Pipeline
1. AI generates text response
2. Text sent to OpenAI TTS API (`tts-1` model)
3. Audio downloaded as MP3 file (`tts.mp3`)
4. Platform-specific audio playback:
   - **macOS**: Uses `afplay` system command
   - **Windows**: Uses NAudio library with WaveOutEvent
   - **Linux**: Uses `aplay` system command
5. Temporary audio file remains for potential replay

### API Integration
- **Groq Cloud**: RESTful API at `https://api.groq.com/openai/v1/`
- **OpenAI TTS**: RESTful API at `https://api.openai.com/v1/audio/speech`
- **Timeout Management**: 10-second timeout for audio download with progress indicators

### File Structure
```
GrouchySpouse/
├── Program.cs              # Main application logic
├── GrouchySpouse.csproj    # Project configuration
├── GrouchySpouse.sln       # Solution file
├── system_prompt.txt       # AI personality configuration
├── readme.md              # This documentation
├── .gitignore             # Git ignore rules
├── .vscode/               # VS Code configuration
│   └── launch.json
├── bin/                   # Compiled binaries (git-ignored)
├── obj/                   # Build artifacts (git-ignored)
└── tts.mp3               # Generated audio file (git-ignored)
```

## 🛠️ Development

### Building for Different Platforms
```bash
# Build for current platform
dotnet build

# Build for specific platforms
dotnet publish -r osx-arm64 -c Release
dotnet publish -r osx-x64 -c Release  
dotnet publish -r win-x64 -c Release
dotnet publish -r linux-x64 -c Release
```

### Debugging
The project includes VS Code launch configuration for debugging:
```bash
# Open in VS Code
code .

# Use F5 to start debugging
# Or use the Debug panel with ".NET Core Attach" configuration
```

## 🔒 Security Considerations

⚠️ **API Key Security**: The current implementation has API keys hardcoded in source code. For production use:

1. **Use Environment Variables**:
   ```csharp
   private static readonly string OPENAI_API_TOKEN = 
       Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? 
       throw new InvalidOperationException("GROQ_API_KEY environment variable not set");
   ```

2. **Use Configuration Files**:
   - Add `appsettings.json` with API keys
   - Add to `.gitignore` to prevent committing secrets

3. **Use Secret Management**:
   - Azure Key Vault
   - AWS Secrets Manager  
   - HashiCorp Vault

## 🐛 Troubleshooting

### Common Issues

**"No response from the model..."**
- Check your Groq API key
- Verify internet connection
- Confirm model name is correct

**Audio not playing**
- **macOS**: Ensure `afplay` is available (should be pre-installed)
- **Windows**: Check NAudio dependencies
- **Linux**: Install ALSA tools: `sudo apt-get install alsa-utils`

**"Timed out while waiting for audio to download..."**
- Check OpenAI API key and billing status
- Verify internet connection stability
- Consider increasing timeout in `_apiTimeout` variable

**Build errors**
- Ensure .NET 9.0 SDK is installed: `dotnet --version`
- Restore packages: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`

## 🤝 Contributing

Contributions are welcome! Areas for improvement:
- Environment variable configuration
- Additional TTS providers
- Voice activity detection
- Conversation export/import
- Web interface option
- Docker containerization

## 📄 License

This project is licensed under the MIT License. See the LICENSE file for details.

## 🙏 Acknowledgments

- **Groq Cloud** for fast language model inference
- **OpenAI** for high-quality text-to-speech
- **NAudio** for Windows audio capabilities
- The open-source community for inspiration and tools
