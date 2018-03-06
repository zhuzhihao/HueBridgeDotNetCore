using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace HueBridge.Utilities
{
    public class Authenticator
    {
        private List<string> _cachedUsers = new List<string>();
        private LiteDatabase _db;

        public Authenticator(LiteDatabase db)
        {
            _db = db;
        }

        public bool IsValidUser(string username)
        {
            // check cache
            if (!_cachedUsers.Contains(username))
            {
                // check db
                var users = _db.GetCollection<Models.User>("users");
                var user = users.FindOne(x => x.Id.Equals(username));
                if (user == null)
                {
                    return false;
                }
                // add to cache
                _cachedUsers.Add(username);
                // update last used date
                user.LastUsedDate = DateTime.Now;
                users.Update(user);
            }

            return true;
        }

        public object ErrorResponse(string path)
        {
            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["error"] = new
                    {
                        type = 1,
                        description = "unauthorized user",
                        address = path
                    }
                }
            };
        }

        public void RemoveUserFromCache(string id)
        {
            _cachedUsers.RemoveAll(x => (x == id));
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RequireAuthenticationAttribute : Attribute
    {
        public RequireAuthenticationAttribute()
        {
            Console.WriteLine("NeedAuthenticationAttribute");
        }
    }
}
