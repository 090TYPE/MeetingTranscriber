using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace MeetingTranscriber.Core.Summary;

public class ClaudeProvider : ISummaryProvider
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly AnthropicClient? _client;

    public ClaudeProvider(string apiKey, string model = "claude-sonnet-4-6")
    {
        _apiKey = apiKey;
        _model = model;
        if (IsConfigured)
            _client = new AnthropicClient(new APIAuthentication(apiKey));
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async IAsyncEnumerable<string> SummarizeAsync(string transcript,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            yield return "Claude API key not set.";
            yield break;
        }

        var client = _client!;

        var prompt = $"Please summarize the following meeting transcript as concise bullet points. " +
                     $"Respond in the same language as the transcript.\n\nTranscript:\n{transcript}";

        var parameters = new MessageParameters
        {
            Messages = new List<Message>
            {
                new Message(RoleType.User, prompt)
            },
            MaxTokens = 1024,
            Model = _model,
            Stream = true,
            Temperature = 1.0m,
        };

        var stream = client.Messages.StreamClaudeMessageAsync(parameters, ct);

        await foreach (var res in stream)
        {
            ct.ThrowIfCancellationRequested();
            if (res.Delta?.Text is { } text && text.Length > 0)
                yield return text;
        }
    }
}
