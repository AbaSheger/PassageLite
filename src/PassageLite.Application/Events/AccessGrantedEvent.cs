namespace PassageLite.Application.Events;

public record AccessGrantedEvent(
    Guid GrantId,
    Guid UserId,
    Guid AreaId,
    string AreaName,
    DateTime ValidFrom,
    DateTime ValidTo,
    DateTime OccurredAt
);
