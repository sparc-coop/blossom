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

    public async Task<IEnumerable<Conversation>> GetChannelsAsync(int limit = 100)
    {
        var response = await _conversationsApi.List(false, limit);
        return response.Channels;
    }

    public async Task<IEnumerable<string>> GetChannelIdsAsync(int limit = 100)
    {
        var channels = await GetChannelsAsync(limit);
        return channels.Select(c => c.Id);
    }

    public async Task<IEnumerable<MessageEvent>> GetMessagesAsync(IEnumerable<string> channelIds, int limit = 100)
    {
        var allMessages = new List<MessageEvent>();
        foreach (var channelId in channelIds)
        {
            try
            {
                var history = await _conversationsApi.History(channelId);
                allMessages.AddRange(history.Messages.Cast<MessageEvent>());
            }
            catch (SlackException ex) when (ex.ErrorMessages.First() == "not_in_channel")
            {
                await _conversationsApi.Join(channelId);
                var history = await _conversationsApi.History(channelId);
                allMessages.AddRange(history.Messages.Cast<MessageEvent>());
            }
        }
        return allMessages;
    }

    public async Task PostMessageAsync(IEnumerable<string> channelIds, string text)
    {
        foreach (var channelId in channelIds)
        {
            await _chatApi.PostMessage(new Message { Channel = channelId, Text = text });
        }
    }
}
