using GreenOps.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenOps.WebApi.Controllers;

[Route("api/audit-logs")]
[Authorize(Roles = AdminRoles)]
public class AuditLogsController : ApiControllerBase
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogsController(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    /// <summary>GET /api/audit-logs — Denetim logları (SuperAdmin tümü/seçili şirket, CompanyAdmin kendi şirketi)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        Guid? scope;
        if (IsSuperAdmin)
            scope = companyId is { } c && c != Guid.Empty ? c : null;
        else if (TryResolveCompany(companyId, out var cid))
            scope = cid;
        else
            return CompanyForbidden();

        var paged = await _auditLogRepository.GetPagedAsync(scope, page, pageSize, ct);

        return Ok(new
        {
            totalCount = paged.TotalCount,
            page = paged.Page,
            pageSize = paged.PageSize,
            data = paged.Items.Select(l => new
            {
                l.Id, l.UserId, l.UserName, l.CompanyId, l.Action, l.EntityName, l.EntityId,
                l.OldValues, l.NewValues, l.CreatedAt
            })
        });
    }
}
