using PassageLite.Application.Events;
using PassageLite.Application.Interfaces;

namespace PassageLite.Application.Services;

public class NoOpAccessEventPublisher : IAccessEventPublisher
{
    public Task PublishAccessGrantedAsync(AccessGrantedEvent accessGrantedEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
