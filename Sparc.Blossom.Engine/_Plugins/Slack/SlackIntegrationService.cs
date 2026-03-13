using SlackNet;
using SlackNet.Events;
using SlackNet.WebApi;

namespace Sparc.Blossom.Plugins.Slack;

public class SlackIntegrationService
{
    private readonly IConversationsApi _conversationsApi;
    private readonly IChatApi _chatApi;

    public SlackIntegrationService(IConfiguration config)
    {
        var client = new SlackServiceBuilder()
            .UseApiToken(config.GetConnectionString("Slack"))
            .GetApiClient();
        _conversationsApi = client.Conversations;
        _chatApi = client.Chat;
    }

    public async Task<string> CreateChannel(string channelName, bool isPrivate = false)
    {
        var response = await _conversationsApi.Create(channelName, isPrivate);
        return response.Id;
    }

    public async IAsyncEnumerable<List<Conversation>> GetChannelsAsync(int limit = 100)
    {
        ConversationListResponse? response = null;
        string? cursor = null;
        int count = 0;
        do
        {
            response = await _conversationsApi.List(false, 100, cursor: cursor);
            count += response.Channels.Count;
            yield return response.Channels.ToList();
            cursor = response.ResponseMetadata.NextCursor;
        } while (!string.IsNullOrWhiteSpace(cursor) && count < limit);
    }

    public async IAsyncEnumerable<List<MessageEvent>> GetMessagesAsync(IEnumerable<string> channelIds, int limitPerChannel = 100)
    {
        foreach (var channelId in channelIds)
        {
            int count = 0;
            string? cursor = null;
            List<MessageEvent> allMessages;

            do
            {
                try
                {
                    var history = await _conversationsApi.History(channelId, limit: 100, cursor: cursor);
                    count += history.Messages.Count;
                    cursor = history.ResponseMetadata?.NextCursor;
                    allMessages = history.Messages.Where(x => string.IsNullOrWhiteSpace(x.Subtype)).ToList();
                }
                catch
                {
                    continue;
                }

                yield return allMessages;
            } while (!string.IsNullOrWhiteSpace(cursor) && count < limitPerChannel);
        }
    }

    public async Task PostMessageAsync(IEnumerable<string> channelIds, string text)
    {
        foreach (var channelId in channelIds)
        {
            await _chatApi.PostMessage(new Message { Channel = channelId, Text = text });
        }
    }
}
