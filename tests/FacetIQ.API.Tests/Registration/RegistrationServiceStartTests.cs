using FacetIQ.API.Registration;

namespace FacetIQ.API.Tests.Registration;

public class RegistrationServiceStartTests
{
    [Fact]
    public async Task NewAddress_WritesAPendingRowAndSendsACode()
    {
        var world = new RegistrationWorld();

        var result = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        Assert.Equal(StartOutcome.CodeSent, result.Outcome);
        Assert.Single(world.Store.Rows);
        Assert.Single(world.Transport.Sent);
        Assert.NotEqual(Guid.Empty, result.RegistrationId);
    }

    [Fact]
    public async Task AddressWithAnAccount_IsRefusedAndTheAddressIsTold()
    {
        var world = new RegistrationWorld();
        await world.CreateAccount("riya@example.test");

        var result = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        Assert.Equal(StartOutcome.AlreadyRegistered, result.Outcome);
        Assert.Empty(world.Store.Rows);
        Assert.Contains("already exists", Assert.Single(world.Transport.Sent).HtmlBody);
    }

    [Fact]
    public async Task WeakPassword_IsRefusedWithoutWritingOrSending()
    {
        var world = new RegistrationWorld();

        var result = await world.Service.StartAsync("riya@example.test", "x", default);

        Assert.Equal(StartOutcome.Invalid, result.Outcome);
        Assert.Contains(result.Errors, error => error.Code.Contains("Password"));
        Assert.Empty(world.Store.Rows);
        Assert.Empty(world.Transport.Sent);
    }

    // Two people may be registering the same address. Neither may block the other.
    [Fact]
    public async Task SecondAttemptForTheSameAddress_IsAllowed()
    {
        var world = new RegistrationWorld();

        await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var second = await world.Service.StartAsync("riya@example.test", "Another-password-2!", default);

        Assert.Equal(StartOutcome.CodeSent, second.Outcome);
        Assert.Equal(2, world.Store.Rows.Count);
    }

    [Fact]
    public async Task ExpiredAttemptsForTheAddress_AreCleanedUpOnTheNextAttempt()
    {
        var world = new RegistrationWorld();
        await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        world.Clock.Advance(TimeSpan.FromMinutes(11));
        await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        Assert.Single(world.Store.Rows);
    }

    [Fact]
    public async Task ThePasswordIsNeverStoredInTheClear()
    {
        var world = new RegistrationWorld();

        await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        Assert.DoesNotContain("A-good-password-1!", Assert.Single(world.Store.Rows).PasswordHash);
    }

    [Fact]
    public async Task ConfiguredBaseUrlEndingInASlash_DoesNotProduceADoubleSlash()
    {
        var world = new RegistrationWorld("http://localhost:5173/");

        await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        var body = Assert.Single(world.Transport.Sent).HtmlBody ?? string.Empty;

        Assert.Equal(1, CountOccurrences(body, "/registration/cancel?token="));
        Assert.DoesNotContain("//registration", body);
    }

    // Each of these passes [EmailAddress] but is delivered to riya@example.test.
    [Theory]
    [InlineData("riya@example.test ")]
    [InlineData(" riya@example.test")]
    [InlineData("<riya@example.test>")]
    [InlineData("Riya Nair <riya@example.test>")]
    [InlineData("riya@example.test\t")]
    public async Task AddressThatDoesNotRoundTrip_IsRejectedRatherThanNarrowed(string decorated)
    {
        var world = new RegistrationWorld();

        var result = await world.Service.StartAsync(decorated, "A-good-password-1!", default);

        Assert.Equal(StartOutcome.Invalid, result.Outcome);
        Assert.Contains(result.Errors, error => error.Code == "InvalidEmail");
        Assert.Empty(world.Store.Rows);
        Assert.Empty(world.Transport.Sent);
    }

    // MailboxAddress.Parse throws for this value.
    [Fact]
    public async Task UnparseableAddress_IsRejectedRatherThanThrowing()
    {
        var world = new RegistrationWorld();

        var result = await world.Service.StartAsync("a@b,c", "A-good-password-1!", default);

        Assert.Equal(StartOutcome.Invalid, result.Outcome);
        Assert.Contains(result.Errors, error => error.Code == "InvalidEmail");
        Assert.Empty(world.Store.Rows);
        Assert.Empty(world.Transport.Sent);
    }

    // Identity's default user name rules exclude apostrophes.
    [Fact]
    public async Task AddressIdentitysDefaultValidatorRejects_IsInvalidRatherThanSilentlyAccepted()
    {
        var world = new RegistrationWorld();

        var result = await world.Service.StartAsync("o'brien@example.test", "A-good-password-1!", default);

        Assert.Equal(StartOutcome.Invalid, result.Outcome);
        Assert.Empty(world.Store.Rows);
        Assert.Empty(world.Transport.Sent);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while (true)
        {
            index = text.IndexOf(value, index, StringComparison.Ordinal);

            if (index < 0)
            {
                break;
            }

            count++;
            index += value.Length;
        }

        return count;
    }
}
