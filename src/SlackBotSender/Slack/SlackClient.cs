using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SlackBotSender.Enums;
using SlackBotSender.Exceptions;
using SlackBotSender.Models;

namespace SlackBotSender.Slack;

internal sealed class SlackClient : IDisposable
{
    private HttpClient? _http { get; set; }
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public bool IsInitialized => _http != null;

    public SlackClient(string botToken, HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
    }

    public void Dispose()
    {
        _http?.Dispose();
        _http = null;
    }

    // -------------------------
    // 1) Existing: post text
    // -------------------------
    public async Task PostMessageAsync(string channelId, string text, CancellationToken ct = default)
    {
        var payload = new { channel = channelId, text };
        await PostSlackJsonAsync("chat.postMessage", payload, ct);
    }

    public async Task PostMessageAsync(string channelId, object? message, CancellationToken ct = default)
    {
        if (message == null)
        {
            throw new MessageNotSpecifiedException();
        }

        var payload = new
        {
            channel = channelId,
            text = string.Empty,
            blocks = new List<object> { message! }
        };

        await PostSlackJsonAsync("chat.postMessage", payload, ct);
    }

    // -----------------------------------------
    // Overload A: post message with Blocks JSON
    // -----------------------------------------
    // blocksJson must be a valid JSON array string, e.g. "[{...},{...}]"
    public async Task PostMessageAsync(string channelId, string text, string blocksJson, CancellationToken ct = default)
    {
        using var blocksDoc = JsonDocument.Parse(blocksJson);

        var payload = new
        {
            channel = channelId,
            text,                 // fallback text (recommended)
            blocks = blocksDoc.RootElement
        };

        await PostSlackJsonAsync("chat.postMessage", payload, ct);
    }

    // -------------------------------------------------
    // Overload B: schedule message (chat.scheduleMessage)
    // -------------------------------------------------
    public async Task<string> PostMessageAsync(
        string channelId,
        string text,
        DateTimeOffset? postAt,
        CancellationToken ct = default)
    {
        // Slack expects unix timestamp in seconds
        var unixSeconds = postAt?.ToUnixTimeSeconds();

        var payload = new
        {
            channel = channelId,
            text,
            post_at = unixSeconds
        };

        var doc = await PostSlackJsonAsync("chat.scheduleMessage", payload, ct);
        // scheduled_message_id is useful if you want to list/cancel later
        return doc.RootElement.TryGetProperty("scheduled_message_id", out var idEl)
            ? idEl.GetString() ?? ""
            : "";
    }

    public async Task PostMessageWithPrivateImageAsync(
        string channelId,
        string text,
        string slackFileId,
        string altText = "image",
        CancellationToken ct = default)
    {
        var payload = new
        {
            channel = channelId,
            text, // fallback text
            blocks = new object[]
            {
                new {
                        type = "section",
                        text = new { type = "mrkdwn", text }
                    },
                new {
                        type = "image",
                        slack_file = new { id = slackFileId },
                        alt_text = altText
                    }
            }
        };

        await PostSlackJsonAsync("chat.postMessage", payload, ct);
    }

    public async Task PostMessageWithPrivateImageAsync(
        string channelId,
        string text,
        Attachment[] slackFileds,
        CancellationToken ct = default)
    {
        var blocks = new List<object>
        {
            new {
                    type = "section",
                    text = new { type = "mrkdwn", text }
                }
        };

        List<string>? attachments = slackFileds.Any(p => p.Type != AttachmentType.Image)
            ? new List<string>()
            : null;

        foreach (var slackFiled in slackFileds)
        {
            if (slackFiled.Type == AttachmentType.Image)
            {
                blocks.Add(new
                {
                    type = "image",
                    slack_file = new { id = slackFiled.FieldId },
                    alt_text = slackFiled.Title
                });
            }
            else if (slackFiled.Type == AttachmentType.FileReference)
            {
                var reference = "<" + slackFiled.FilePath;
                if (!string.IsNullOrWhiteSpace(slackFiled.Title))
                {
                    reference += "|" + slackFiled.Title;
                }
                reference += ">";
                blocks.Add(new
                {
                    type = "section",
                    text = new { type = "mrkdwn", text = reference }
                });
            }
            else if (slackFiled.FileIds?.Any() ?? false)
            {
                foreach (var channel in slackFiled.FileIds.Keys)
                {
                    attachments!.Add(slackFiled.FileIds[channel]);
                }
            }
        }

        var payload = new
        {
            channel = channelId,
            text, // fallback text
            blocks = blocks.ToArray(),
            attachments = attachments == null
                ? null
                : new
                {
                    type = "files",
                    files = attachments.ToArray()
                }
        };

        var retry = 5;
        while (retry > 0)
        {
            try
            {
                await PostSlackJsonAsync("chat.postMessage", payload, ct);
                retry = 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("invalid_blocks", StringComparison.InvariantCultureIgnoreCase))
                {
                    --retry;
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                else
                {
                    retry = 0;
                    throw;
                }
            }
        }
    }

