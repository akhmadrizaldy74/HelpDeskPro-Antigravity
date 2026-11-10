namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;

using FluentValidation;

public class GetTicketsQueryValidator : AbstractValidator<GetTicketsQuery>
{
    public GetTicketsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Nomor halaman minimal 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Ukuran halaman harus antara 1 dan 100.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom!.Value).WithMessage("Tanggal akhir harus lebih besar atau sama dengan tanggal awal.")
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}

