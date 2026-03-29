using System.ComponentModel.DataAnnotations;

namespace Demo.InternetBankingWeb.Banking;

public sealed class BankingPortalState
{
    private const string DemoUsername = "demo.klient";
    private const string DemoPassword = "Demo123!";
    private const string DemoAuthorizationCode = "246810";

    private readonly List<BankAccount> _accounts;
    private readonly List<AdvisoryMessage> _messages;

    public BankingPortalState()
    {
        _accounts =
        [
            new()
            {
                Id = "personal-main",
                Title = "Osobny ucet",
                AccountNumber = "SK12 5600 0000 0099 1122 3344",
                AvailableBalance = 4826.11m,
                BookedBalance = 4761.49m,
                Currency = "EUR",
                Accent = "var(--bank-accent)",
                Transactions =
                [
                    new() { Date = DateOnly.FromDateTime(DateTime.Today), Counterparty = "PRISMA ENERGIA", Description = "Mesačna zalohova platba", Reference = "SEPA 240329", Amount = -91.30m, Category = "Inkaso" },
                    new() { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), Counterparty = "TECHFABRIK S.R.O.", Description = "Vyplata marec", Reference = "SALARY 2026-03", Amount = 3180.00m, Category = "Prijem" },
                    new() { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), Counterparty = "MESTSKY DOPRAVCA", Description = "Predplatny listok", Reference = "CARD 8721", Amount = -30.00m, Category = "Karta" },
                    new() { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-4)), Counterparty = "SSE DISTRIBUCIA", Description = "Elektrina podnikanie", Reference = "SEPA 240325", Amount = -128.41m, Category = "Inkaso" }
                ]
            },
            new()
            {
                Id = "savings",
                Title = "Rezervny ucet",
                AccountNumber = "SK88 5600 0000 0077 2211 4455",
                AvailableBalance = 12440.80m,
                BookedBalance = 12440.80m,
                Currency = "EUR",
                Accent = "var(--bank-success)",
                Transactions =
                [
                    new() { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-8)), Counterparty = "Vlastny prevod", Description = "Odlozenie rezervy", Reference = "SAVING-UP", Amount = 500.00m, Category = "Presun" },
                    new() { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)), Counterparty = "Urok", Description = "Pripisany uroku", Reference = "INT-03", Amount = 4.80m, Category = "Urok" }
                ]
            }
        ];

        _messages =
        [
            new("Bezpecnost", "Nikdy nepotvrdzujte platbu, ktoru ste nezadali. Demo autorizacny kod je 246810."),
            new("Karty", "Nova karta Visa Business bude pripravena na prevzatie od 1. aprila."),
            new("Upozornenie", "V piatok medzi 22:00 a 22:30 prebehne planovana udrzba.")
        ];
    }

    public bool IsAuthenticated { get; private set; }

    public BankingUser? CurrentUser { get; private set; }

    public IReadOnlyList<BankAccount> Accounts => _accounts;

    public IReadOnlyList<AdvisoryMessage> Messages => _messages;

    public string DemoCredentials => $"{DemoUsername} / {DemoPassword}";

    public string DemoAuthorizationCodeHint => DemoAuthorizationCode;

    public bool Login(string username, string password)
    {
        if (!string.Equals(username?.Trim(), DemoUsername, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(password, DemoPassword, StringComparison.Ordinal))
        {
            return false;
        }

        IsAuthenticated = true;
        CurrentUser = new BankingUser
        {
            DisplayName = "Martin Demo",
            ClientNumber = "48092173",
            LastLoginUtc = DateTimeOffset.UtcNow.AddMinutes(-14),
            PreferredAccountId = _accounts[0].Id
        };

        return true;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        CurrentUser = null;
    }

    public BankAccount GetPreferredAccount() =>
        _accounts.First(account => account.Id == (CurrentUser?.PreferredAccountId ?? _accounts[0].Id));

    public IReadOnlyList<BankTransaction> SearchTransactions(string? query)
    {
        var allTransactions = _accounts
            .SelectMany(account => account.Transactions.Select(transaction => transaction with { AccountTitle = account.Title }))
            .OrderByDescending(transaction => transaction.Date)
            .ThenBy(transaction => transaction.Counterparty)
            .ToArray();

        if (string.IsNullOrWhiteSpace(query))
        {
            return allTransactions;
        }

        return allTransactions
            .Where(transaction =>
                transaction.Counterparty.Contains(query, StringComparison.OrdinalIgnoreCase)
                || transaction.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                || transaction.Reference.Contains(query, StringComparison.OrdinalIgnoreCase)
                || transaction.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public TransferReview CreateReview(TransferInput input)
    {
        var sourceAccount = _accounts.First(account => account.Id == input.SourceAccountId);

        return new TransferReview
        {
            SourceAccountId = sourceAccount.Id,
            SourceAccountTitle = sourceAccount.Title,
            SourceAccountNumber = sourceAccount.AccountNumber,
            RecipientName = input.RecipientName.Trim(),
            RecipientIban = input.RecipientIban.Trim(),
            Amount = input.Amount,
            VariableSymbol = input.VariableSymbol?.Trim() ?? string.Empty,
            Message = input.Message?.Trim() ?? string.Empty,
            AuthorizationCodeHint = DemoAuthorizationCode,
            ExecutionDate = DateOnly.FromDateTime(DateTime.Today),
            Reference = $"IB-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}"
        };
    }

    public TransferReceipt ConfirmTransfer(TransferReview review, string authorizationCode)
    {
        if (!string.Equals(authorizationCode.Trim(), DemoAuthorizationCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Autorizacny kod nie je spravny. Pre demo pouzite 246810.");
        }

        var sourceAccount = _accounts.First(account => account.Id == review.SourceAccountId);
        sourceAccount.AvailableBalance -= review.Amount;
        sourceAccount.BookedBalance -= review.Amount;
        sourceAccount.Transactions.Insert(0, new BankTransaction
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Counterparty = review.RecipientName,
            Description = string.IsNullOrWhiteSpace(review.Message) ? "Okamzita platba" : review.Message,
            Reference = review.Reference,
            Amount = -review.Amount,
            Category = "Platba"
        });

        return new TransferReceipt
        {
            Reference = review.Reference,
            RecipientName = review.RecipientName,
            RecipientIban = review.RecipientIban,
            Amount = review.Amount,
            SourceAccountTitle = review.SourceAccountTitle,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };
    }
}

public sealed class BankingUser
{
    public string DisplayName { get; set; } = string.Empty;

    public string ClientNumber { get; set; } = string.Empty;

    public DateTimeOffset LastLoginUtc { get; set; }

    public string PreferredAccountId { get; set; } = string.Empty;
}

public sealed class BankAccount
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public decimal AvailableBalance { get; set; }

    public decimal BookedBalance { get; set; }

    public string Currency { get; set; } = "EUR";

    public string Accent { get; set; } = "var(--bank-accent)";

    public List<BankTransaction> Transactions { get; set; } = [];
}

public record BankTransaction
{
    public DateOnly Date { get; init; }

    public string Counterparty { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Reference { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Category { get; init; } = string.Empty;

    public string? AccountTitle { get; init; }
}

public sealed record AdvisoryMessage(string Title, string Body);

public sealed class TransferInput
{
    [Required]
    public string SourceAccountId { get; set; } = "personal-main";

    [Required(ErrorMessage = "Zadajte meno prijemcu.")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zadajte IBAN prijemcu.")]
    [RegularExpression(@"^SK\d{2}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}$", ErrorMessage = "Pouzite slovensky IBAN v tvare SK..")]
    public string RecipientIban { get; set; } = "SK31 1100 0000 0029 8745 5412";

    [Range(typeof(decimal), "0.01", "50000", ErrorMessage = "Suma musi byt vacsia ako 0.")]
    public decimal Amount { get; set; } = 145.80m;

    public string? VariableSymbol { get; set; } = "202603";

    public string? Message { get; set; } = "Faktura za servis";
}

public sealed class TransferReview
{
    public string SourceAccountId { get; set; } = string.Empty;

    public string SourceAccountTitle { get; set; } = string.Empty;

    public string SourceAccountNumber { get; set; } = string.Empty;

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string VariableSymbol { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string AuthorizationCodeHint { get; set; } = string.Empty;

    public DateOnly ExecutionDate { get; set; }

    public string Reference { get; set; } = string.Empty;
}

public sealed class TransferReceipt
{
    public string Reference { get; set; } = string.Empty;

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string SourceAccountTitle { get; set; } = string.Empty;

    public DateTimeOffset SubmittedAtUtc { get; set; }
}
