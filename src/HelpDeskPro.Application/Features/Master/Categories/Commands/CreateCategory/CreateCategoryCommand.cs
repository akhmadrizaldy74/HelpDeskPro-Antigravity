namespace HelpDeskPro.Application.Features.Master.Categories.Commands.CreateCategory;

using HelpDeskPro.Application.Features.Master.Categories.Models;
using MediatR;

public sealed record CreateCategoryCommand(string Name) : IRequest<CategoryDto>;

