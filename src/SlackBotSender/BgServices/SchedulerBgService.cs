using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace SlackBotSender.BgServices
{
    internal class SchedulerBgService : BackgroundService
    {
        private static string _clipboardRepo { get; set; }
        private static string _attachmentsRepo { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var oAssembly = Assembly.GetExecutingAssembly();
                var root = Path.GetDirectoryName(oAssembly.Location)!;
                _clipboardRepo = Path.Combine(root, "Clipboard");
                _attachmentsRepo = Path.Combine(root, "Attachments");

                while (stoppingToken.IsCancellationRequested || DataLayer.DbContext.Data == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!Client.IsInitialized)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    try
                    {
                        var message = await DataLayer.DbContext.Data.GetTopScheduledMessage();

                        if (message != null)
                        {
                            try
                            {
                                await SendMessage(message);
                            }
                            catch (Exception ex)
                            {
                                // TODO:
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
            }
        }


        private async Task SendMessage(DataLayer.Models.Message message)
        {
            try
            {
                #region Channels
                var channels = new List<string>();

                var msgChannels = message.Channels?.Where(p => !string.IsNullOrWhiteSpace(p.Channel?.SlackChannelID));
                if (msgChannels?.Any() ?? false)
                {
                    foreach (var channel in msgChannels)
                    {
                        if (!channels.Any(p => p.Equals(channel.Channel!.SlackChannelID, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            channels.Add(channel.Channel!.SlackChannelID);
                        }
                    }
                }

                var msgBundleChannels = message.Bundles?.Where(p => p.Bundle?.Channels?.Any() ?? false).SelectMany(p => p.Bundle!.Channels!);
                if (msgBundleChannels?.Any() ?? false)
                {
                    foreach (var channel in msgBundleChannels.Where(p => !string.IsNullOrWhiteSpace(p.SlackChannelID)).Select(p => p.SlackChannelID))
                    {
                        if (!channels.Any(p => p.Equals(channel, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            channels.Add(channel);
                        }
                    }
                }

                channels = channels.Distinct().ToList();
                var chs = channels.Any() ? channels.ToArray() : null;
                #endregion

                #region Attachments
                List<Models.Attachment>? attachments = null;

                if (message.Attachments?.Any() ?? false)
                {
                    attachments = new List<Models.Attachment>();
                    foreach (var attachment in message.Attachments)
                    {
                        var path = attachment.Path;

                        switch (attachment.Type)
                        {
                            case DataLayer.Enums.AttachmentType.Image:
                            case DataLayer.Enums.AttachmentType.File:
                                {
                                    if (string.IsNullOrWhiteSpace(attachment.Path))
                                    {
                                        continue;
                                    }

                                    path = GetAttachedFileLocalPath(attachment.Path);

                                    if (!string.IsNullOrWhiteSpace(attachment.ContentText))
                                    {
                                        File.WriteAllText(path, attachment.ContentText);
                                    }
                                    else if (attachment.ContentBin != null)
                                    {
                                        File.WriteAllBytes(path, attachment.ContentBin);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                break;
                            case DataLayer.Enums.AttachmentType.FileReference:
                            default:
                                break;
                        }

                        attachments.Add(new Models.Attachment
                        {
                            Type = (Enums.AttachmentType)attachment.Type,
                            FilePath = path,
                            Title = string.IsNullOrWhiteSpace(attachment.Title)
                                ? string.Empty
                                : attachment.Title
                        });
                    }
                }

                var att = attachments?.ToArray();
                #endregion

                var sendingDt = DateTime.Now;
                var result = await Client.SendMessage(message.Text, chs, att);

                var only = result.FirstOrDefault(p => p.Result != Enums.SendMessageResult.None);
                if (only != null)
                {
                    if (only.Result == Enums.SendMessageResult.Sent)
                    {
                        await DataLayer.DbContext.Data.UpdateMessageSetManaged(message.ID, sendingDt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (Directory.Exists(_attachmentsRepo))
                {
                    Directory.Delete(_attachmentsRepo, true);
                }
            }
        }

        private static string GetAttachedFileLocalPath(string filePath)
        {
            var clipboard = Path.GetFullPath(_clipboardRepo);
            var repo = Path.GetFullPath(_attachmentsRepo);
            filePath = Path.GetFullPath(filePath);

            if (filePath.StartsWith(clipboard, StringComparison.InvariantCultureIgnoreCase))
            {
                filePath = filePath.Replace(clipboard, repo);
                filePath = Path.GetFullPath(filePath);
            }

            if (filePath.StartsWith(_attachmentsRepo, StringComparison.InvariantCultureIgnoreCase))
            {
                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir!);
                }
                return Path.Combine(dir!, Path.GetFileName(filePath));
            }
            else
            {
                return Path.Combine(repo, Path.GetFileName(filePath));
            }
        }
    }
}
