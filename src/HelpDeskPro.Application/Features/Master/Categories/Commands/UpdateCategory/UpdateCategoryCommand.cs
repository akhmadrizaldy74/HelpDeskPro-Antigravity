namespace HelpDeskPro.Application.Features.Master.Categories.Commands.UpdateCategory;

using HelpDeskPro.Application.Features.Master.Categories.Models;
using MediatR;

public sealed record UpdateCategoryCommand(int Id, string Name, bool IsActive) : IRequest<CategoryDto>;

