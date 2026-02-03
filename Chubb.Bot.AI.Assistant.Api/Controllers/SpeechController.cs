using Chubb.Bot.AI.Assistant.Application.DTOs.Requests;
using Chubb.Bot.AI.Assistant.Application.DTOs.Responses;
using Chubb.Bot.AI.Assistant.Infrastructure.HttpClients.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Chubb.Bot.AI.Assistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpeechController : ControllerBase
{
    private readonly ISpeechClient _speechClient;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(ISpeechClient speechClient, ILogger<SpeechController> logger)
    {
        _speechClient = speechClient;
        _logger = logger;
    }

    /// <summary>
    /// Convierte texto a voz (Text-to-Speech)
    /// </summary>
    /// <param name="request">Texto a convertir</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Audio en formato WAV</returns>
    [HttpPost("tts")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TextToSpeech(
        [FromBody] TextToSpeechRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Converting text to speech for session: {SessionId}", request.SessionId);

        var audioData = await _speechClient.SynthesizeSpeechAsync(request.Text, cancellationToken);

        return File(audioData, "audio/wav", $"speech_{DateTime.UtcNow.Ticks}.wav");
    }

    /// <summary>
    /// Convierte voz a texto (Speech-to-Text)
    /// </summary>
    /// <param name="request">Solicitud con audio en Base64</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Texto reconocido del audio</returns>
    [HttpPost("stt")]
    [ProducesResponseType(typeof(SpeechToTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SpeechToTextResponse>> SpeechToText(
        [FromBody] SpeechToTextRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Converting speech to text. Language: {Language}, Format: {Format}",
            request.Language,
            request.AudioFormat);

        try
        {
            // Convertir de Base64 a bytes
            var audioData = Convert.FromBase64String(request.AudioBase64);

            // Llamar al servicio de speech
            var text = await _speechClient.RecognizeSpeechAsync(audioData, cancellationToken);

            var response = new SpeechToTextResponse
            {
                Text = text,
                Success = true,
                Confidence = null, // El servicio puede proveer esto
                DurationSeconds = null, // El servicio puede proveer esto
                DetectedLanguage = request.Language
            };

            return Ok(response);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid Base64 audio data");
            return BadRequest(new SpeechToTextResponse
            {
                Text = string.Empty,
                Success = false,
                ErrorMessage = "Invalid Base64 audio format"
            });
        }
    }
}