    public async Task PostMessageWithPrivateImageAsync(
        object? message,
        string channelId,
        Attachment[] slackFileds,
        CancellationToken ct = default)
    {
        if (message == null)
        {
            throw new MessageNotSpecifiedException();
        }

        var blocks = new List<object>
        {
            message
        };

        List<string>? attachments = slackFileds.Any(p => p.Type != AttachmentType.Image)
            ? new List<string>()
            : null;

        foreach (var slackFiled in slackFileds)
        {
            if (slackFiled.Type == AttachmentType.Image)
            {
                blocks.Add(new
                {
                    type = "image",
                    slack_file = new { id = slackFiled.FieldId },
                    alt_text = slackFiled.Title
                });
            }
            else if (slackFiled.Type == AttachmentType.FileReference)
            {
                var reference = "<" + slackFiled.FilePath;
                if (!string.IsNullOrWhiteSpace(slackFiled.Title))
                {
                    reference += "|" + slackFiled.Title;
                }
                reference += ">";
                blocks.Add(new
                {
                    type = "section",
                    text = new { type = "mrkdwn", text = reference }
                });
            }
            else if (slackFiled.FileIds?.Any() ?? false)
            {
                foreach (var channel in slackFiled.FileIds.Keys)
                {
                    attachments!.Add(slackFiled.FileIds[channel]);
                }
            }
        }

        var payload = new
        {
            channel = channelId,
            //text, // fallback text
            text = string.Empty,
            blocks = blocks.ToArray(),
            attachments = attachments == null
                ? null
                : new
                {
                    type = "files",
                    files = attachments.ToArray()
                }
        };

        var retry = 5;
        while (retry > 0)
        {
            try
            {
                await PostSlackJsonAsync("chat.postMessage", payload, ct);
                retry = 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("invalid_blocks", StringComparison.InvariantCultureIgnoreCase))
                {
                    --retry;
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                else
                {
                    retry = 0;
                    throw;
                }
            }
        }
    }

    public async Task PostMessageWithPrivateImageAsync(
        string channelId,
        string text,
        List<ImageAttachment> images,
        CancellationToken ct = default)
    {
        var payload = new
        {
            channel = channelId,
            text, // fallback text
            blocks = new object[images.Count + 1]
        };
        payload.blocks[0] = new
        {
            type = "section",
            text = new { type = "mrkdwn", text }
        };
        for (var i = 0; i < images.Count; ++i)
        {
            payload.blocks[i + 1] = new
            {
                type = "image",
                slack_file = new { id = images[i].FileId },
                alt_text = images[i].AlternativeText
            };
        }

        await PostSlackJsonAsync("chat.postMessage", payload, ct);
    }


    // -------------------------------------------------------------------
    // Overload C: upload a file (media) then share it to a channel
    // New flow: files.getUploadURLExternal -> POST bytes -> completeUploadExternal
    // -------------------------------------------------------------------
    public async Task<string> UploadFileAsync(
        string channelId,
        string filePath,
        string? title = null,
        string? initialComment = null,
        CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return await UploadFileAsync(channelId, bytes, fileName, title, initialComment, ct);
    }

