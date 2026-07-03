using PassageLite.Application.DTOs;
using PassageLite.Application.Events;
using PassageLite.Application.Interfaces;
using PassageLite.Domain.Entities;
using PassageLite.Domain.Interfaces;

namespace PassageLite.Application.Services;

public class AccessService : IAccessService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessEventPublisher _accessEventPublisher;

    public AccessService(IUnitOfWork unitOfWork, IAccessEventPublisher accessEventPublisher)
    {
        _unitOfWork = unitOfWork;
        _accessEventPublisher = accessEventPublisher;
    }

    public async Task<AccessGrantDto> GrantAccessAsync(GrantAccessRequest request)
    {
        // Check if user exists
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null)
        {
            throw new ArgumentException("User not found");
        }

        // Check if area exists
        var area = await _unitOfWork.Areas.GetByIdAsync(request.AreaId);
        if (area == null)
        {
            throw new ArgumentException("Area not found");
        }

        // Check for existing active grant
        var existingGrant = await _unitOfWork.AccessGrants.GetActiveGrantAsync(request.UserId, request.AreaId);
        if (existingGrant != null)
        {
            // Update existing grant
            existingGrant.ValidFrom = request.ValidFrom;
            existingGrant.ValidTo = request.ValidTo;
            existingGrant.IsRevoked = false;
            await _unitOfWork.AccessGrants.UpdateAsync(existingGrant);
            await _unitOfWork.SaveChangesAsync();
            await PublishAccessGrantedAsync(existingGrant, area.Name);

            return new AccessGrantDto(
                existingGrant.Id,
                existingGrant.UserId,
                existingGrant.AreaId,
                area.Name,
                existingGrant.ValidFrom,
                existingGrant.ValidTo,
                existingGrant.IsRevoked,
                existingGrant.IsCurrentlyValid()
            );
        }

        // Create new grant
        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            AreaId = request.AreaId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AccessGrants.AddAsync(grant);
        await _unitOfWork.SaveChangesAsync();
        await PublishAccessGrantedAsync(grant, area.Name);

        return new AccessGrantDto(
            grant.Id,
            grant.UserId,
            grant.AreaId,
            area.Name,
            grant.ValidFrom,
            grant.ValidTo,
            grant.IsRevoked,
            grant.IsCurrentlyValid()
        );
    }

    public async Task<bool> RevokeAccessAsync(RevokeAccessRequest request)
    {
        var grant = await _unitOfWork.AccessGrants.GetActiveGrantAsync(request.UserId, request.AreaId);
        
        if (grant == null)
        {
            return false;
        }

        grant.IsRevoked = true;
        await _unitOfWork.AccessGrants.UpdateAsync(grant);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<AccessGrantDto>> GetUserAccessGrantsAsync(Guid userId)
    {
        var grants = await _unitOfWork.AccessGrants.GetByUserIdAsync(userId);
        
        return grants.Select(g => new AccessGrantDto(
            g.Id,
            g.UserId,
            g.AreaId,
            g.Area.Name,
            g.ValidFrom,
            g.ValidTo,
            g.IsRevoked,
            g.IsCurrentlyValid()
        ));
    }

    public async Task<AccessCheckResult> CheckAccessAsync(Guid userId, Guid areaId)
    {
        // Check if area exists
        var area = await _unitOfWork.Areas.GetByIdAsync(areaId);
        if (area == null)
        {
            return new AccessCheckResult(false, "Area not found");
        }

        var grant = await _unitOfWork.AccessGrants.GetActiveGrantAsync(userId, areaId);

        if (grant == null)
        {
            return new AccessCheckResult(false, "No access grant found for this area");
        }

        if (grant.IsRevoked)
        {
            return new AccessCheckResult(false, "Access has been revoked");
        }

        var now = DateTime.UtcNow;

        if (now < grant.ValidFrom)
        {
            return new AccessCheckResult(false, $"Access not yet valid. Starts at {grant.ValidFrom:u}");
        }

        if (now > grant.ValidTo)
        {
            return new AccessCheckResult(false, $"Access has expired. Ended at {grant.ValidTo:u}");
        }

        return new AccessCheckResult(true, $"Access granted to {area.Name}");
    }

    private Task PublishAccessGrantedAsync(AccessGrant grant, string areaName)
    {
        var accessGrantedEvent = new AccessGrantedEvent(
            grant.Id,
            grant.UserId,
            grant.AreaId,
            areaName,
            grant.ValidFrom,
            grant.ValidTo,
            DateTime.UtcNow
        );

        return _accessEventPublisher.PublishAccessGrantedAsync(accessGrantedEvent);
    }
}
