using System;
using System.Collections.Generic;
using System.Windows.Media.Converters;

namespace CefBrowserTest
{
    public class User : IEquatable<User>
    {
        public User(string name, string sessionId)
        {
            this.Name = name;
            this.Sessions = new List<string> { sessionId };
            this.LastVote = String.Empty;
        }

        public string Name { get; }

        public List<string> Sessions { get; private set; }

        public string LastVote { get; set; }

        public bool Equals(User user)
        {
            return this.Name.Equals(user.Name);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
