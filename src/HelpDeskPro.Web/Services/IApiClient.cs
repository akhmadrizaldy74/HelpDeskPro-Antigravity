namespace HelpDeskPro.Web.Services;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Auth.Models;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Application.Features.Escalations.Models;
using HelpDeskPro.Application.Features.Master.Categories.Commands.CreateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Commands.UpdateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Models;
using HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Commands.UpdateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Models;
using HelpDeskPro.Application.Features.Master.Users.Commands.CreateUser;
using HelpDeskPro.Application.Features.Master.Users.Commands.UpdateUser;
using HelpDeskPro.Application.Features.Master.Users.Models;
using HelpDeskPro.Application.Features.Notifications.Models;
using HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;
using HelpDeskPro.Domain.Enums;

public interface IApiClient
{
    // Auth
    Task<ApiResponse<AuthResponseDto>?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    // Notifications
    Task<ApiResponse<int>?> GetUnreadNotificationCountAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<NotificationDto>>?> GetNotificationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<int>?> MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken = default);

    // Tickets
    Task<ApiResponse<PagedResult<TicketListItemDto>>?> GetTicketsAsync(GetTicketsQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketFilterOptionsDto>?> GetTicketFilterOptionsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketDetailDto>?> GetTicketByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketDetailDto>?> CreateTicketAsync(CreateTicketCommand command, CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketDetailDto>?> AssignTicketAsync(long id, Guid assigneeId, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketDetailDto>?> ChangeTicketStatusAsync(long id, TicketStatus status, string? reason, byte[]? rowVersion, CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketCommentDto>?> AddCommentAsync(long id, string body, bool isInternal, CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketAttachmentDto>?> UploadAttachmentAsync(long id, Stream stream, string fileName, string contentType, long length, CancellationToken cancellationToken = default);
    Task<ApiResponse<TicketDetailDto>?> RateTicketAsync(long id, int rating, string? comment, CancellationToken cancellationToken = default);
    Task<ApiResponse<EscalationDto>?> EscalateTicketAsync(long id, string note, CancellationToken cancellationToken = default);

    // Dashboard & Escalation
    Task<ApiResponse<DashboardSummaryDto>?> GetDashboardSummaryAsync(int periodDays = 30, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<AgentWorkloadDto>>?> GetAgentWorkloadAsync(int? days = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<PendingEscalationDto>>?> GetPendingEscalationsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<EscalationDto>?> AcknowledgeEscalationAsync(long id, CancellationToken cancellationToken = default);

    // Master Departments
    Task<ApiResponse<IReadOnlyList<DepartmentDto>>?> GetDepartmentsAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartmentDto>?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartmentDto>?> CreateDepartmentAsync(CreateDepartmentCommand command, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartmentDto>?> UpdateDepartmentAsync(int id, UpdateDepartmentCommand command, CancellationToken cancellationToken = default);
    Task<ApiResponse<DepartmentDto>?> ToggleDepartmentStatusAsync(int id, bool? isActive = null, CancellationToken cancellationToken = default);

    // Master Categories
    Task<ApiResponse<IReadOnlyList<CategoryDto>>?> GetCategoriesAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>?> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>?> UpdateCategoryAsync(int id, UpdateCategoryCommand command, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>?> ToggleCategoryStatusAsync(int id, bool? isActive = null, CancellationToken cancellationToken = default);

    // Master Users
    Task<ApiResponse<PagedResult<UserListItemDto>>?> GetUsersAsync(int? departmentId = null, string? role = null, bool? isActive = null, string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDetailDto>?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDetailDto>?> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDetailDto>?> UpdateUserAsync(Guid id, UpdateUserCommand command, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDetailDto>?> ToggleUserStatusAsync(Guid id, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<string>?> ResetUserPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default);

    // Master SLA Policies
    Task<ApiResponse<IReadOnlyList<SlaPolicyDto>>?> GetSlaPoliciesAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<SlaPolicyDto>?> UpdateSlaPolicyAsync(TicketPriority priority, int responseTimeMinutes, int resolutionTimeMinutes, CancellationToken cancellationToken = default);
}
