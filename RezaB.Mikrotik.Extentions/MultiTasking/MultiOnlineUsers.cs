using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Mikrotik.Extentions.MultiTasking
{
    public static class MultiOnlineUsers
    {
        public static async Task<IEnumerable<UserListResults>> GetOnlineUsersList(this IEnumerable<MikrotikApiCredentials> Credentials)
        {
            var tasks = new List<Task<UserListResults>>();

            foreach (var mikrotikCredential in Credentials)
            {
                tasks.Add(Task.Run(() => (new UserListResults() { NASIP = mikrotikCredential.IP, Users = new MikrotikRouter(mikrotikCredential).GetOnlineUsers()})));
            }

            var onlineUsers = await Task.WhenAll(tasks);

            return onlineUsers;
        }
    }

    public class UserListResults
    {
        public string NASIP { get; set; }

        public List<string> Users { get; set; }
    }
}
