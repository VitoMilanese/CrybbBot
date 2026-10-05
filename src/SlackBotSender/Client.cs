using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlackBotSender.BgServices;
using SlackBotSender.Enums;
using SlackBotSender.Exceptions;
using SlackBotSender.Slack;

namespace SlackBotSender;

public static class Client
{
    private static SlackClient? _slack { get; set; }

    public static bool IsInitialized => _slack?.IsInitialized ?? false;

    static Client()
    {
        IHostBuilder builder = Host.CreateDefaultBuilder()
            .UseWindowsService(options =>
            {
                options.ServiceName = "AFCS.TOM.APP";
            })
            .ConfigureServices((context, services) =>
            {
                //LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
                services.AddHostedService<SchedulerBgService>();
            });
        var host = builder.Build();
        _ = Task.Run(host.Run);
    }

    static string Require(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing configuration value: {name}");
        return value;
    }

    public static async Task Init(string token)
    {
        if (_slack != null)
        {
            throw new ClientAlreadyInitializedException();
        }

        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var appSettingsPath = Path.Combine(root, "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            var defaultAppSettingsPath = Path.Combine(root, "default_appsettings.json");
            if (File.Exists(defaultAppSettingsPath))
            {
                File.Copy(defaultAppSettingsPath, appSettingsPath);
            }
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        _slack = new SlackClient(token);

        try
        {
            await _slack.AuthTest();
        }
        catch
        {
            Finalize();
            throw;
        }
    }

    public static void Finalize()
    {
        if (_slack == null)
        {
            throw new ClientNotInitializedException();
        }

        _slack?.Dispose();
        _slack = null;
    }

    public static async Task<List<Models.SendMessageResult>> SendMessage(string message, string[]? channels, Models.Attachment[]? attachments = null)
    {
        if (_slack == null)
        {
            throw new ClientNotInitializedException();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new MessageNotSpecifiedException();
        }

        if (channels == null || channels.Length == 0)
        {
            throw new ChannelNotSpecifiedException();
        }

        var result = new List<Models.SendMessageResult>();

        if (attachments?.Any() ?? false)
        {
            foreach (var attachment in attachments.Where(p => p.Type != AttachmentType.FileReference))
            {
                try
                {
                    if (attachment.Type == AttachmentType.Image)
                    {
                        var fileId = await _slack.UploadImagePrivateAsync(
                            filePath: attachment.FilePath,
                            title: attachment.Title,
                            initialComment: string.Empty);
                        attachment.FieldId = fileId;
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else if (attachment.Type == AttachmentType.FileReference)
                    {
                        // Does not require uploading
                        // Is going to be attached to payload blocks
                    }
                    else
                    {
                        if (attachment.FileIds == null)
                        {
                            attachment.FileIds = new Dictionary<string, string>();
                        }

                        foreach (var channel in channels)
                        {
                            if (attachment.FileIds.TryAdd(channel, string.Empty))
                            {
                                var fileId = await _slack.UploadFileAsync(
                                    channelId: channel,
                                    filePath: attachment.FilePath,
                                    title: attachment.Title,
                                    initialComment: string.Empty);
                                attachment.FileIds[channel] = fileId;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    foreach (var channel in channels)
                    {
                        result.Add(new Models.SendMessageResult
                        {
                            ChannelId = channel,
                            Result = SendMessageResult.Failed,
                            ErrorType = SendMessageErrorType.UploadingAttachment,
                            Error = ex.Message
                        });
                    }
                    return result;
                }
            }

            foreach (var channel in channels)
            {
                var msgResult = new Models.SendMessageResult();
                try
                {
                    msgResult.ChannelId = channel;
                    await _slack.PostMessageWithPrivateImageAsync(
                        channel,
                        message,
                        attachments);
                    msgResult.Result = SendMessageResult.Sent;
                    //await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
                catch (Exception ex)
                {
                    msgResult.Result = SendMessageResult.Failed;
                    msgResult.ErrorType = SendMessageErrorType.SendingMessage;
                    msgResult.Error = ex.Message;
                }
                finally
                {
                    result.Add(msgResult);
                }
            }
        }
        else
        {
            foreach (var channel in channels)
            {
                var msgResult = new Models.SendMessageResult();
                try
                {
                    msgResult.ChannelId = channel;
                    await _slack.PostMessageAsync(channel, message);
                    msgResult.Result = SendMessageResult.Sent;
                    //await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
                catch (Exception ex)
                {
                    msgResult.Result = SendMessageResult.Failed;
                    msgResult.ErrorType = SendMessageErrorType.SendingMessage;
                    msgResult.Error = ex.Message;
                }
                finally
                {
                    result.Add(msgResult);
                }
            }
        }
        
        return result;
    }

    public static async Task<List<Models.SendMessageResult>> SendMessage(object? message, string[]? channels, Models.Attachment[]? attachments = null)
    {
        if (_slack == null)
        {
            throw new ClientNotInitializedException();
        }

        if (message == null)
        {
            throw new MessageNotSpecifiedException();
        }

        if (channels == null || channels.Length == 0)
        {
            throw new ChannelNotSpecifiedException();
        }

        var result = new List<Models.SendMessageResult>();

        if (attachments?.Any() ?? false)
        {
            foreach (var attachment in attachments.Where(p => p.Type != AttachmentType.FileReference))
            {
                try
                {
                    if (attachment.Type == AttachmentType.Image)
                    {
                        var fileId = await _slack.UploadImagePrivateAsync(
                            filePath: attachment.FilePath,
                            title: attachment.Title,
                            initialComment: string.Empty);
                        attachment.FieldId = fileId;
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else if (attachment.Type == AttachmentType.FileReference)
                    {
                        // Does not require uploading
                        // Is going to be attached to payload blocks
                    }
                    else
                    {
                        if (attachment.FileIds == null)
                        {
                            attachment.FileIds = new Dictionary<string, string>();
                        }

                        foreach (var channel in channels)
                        {
                            if (attachment.FileIds.TryAdd(channel, string.Empty))
                            {
                                var fileId = await _slack.UploadFileAsync(
                                    channelId: channel,
                                    filePath: attachment.FilePath,
                                    title: attachment.Title,
                                    initialComment: string.Empty);
                                attachment.FileIds[channel] = fileId;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    foreach (var channel in channels)
                    {
                        result.Add(new Models.SendMessageResult
                        {
                            ChannelId = channel,
                            Result = SendMessageResult.Failed,
                            ErrorType = SendMessageErrorType.UploadingAttachment,
                            Error = ex.Message
                        });
                    }
                    return result;
                }
            }

            foreach (var channel in channels)
            {
                var msgResult = new Models.SendMessageResult();
                try
                {
                    msgResult.ChannelId = channel;
                    await _slack.PostMessageWithPrivateImageAsync(
                        message,
                        channel,
                        attachments);
                    msgResult.Result = SendMessageResult.Sent;
                    //await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
                catch (Exception ex)
                {
                    msgResult.Result = SendMessageResult.Failed;
                    msgResult.ErrorType = SendMessageErrorType.SendingMessage;
                    msgResult.Error = ex.Message;
                }
                finally
                {
                    result.Add(msgResult);
                }
            }
        }
        else
        {
            foreach (var channel in channels)
            {
                var msgResult = new Models.SendMessageResult();
                try
                {
                    msgResult.ChannelId = channel;
                    await _slack.PostMessageAsync(channel, message);
                    msgResult.Result = SendMessageResult.Sent;
                    //await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
                catch (Exception ex)
                {
                    msgResult.Result = SendMessageResult.Failed;
                    msgResult.ErrorType = SendMessageErrorType.SendingMessage;
                    msgResult.Error = ex.Message;
                }
                finally
                {
                    result.Add(msgResult);
                }
            }
        }
        
        return result;
    }

    internal static async Task SendMessage(string text, string[]? chs, object att)
    {
        throw new NotImplementedException();
    }
}