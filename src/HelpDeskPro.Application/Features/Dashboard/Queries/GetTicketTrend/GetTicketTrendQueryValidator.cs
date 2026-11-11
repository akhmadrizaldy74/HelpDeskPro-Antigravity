namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetTicketTrend;

using FluentValidation;

public sealed class GetTicketTrendQueryValidator : AbstractValidator<GetTicketTrendQuery>
{
    public GetTicketTrendQueryValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 31)
            .WithMessage("Parameter hari harus di antara 1 sampai 31.");
    }
}
