using Microsoft.AspNetCore.SignalR;

namespace BlogManagement.Hubs
{
    public class BlogHub : Hub
    {
        public async Task SendUpdate(string user, string content)
        {
            await Clients.Others.SendAsync("ReceiveUpdate", user, content);
        }
        public async Task JoinGroup(string blogPostId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, blogPostId);
        }

        public async Task LeaveGroup(string blogPostId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, blogPostId);
        }
    }
}
