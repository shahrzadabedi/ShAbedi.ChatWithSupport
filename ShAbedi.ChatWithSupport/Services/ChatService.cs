using Grpc.Core;
using System.Collections.Concurrent;

namespace ShAbedi.ChatWithSupport.Services;
public class ChatService : ChatWithSupport.ChatService.ChatServiceBase
{
    private static readonly ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>> UserClients = new();
    private static readonly ConcurrentDictionary<string, IServerStreamWriter<ChatMessage>> SupportClients = new();
    private static readonly ConcurrentDictionary<string, string> UserSupportPairs = new();
    private static readonly ConcurrentDictionary<string, string> SupportUserPairs = new();

    private static readonly List<string> SupportAvailableUsers = new();
    private static readonly List<string> NormalAvailableUsers = new();

    public override async Task Chat(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        string userId = context.RequestHeaders.GetValue("user-id");

        if (NormalAvailableUsers.Any(u=> u== userId))
        {
            UserClients.TryAdd(userId, responseStream);
        }
        else if (SupportAvailableUsers.Any(u=> u== userId))
        {
            SupportClients.TryAdd(userId, responseStream);
        }
        else
        {
            throw new Exception("User is not registered!");
        }

        try
        {
            await foreach (var message in requestStream.ReadAllAsync())
            {
                Console.WriteLine($"Received message from {message.UserId} to {message.RecipientId}: {message.Message}");
                await SendMessageToRecipientAsync(message);
            }
        }
        finally
        {
            UserClients.TryRemove(userId, out _);
            SupportClients.TryRemove(userId, out _);
            UserSupportPairs.TryRemove(userId, out _);
        }
    }

    private async Task SendMessageToRecipientAsync(ChatMessage message)
    {
        if (UserSupportPairs.TryGetValue(message.UserId, out var recipientId))
        {
            if (SupportClients.TryGetValue(recipientId, out var recipientStream))
            {
                await recipientStream.WriteAsync(message);
            }
        }
        else if (SupportUserPairs.TryGetValue(message.UserId, out var receipientId2))
        {
            if (UserClients.TryGetValue(receipientId2, out var userStream))
            {
                await userStream.WriteAsync(message);
            }
        }
        else
        {
            Console.WriteLine($"Recipient {message.RecipientId} not connected.");
        }
    }

    public override Task<RegisterResponse> RegisterSupportClient(SupportRequest request, ServerCallContext context)
    {
        if (SupportAvailableUsers.All(u => u != request.SupportId))
        {
            SupportAvailableUsers.Add(request.SupportId);
        }

        return Task.FromResult(new RegisterResponse { Result = true});
    }

    public override Task<RegisterResponse> RegisterNormalClient(UserRequest request, ServerCallContext context)
    {
        if (NormalAvailableUsers.All(u => u != request.UserId))
        {
            NormalAvailableUsers.Add(request.UserId);
        }

        return Task.FromResult(new RegisterResponse { Result = true });
    }

    public override Task<SupportResponse> FindSupport(UserRequest request, ServerCallContext context)
    {
        var availableSupport = SupportAvailableUsers.FirstOrDefault();
        if (availableSupport != null)
        {
            UserSupportPairs.TryAdd(request.UserId, availableSupport);
            SupportUserPairs.TryAdd(availableSupport, request.UserId);

            Console.WriteLine($"User {request.UserId} connected to Support {availableSupport}.");
            
            return Task.FromResult(new SupportResponse { SupportId = availableSupport });
        }
        else
        {
            Console.WriteLine("No support available at the moment.");
            
            return Task.FromResult(new SupportResponse { SupportId = string.Empty });
        }
    }

    public override Task<UserResponse> GetCurrentAssignedUser(SupportRequest request, ServerCallContext context)
    {
        if (SupportUserPairs.ContainsKey(request.SupportId))
        {
            SupportUserPairs.TryGetValue(request.SupportId, out var userId);
            return Task.FromResult(new UserResponse { UserId = userId });
        }
        else
        {
            return Task.FromResult(new UserResponse { UserId = string.Empty });
        }

    }

}