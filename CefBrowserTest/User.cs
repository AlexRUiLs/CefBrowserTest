using System;

namespace CefBrowserTest
{
    public class User
    {
        public User(string name, string sessionId)
        {
            this.Name = name;
            this.SessionId = sessionId;
            this.LastVote = String.Empty;
        }

        public string Name { get; private set; }

        public string SessionId { get; private set; }

        public string LastVote { get; set; }
    }
}
