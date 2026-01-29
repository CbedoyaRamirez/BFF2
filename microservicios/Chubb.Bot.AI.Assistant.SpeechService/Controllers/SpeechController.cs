using Microsoft.AspNetCore.Mvc;
using Chubb.Bot.AI.Assistant.SpeechService.Models;

namespace Chubb.Bot.AI.Assistant.SpeechService.Controllers;

[ApiController]
[Route("api/speech")]
public class SpeechController : ControllerBase
{
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(ILogger<SpeechController> logger)
    {
        _logger = logger;
    }

    [HttpPost("synthesize")]
    public IActionResult Synthesize([FromBody] SynthesizeRequest request)
    {
        _logger.LogInformation("Synthesizing text to speech: {Text}", request.Text);

        // Simulate text-to-speech conversion
        var response = new SynthesizeResponse
        {
            AudioId = Guid.NewGuid().ToString(),
            AudioUrl = $"https://storage.example.com/audio/{Guid.NewGuid()}.{request.Format}",
            Format = request.Format,
            DurationSeconds = request.Text.Length / 10, // Simulate duration based on text length
            FileSizeBytes = request.Text.Length * 1024, // Simulate file size
            GeneratedAt = DateTime.UtcNow,
            Base64Audio = "U2ltdWxhdGVkIGF1ZGlvIGRhdGE=" // Simulated base64 encoded audio
        };

        _logger.LogInformation("Audio synthesized successfully: {AudioId}", response.AudioId);

        return Ok(response);
    }

    [HttpPost("recognize")]
    public IActionResult Recognize([FromBody] RecognizeRequest request)
    {
        _logger.LogInformation("Recognizing speech from audio");

        // Simulate speech-to-text conversion
        var sampleTexts = new[]
        {
            "I would like to get a quote for auto insurance",
            "What types of coverage do you offer?",
            "How do I file a claim?",
            "Can you help me with my policy?",
            "I need to update my contact information"
        };

        var transcribedText = sampleTexts[Random.Shared.Next(sampleTexts.Length)];

        var response = new RecognizeResponse
        {
            TranscriptionId = Guid.NewGuid().ToString(),
            Text = transcribedText,
            Confidence = Random.Shared.NextDouble() * 0.2 + 0.8, // 0.8 to 1.0
            Language = request.Language,
            DurationSeconds = Random.Shared.Next(3, 10),
            ProcessedAt = DateTime.UtcNow,
            Words = transcribedText.Split(' ').Select((word, index) => new Word
            {
                Text = word,
                StartTime = index * 0.5,
                EndTime = (index + 1) * 0.5,
                Confidence = Random.Shared.NextDouble() * 0.2 + 0.8
            }).ToList()
        };

        _logger.LogInformation("Speech recognized successfully: {TranscriptionId}", response.TranscriptionId);

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", service = "SpeechService", timestamp = DateTime.UtcNow });
    }
}
