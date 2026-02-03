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
    /// <param name="audioFile">Archivo de audio en formato WAV</param>
    /// <param name="sessionId">ID de sesión (opcional)</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Texto reconocido del audio</returns>
    [HttpPost("stt")]
    [ProducesResponseType(typeof(SpeechToTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SpeechToTextResponse>> SpeechToText(
        IFormFile audioFile,
        [FromQuery] string? sessionId,
        CancellationToken cancellationToken)
    {
        if (audioFile == null || audioFile.Length == 0)
        {
            return BadRequest("Audio file is required");
        }

        _logger.LogInformation("Converting speech to text for session: {SessionId}", sessionId);

        byte[] audioData;
        using (var memoryStream = new MemoryStream())
        {
            await audioFile.CopyToAsync(memoryStream, cancellationToken);
            audioData = memoryStream.ToArray();
        }

        var text = await _speechClient.RecognizeSpeechAsync(audioData, cancellationToken);

        var response = new SpeechToTextResponse
        {
            Text = text,
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }
}
