using Grpc.Core;
using Grpc.Net.Client;
using ShAbedi.ChatWithSupport;

Console.WriteLine("Enter User ID:");
string userId = Console.ReadLine();

var channel = GrpcChannel.ForAddress("http://localhost:5047");
var client = new ChatService.ChatServiceClient(channel);

client.RegisterNormalClient(new UserRequest() { UserId = userId });

var supportResponse = await client.FindSupportAsync(new UserRequest { UserId = userId });

if (string.IsNullOrEmpty(supportResponse.SupportId))
{
    Console.WriteLine("No support available at the moment. Please try again later.");
    return;
}

var headers = new Metadata { { "user-id", userId } };
using var call = client.Chat(headers);

var readTask = Task.Run(async () =>
{
    await foreach (var message in call.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"From {message.UserId}: {message.Message}");
    }
});

Console.WriteLine("Connected to support.");

while (true)
{
    Console.Write("User: ");
    var messageText = Console.ReadLine();

    var message = new ChatMessage { UserId = userId, RecipientId = supportResponse.SupportId, Message = messageText };
    await call.RequestStream.WriteAsync(message);
}