    public async Task<string> UploadImagePrivateAsync(
        string filePath,
        string? title = null,
        string? initialComment = null,
        CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, ct);
        return await UploadImagePrivateAsync(bytes, fileName, title, ct);
    }

    public async Task<string> UploadFileAsync(
        string channelId,
        byte[] fileBytes,
        string fileName,
        string? title = null,
        string? initialComment = null,
        CancellationToken ct = default)
    {
        // Step 1) Ask Slack for an upload URL + file_id
        // Required: filename + length :contentReference[oaicite:5]{index=5}
        var getUrlResp = await PostSlackFormAsync(
            "files.getUploadURLExternal",
            new Dictionary<string, string>
            {
                ["filename"] = fileName,
                ["length"] = fileBytes.Length.ToString()
            },
            ct);

        var uploadUrl = getUrlResp.RootElement.GetProperty("upload_url").GetString()
            ?? throw new InvalidOperationException("Slack did not return upload_url.");

        var fileId = getUrlResp.RootElement.GetProperty("file_id").GetString()
            ?? throw new InvalidOperationException("Slack did not return file_id.");

        // Step 2) Upload bytes to the returned URL (no Slack token header required here)
        using (var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl))
        {
            uploadReq.Content = new ByteArrayContent(fileBytes);
            uploadReq.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var uploadResp = await _http.SendAsync(uploadReq, ct);
            uploadResp.EnsureSuccessStatusCode();
        }

        // Step 3) Finalize + share the file in a channel
        // If channel_id not provided, file remains private :contentReference[oaicite:6]{index=6}
        var files = new[]
        {
            new Dictionary<string, string> { ["id"] = fileId, ["title"] = title ?? fileName }
        };

        var completePayload = new Dictionary<string, object?>
        {
            ["files"] = files,
            ["channel_id"] = channelId,
            ["initial_comment"] = initialComment
        };

        await PostSlackJsonAsync("files.completeUploadExternal", completePayload, ct);

        return fileId;
    }

    public async Task<string> UploadImagePrivateAsync(
        byte[] fileBytes,
        string fileName,
        string? title = null,
        CancellationToken ct = default)
    {
        // 1) get upload URL + file_id
        var getUrlResp = await PostSlackFormAsync(
            "files.getUploadURLExternal",
            new Dictionary<string, string>
            {
                ["filename"] = fileName,
                ["length"] = fileBytes.Length.ToString()
            },
            ct);

        var uploadUrl = getUrlResp.RootElement.GetProperty("upload_url").GetString()
            ?? throw new InvalidOperationException("Slack did not return upload_url.");

        var fileId = getUrlResp.RootElement.GetProperty("file_id").GetString()
            ?? throw new InvalidOperationException("Slack did not return file_id.");

        // 2) upload bytes to uploadUrl
        using (var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl))
        {
            uploadReq.Content = new ByteArrayContent(fileBytes);
            uploadReq.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            using var uploadResp = await _http.SendAsync(uploadReq, ct);
            uploadResp.EnsureSuccessStatusCode();
        }

        // 3) complete upload WITHOUT channel_id => private file
        var files = new[]
        {
            new Dictionary<string, string> { ["id"] = fileId, ["title"] = title ?? fileName }
        };

        var completePayload = new Dictionary<string, object?>
        {
            ["files"] = files
            // intentionally NO channel_id here -> private
        };

        await PostSlackJsonAsync("files.completeUploadExternal", completePayload, ct);

        return fileId;
    }

    public async Task<bool> AuthTest(CancellationToken ct = default)
    {
        var result = await PostSlackJsonAsync("auth.test", null, ct);
        return true;
    }


    // -------------------------
    // Helpers
    // -------------------------
    private async Task<JsonDocument> PostSlackJsonAsync(string method, object payload, CancellationToken ct)
    {
        using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync($"https://slack.com/api/{method}", content, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("ok", out var okEl) || !okEl.GetBoolean())
        {
            var error = doc.RootElement.TryGetProperty("error", out var errEl) ? errEl.GetString() : "unknown_error";

            if (error?.ToString().Equals("invalid_auth", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                throw new InvalidOperationException("Invalid authentication");
            }

            if (error?.ToString().Equals("channel_not_found", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                var str = payload?.ToString()?.Split(new[] { ' ', '=', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var channel = string.Empty;
                if (str != null && str.Length > 1)
                {
                    channel = str[2];
                }
                throw new Exceptions.ChannelNotFoundException(channel);
            }

            throw new InvalidOperationException($"Slack API error ({method}): {error}. Response: {body}");
        }

        return doc;
    }

    public class PayloadWithChannel
    {
        public string? Channel { get; set; }
    }

    private async Task<JsonDocument> PostSlackFormAsync(string method, Dictionary<string, string> formFields, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(formFields);
        using var resp = await _http.PostAsync($"https://slack.com/api/{method}", content, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("ok", out var okEl) || !okEl.GetBoolean())
        {
            var error = doc.RootElement.TryGetProperty("error", out var errEl) ? errEl.GetString() : "unknown_error";
            throw new InvalidOperationException($"Slack API error ({method}): {error}. Response: {body}");
        }

        return doc;
    }
}
