using System.Collections.Concurrent;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public class UserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public IEnumerable<User> GetAll() => _users.Values.OrderBy(u => u.CreatedAt);

    public User? Get(Guid id) => _users.TryGetValue(id, out var user) ? user : null;

    public void Create(User user) => _users[user.Id] = user;

    public void Update(Guid id, User user) => _users[id] = user;

    public bool Delete(Guid id) => _users.TryRemove(id, out _);
}
