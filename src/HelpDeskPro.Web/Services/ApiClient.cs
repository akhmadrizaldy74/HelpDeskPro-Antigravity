namespace HelpDeskPro.Web.Services;

using System.Net.Http.Json;
using System.Text.Json;
using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Auth.Commands.Login;
using HelpDeskPro.Application.Features.Auth.Models;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Application.Features.Escalations.Models;
using HelpDeskPro.Application.Features.Master.Categories.Commands.CreateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Commands.UpdateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Models;
using HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Commands.UpdateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.CreateHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.UpdateHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Models;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Models;
using HelpDeskPro.Application.Features.Master.Users.Commands.CreateUser;
using HelpDeskPro.Application.Features.Master.Users.Commands.UpdateUser;
using HelpDeskPro.Application.Features.Master.Users.Models;
using HelpDeskPro.Application.Features.Notifications.Models;
using HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;
using HelpDeskPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<AuthResponseDto>?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var command = new LoginCommand(email, password);
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", command, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<AuthResponseDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<UserDto>?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/auth/me", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<UserDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<int>?> GetUnreadNotificationCountAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/notifications/unread-count", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResponse.Ok(0);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<int>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<NotificationDto>>?> GetNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/notifications", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<IReadOnlyList<NotificationDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<NotificationDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<int>?> MarkAllNotificationsAsReadAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsync("api/notifications/read-all", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<int>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<int>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<PagedResult<TicketListItemDto>>?> GetTicketsAsync(GetTicketsQuery query, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"page={query.Page}",
            $"pageSize={query.PageSize}"
        };

        if (query.Statuses is { Count: > 0 })
        {
            foreach (var status in query.Statuses)
            {
                queryParams.Add($"statuses={(int)status}");
            }
        }

        if (query.Priorities is { Count: > 0 })
        {
            foreach (var priority in query.Priorities)
            {
                queryParams.Add($"priorities={(int)priority}");
            }
        }

        if (query.CategoryId.HasValue)
        {
            queryParams.Add($"categoryId={query.CategoryId.Value}");
        }

        if (query.AssigneeId.HasValue)
        {
            queryParams.Add($"assigneeId={query.AssigneeId.Value}");
        }

        if (query.Unassigned.HasValue)
        {
            queryParams.Add($"unassigned={query.Unassigned.Value.ToString().ToLowerInvariant()}");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(query.Search.Trim())}");
        }

        var url = $"api/tickets?{string.Join("&", queryParams)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<PagedResult<TicketListItemDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TicketListItemDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketFilterOptionsDto>?> GetTicketFilterOptionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/tickets/filter-options", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketFilterOptionsDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketFilterOptionsDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketDetailDto>?> GetTicketByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/tickets/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketDetailDto>?> CreateTicketAsync(CreateTicketCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tickets", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketDetailDto>?> AssignTicketAsync(long id, Guid assigneeId, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var payload = new { AssigneeId = assigneeId, RowVersion = rowVersion };
        var response = await _httpClient.PatchAsJsonAsync($"api/tickets/{id}/assign", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketDetailDto>?> ChangeTicketStatusAsync(long id, TicketStatus status, string? reason, byte[]? rowVersion, CancellationToken cancellationToken = default)
    {
        var payload = new { Status = status, Reason = reason, RowVersion = rowVersion };
        var response = await _httpClient.PatchAsJsonAsync($"api/tickets/{id}/status", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketCommentDto>?> AddCommentAsync(long id, string body, bool isInternal, CancellationToken cancellationToken = default)
    {
        var payload = new { Body = body, IsInternal = isInternal };
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{id}/comments", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketCommentDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketCommentDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketAttachmentDto>?> UploadAttachmentAsync(long id, Stream stream, string fileName, string contentType, long length, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        content.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync($"api/tickets/{id}/attachments", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketAttachmentDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketAttachmentDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<TicketDetailDto>?> RateTicketAsync(long id, int rating, string? comment, CancellationToken cancellationToken = default)
    {
        var payload = new { Rating = rating, Comment = comment };
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{id}/rate", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<TicketDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<TicketDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<EscalationDto>?> EscalateTicketAsync(long id, string note, CancellationToken cancellationToken = default)
    {
        var payload = new { Note = note };
        var response = await _httpClient.PostAsJsonAsync($"api/tickets/{id}/escalate", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<EscalationDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<EscalationDto>>(_jsonOptions, cancellationToken);
    }

    // Dashboard & Escalation
    public async Task<ApiResponse<DashboardSummaryDto>?> GetDashboardSummaryAsync(int periodDays = 30, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/dashboard/summary?periodDays={periodDays}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<DashboardSummaryDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<AgentWorkloadDto>>?> GetAgentWorkloadAsync(int? days = null, CancellationToken cancellationToken = default)
    {
        var url = days.HasValue ? $"api/dashboard/agent-workload?days={days.Value}" : "api/dashboard/agent-workload";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<IReadOnlyList<AgentWorkloadDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<AgentWorkloadDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<PendingEscalationDto>>?> GetPendingEscalationsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/dashboard/pending-escalations", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<IReadOnlyList<PendingEscalationDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PendingEscalationDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<EscalationDto>?> AcknowledgeEscalationAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsync($"api/escalations/{id}/acknowledge", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<EscalationDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<EscalationDto>>(_jsonOptions, cancellationToken);
    }

    // Master Departments
    public async Task<ApiResponse<IReadOnlyList<DepartmentDto>>?> GetDepartmentsAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var url = isActive.HasValue ? $"api/master/departments?isActive={isActive.Value.ToString().ToLowerInvariant()}" : "api/master/departments";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<IReadOnlyList<DepartmentDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<DepartmentDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<DepartmentDto>?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/master/departments/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<DepartmentDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<DepartmentDto>?> CreateDepartmentAsync(CreateDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/master/departments", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<DepartmentDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<DepartmentDto>?> UpdateDepartmentAsync(int id, UpdateDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/master/departments/{id}", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<DepartmentDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<DepartmentDto>?> ToggleDepartmentStatusAsync(int id, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var url = isActive.HasValue ? $"api/master/departments/{id}/toggle-status?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"api/master/departments/{id}/toggle-status";
        var response = await _httpClient.PatchAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<DepartmentDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentDto>>(_jsonOptions, cancellationToken);
    }

    // Master Categories
    public async Task<ApiResponse<IReadOnlyList<CategoryDto>>?> GetCategoriesAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var url = isActive.HasValue ? $"api/master/categories?isActive={isActive.Value.ToString().ToLowerInvariant()}" : "api/master/categories";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<IReadOnlyList<CategoryDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CategoryDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<CategoryDto>?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/master/categories/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<CategoryDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<CategoryDto>?> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/master/categories", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<CategoryDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<CategoryDto>?> UpdateCategoryAsync(int id, UpdateCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/master/categories/{id}", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<CategoryDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<CategoryDto>?> ToggleCategoryStatusAsync(int id, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var url = isActive.HasValue ? $"api/master/categories/{id}/toggle-status?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"api/master/categories/{id}/toggle-status";
        var response = await _httpClient.PatchAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<CategoryDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>(_jsonOptions, cancellationToken);
    }

    // Master Users
    public async Task<ApiResponse<PagedResult<UserListItemDto>>?> GetUsersAsync(int? departmentId = null, string? role = null, bool? isActive = null, string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (departmentId.HasValue) queryParams.Add($"departmentId={departmentId.Value}");
        if (!string.IsNullOrWhiteSpace(role)) queryParams.Add($"role={Uri.EscapeDataString(role)}");
        if (isActive.HasValue) queryParams.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");

        var url = $"api/master/users?{string.Join("&", queryParams)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<PagedResult<UserListItemDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserListItemDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<UserDetailDto>?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/master/users/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<UserDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<UserDetailDto>?> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/master/users", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<UserDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<UserDetailDto>?> UpdateUserAsync(Guid id, UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/master/users/{id}", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<UserDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<UserDetailDto>?> ToggleUserStatusAsync(Guid id, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var url = isActive.HasValue ? $"api/master/users/{id}/toggle-status?isActive={isActive.Value.ToString().ToLowerInvariant()}" : $"api/master/users/{id}/toggle-status";
        var response = await _httpClient.PatchAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<UserDetailDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<string>?> ResetUserPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        var payload = new { NewPassword = newPassword };
        var response = await _httpClient.PostAsJsonAsync($"api/master/users/{id}/reset-password", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<string>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_jsonOptions, cancellationToken);
    }

    // Master SLA Policies
    public async Task<ApiResponse<IReadOnlyList<SlaPolicyDto>>?> GetSlaPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/master/sla-policies", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<IReadOnlyList<SlaPolicyDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SlaPolicyDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<SlaPolicyDto>?> UpdateSlaPolicyAsync(TicketPriority priority, int responseTimeMinutes, int resolutionTimeMinutes, CancellationToken cancellationToken = default)
    {
        var payload = new { ResponseTimeMinutes = responseTimeMinutes, ResolutionTimeMinutes = resolutionTimeMinutes };
        var response = await _httpClient.PutAsJsonAsync($"api/master/sla-policies/{priority}", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<SlaPolicyDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<SlaPolicyDto>>(_jsonOptions, cancellationToken);
    }

    // Master Holidays
    public async Task<ApiResponse<IReadOnlyList<HolidayDto>>?> GetHolidaysAsync(int? year = null, CancellationToken cancellationToken = default)
    {
        var url = year.HasValue ? $"api/master/holidays?year={year.Value}" : "api/master/holidays";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<IReadOnlyList<HolidayDto>>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<HolidayDto>>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<HolidayDto>?> CreateHolidayAsync(CreateHolidayCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/master/holidays", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<HolidayDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<HolidayDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<HolidayDto>?> UpdateHolidayAsync(int id, UpdateHolidayCommand command, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/master/holidays/{id}", command, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<HolidayDto>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<HolidayDto>>(_jsonOptions, cancellationToken);
    }

    public async Task<ApiResponse<string>?> DeleteHolidayAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/master/holidays/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ExtractErrorMessageAsync(response, cancellationToken);
            return ApiResponse.Failure<string>(error);
        }

        return await response.Content.ReadFromJsonAsync<ApiResponse<string>>(_jsonOptions, cancellationToken);
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return "Tiket sudah diubah pengguna lain, muat ulang halaman.";
        }

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return $"Permintaan gagal dengan status {response.StatusCode}.";
            }

            var problem = JsonSerializer.Deserialize<ProblemDetails>(content, _jsonOptions);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return problem.Detail;
            }

            if (problem?.Extensions.TryGetValue("errors", out var errorsObj) == true && errorsObj is JsonElement element && element.ValueKind == JsonValueKind.Object)
            {
                var messages = new List<string>();
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            var s = item.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                messages.Add(s);
                            }
                        }
                    }
                }
                if (messages.Count > 0)
                {
                    return string.Join("; ", messages);
                }
            }

            return problem?.Title ?? $"Terjadi kesalahan ({response.StatusCode}).";
        }
        catch
        {
            return $"Terjadi kesalahan komunikasi dengan server ({response.StatusCode}).";
        }
    }
}
