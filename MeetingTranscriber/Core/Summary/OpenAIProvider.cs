using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace MeetingTranscriber.Core.Summary;

public class OpenAIProvider : ISummaryProvider
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly OpenAIService? _service;

    public OpenAIProvider(string apiKey, string model = "gpt-4o-mini")
    {
        _apiKey = apiKey;
        _model = model;
        if (IsConfigured)
            _service = new OpenAIService(new OpenAiOptions { ApiKey = apiKey });
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async IAsyncEnumerable<string> SummarizeAsync(string transcript,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            yield return "OpenAI API key not set.";
            yield break;
        }

        var openAiService = _service!;

        var prompt = $"Please summarize the following meeting transcript as concise bullet points. " +
                     $"Respond in the same language as the transcript.\n\nTranscript:\n{transcript}";

        var request = new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem("You are a helpful meeting summarizer."),
                ChatMessage.FromUser(prompt)
            },
            Model = _model,
            Stream = true,
        };

        var stream = openAiService.ChatCompletion.CreateCompletionAsStream(request, cancellationToken: ct);

        await foreach (var response in stream)
        {
            ct.ThrowIfCancellationRequested();

            if (!response.Successful)
            {
                yield return $"OpenAI error: {response.Error?.Message ?? "Unknown error"}";
                yield break;
            }

            var text = response.Choices?[0]?.Delta?.Content;
            if (text is { Length: > 0 })
                yield return text;
        }
    }
}
