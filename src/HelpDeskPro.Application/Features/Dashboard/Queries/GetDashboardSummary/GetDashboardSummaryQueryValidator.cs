namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

using FluentValidation;

public sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(x => x.PeriodDays)
            .Must(x => x == 7 || x == 30)
            .WithMessage("Parameter periode harus bernilai 7 atau 30 hari.");
    }
}

