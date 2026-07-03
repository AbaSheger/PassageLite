using PassageLite.Application.Events;

namespace PassageLite.Application.Interfaces;

public interface IAccessEventPublisher
{
    Task PublishAccessGrantedAsync(AccessGrantedEvent accessGrantedEvent, CancellationToken cancellationToken = default);
}
