using Grpc.Core;
using Grpc.Net.Client;
using ShAbedi.ChatWithSupport;

Console.WriteLine("Enter Support ID:");
string supportId = Console.ReadLine();

var channel = GrpcChannel.ForAddress("http://localhost:5047");
var client = new ChatService.ChatServiceClient(channel);

client.RegisterSupportClient(new SupportRequest { SupportId = supportId });

var headers = new Metadata { { "user-id", supportId } };
using var call = client.Chat(headers);

var assignedUserId = String.Empty;
while (client.GetCurrentAssignedUser(new SupportRequest { SupportId = supportId }).UserId == string.Empty);

assignedUserId = client.GetCurrentAssignedUser(new SupportRequest { SupportId = supportId }).UserId;


var readTask = Task.Run(async () =>
{
    await foreach (var message in call.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"From {message.UserId}: {message.Message}");
    }
});

Console.WriteLine("Support ready to chat.");

var recipientId = assignedUserId;
while (true)
{
    Console.Write("Support: ");
    var messageText = Console.ReadLine();

    if (recipientId != null)
    {
        var message = new ChatMessage { UserId = supportId, RecipientId = recipientId, Message = messageText };
        await call.RequestStream.WriteAsync(message);
    }
    else
    {
        Console.WriteLine("No users connected to you at the moment.");
    }
}