using FacetIQ.API.Registration;
using FacetIQ.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace FacetIQ.API.Tests.Registration;

public class RegistrationServiceConfirmTests
{
    [Fact]
    public async Task RightCode_CreatesOneConfirmedAccountWithASubject()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        var outcome = await world.Service.ConfirmAsync(
            started.RegistrationId,
            world.LastCode,
            "A-good-password-1!",
            default);

        Assert.Equal(ConfirmOutcome.Created, outcome);
        var user = Assert.Single(world.Users);
        Assert.True(user.EmailConfirmed);
        Assert.Single(world.Subjects.Rows);
    }

    [Fact]
    public async Task ConfirmedAccount_KeepsTheAddressAsTyped()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("Riya.Naidoo@Example.Test", "A-good-password-1!", default);

        await world.Service.ConfirmAsync(started.RegistrationId, world.LastCode, "A-good-password-1!", default);

        var user = Assert.Single(world.Users);
        Assert.Equal("Riya.Naidoo@Example.Test", user.Email);
        Assert.Equal("Riya.Naidoo@Example.Test", user.UserName);
    }

    [Fact]
    public async Task TheStoredPasswordWorksAfterwards()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        await world.Service.ConfirmAsync(started.RegistrationId, world.LastCode, "A-good-password-1!", default);

        var user = Assert.Single(world.Users);
        Assert.Equal(
            PasswordVerificationResult.Success,
            world.Users.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, "A-good-password-1!"));
    }

    [Fact]
    public async Task SuccessRemovesEveryPendingAttemptForThatAddress()
    {
        var world = new RegistrationWorld();
        var mine = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        await world.Service.StartAsync("riya@example.test", "Someone-elses-2!", default);

        await world.Service.ConfirmAsync(
            mine.RegistrationId,
            world.CodeFor(mine.RegistrationId),
            "A-good-password-1!",
            default);

        Assert.Empty(world.Store.Rows);
    }

    [Fact]
    public async Task WrongCode_CountsAnAttemptAndKeepsThePending()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        var outcome = await world.Service.ConfirmAsync(
            started.RegistrationId,
            "000000",
            "A-good-password-1!",
            default);

        Assert.Equal(ConfirmOutcome.Invalid, outcome);
        Assert.Equal(1, Assert.Single(world.Store.Rows).Attempts);
    }

    [Fact]
    public async Task WrongPassword_CountsAnAttemptAndKeepsThePendingAndCreatesNoAccount()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        var outcome = await world.Service.ConfirmAsync(
            started.RegistrationId,
            world.LastCode,
            "A-different-password-1!",
            default);

        Assert.Equal(ConfirmOutcome.Invalid, outcome);
        Assert.Equal(1, Assert.Single(world.Store.Rows).Attempts);
        Assert.Empty(world.Users);
    }

    // The code arrives in the inbox of someone who does not know the password it was started with.
    [Fact]
    public async Task RightCodeWithSomeoneElsesPassword_TheSharedBrowserCase_CreatesNoAccount()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "Owners-password-1!", default);
        var code = world.LastCode;

        var outcome = await world.Service.ConfirmAsync(
            started.RegistrationId,
            code,
            "Walk-up-users-password-1!",
            default);

        Assert.Equal(ConfirmOutcome.Invalid, outcome);
        Assert.Empty(world.Users);
    }

    [Fact]
    public async Task FifthWrongCode_DestroysThePending()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await world.Service.ConfirmAsync(started.RegistrationId, "000000", "A-good-password-1!", default);
        }

        Assert.Empty(world.Store.Rows);
    }

    [Fact]
    public async Task FifthWrongPassword_DestroysThePending()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var code = world.LastCode;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await world.Service.ConfirmAsync(started.RegistrationId, code, "A-different-password-1!", default);
        }

        Assert.Empty(world.Store.Rows);
    }

    [Fact]
    public async Task ExpiredCode_IsRefused()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        world.Clock.Advance(TimeSpan.FromMinutes(11));

        Assert.Equal(
            ConfirmOutcome.Invalid,
            await world.Service.ConfirmAsync(started.RegistrationId, world.LastCode, "A-good-password-1!", default));
    }

    // The address became an account between starting and confirming.
    [Fact]
    public async Task AddressTakenMeanwhile_IsRefusedAndThePendingIsCleared()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        await world.CreateAccount("riya@example.test");

        var outcome = await world.Service.ConfirmAsync(
            started.RegistrationId,
            world.LastCode,
            "A-good-password-1!",
            default);

        Assert.Equal(ConfirmOutcome.AlreadyRegistered, outcome);
        Assert.Empty(world.Store.Rows);
    }

    [Fact]
    public async Task Resend_SendsANewCodeAndRetiresTheOldOne()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var first = world.LastCode;

        world.Clock.Advance(TimeSpan.FromSeconds(30));
        await world.Service.ResendAsync(started.RegistrationId, default);

        Assert.NotEqual(first, world.LastCode);
        Assert.Equal(
            ConfirmOutcome.Invalid,
            await world.Service.ConfirmAsync(started.RegistrationId, first, "A-good-password-1!", default));
    }

    [Fact]
    public async Task FourthResend_SendsNothing()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        for (var resend = 0; resend < 4; resend++)
        {
            world.Clock.Advance(TimeSpan.FromSeconds(30));
            await world.Service.ResendAsync(started.RegistrationId, default);
        }

        // One code at the start, three resends.
        Assert.Equal(4, world.Transport.Sent.Count);
    }

    [Fact]
    public async Task ResendForAnUnknownRegistration_DoesNothingAndDoesNotThrow()
    {
        var world = new RegistrationWorld();

        var result = await world.Service.ResendAsync(Guid.NewGuid(), default);

        Assert.Equal(new ResendResult(false, 0, 0), result);
        Assert.Empty(world.Transport.Sent);
    }

    [Fact]
    public async Task ResendInsideTheCooldown_LeavesTheOriginalCodeWorking()
    {
        var world = new RegistrationWorld(shareClockWithThrottle: true);
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var first = world.LastCode;

        await world.Service.ResendAsync(started.RegistrationId, default);

        Assert.Single(world.Transport.Sent);
        Assert.Equal(
            ConfirmOutcome.Created,
            await world.Service.ConfirmAsync(started.RegistrationId, first, "A-good-password-1!", default));
    }

    [Fact]
    public async Task ResendInsideTheCooldown_SendsNothingAndReportsTheSecondsRemaining()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        world.Clock.Advance(TimeSpan.FromSeconds(11));
        var result = await world.Service.ResendAsync(started.RegistrationId, default);

        Assert.Equal(new ResendResult(false, 19, 3), result);
        Assert.Single(world.Transport.Sent);
    }

    [Fact]
    public async Task ResendAfterTheCooldown_SendsAndReportsOneFewerResendLeft()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        world.Clock.Advance(TimeSpan.FromSeconds(30));
        var result = await world.Service.ResendAsync(started.RegistrationId, default);

        Assert.Equal(new ResendResult(true, 30, 2), result);
        Assert.Equal(2, world.Transport.Sent.Count);
    }

    [Fact]
    public async Task FourthResend_ReportsNoneLeft()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        for (var resend = 0; resend < 3; resend++)
        {
            world.Clock.Advance(TimeSpan.FromSeconds(30));
            await world.Service.ResendAsync(started.RegistrationId, default);
        }

        world.Clock.Advance(TimeSpan.FromSeconds(30));
        var result = await world.Service.ResendAsync(started.RegistrationId, default);

        Assert.Equal(new ResendResult(false, 0, 0), result);
    }

    // Driven as two sequential calls on a single thread rather than truly in parallel: the first
    // call's claim moves LastSentAt, so the second sees a row it is no longer inside the window for.
    [Fact]
    public async Task TwoResendsAtTheSameInstant_OnlyOneClaimsTheSlot()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        world.Clock.Advance(TimeSpan.FromSeconds(30));
        var first = await world.Service.ResendAsync(started.RegistrationId, default);
        var second = await world.Service.ResendAsync(started.RegistrationId, default);

        Assert.True(first.Sent);
        Assert.False(second.Sent);
        Assert.Equal(2, world.Transport.Sent.Count);
        Assert.Equal(1, Assert.Single(world.Store.Rows).Resends);
    }

    [Fact]
    public async Task ResendWithDeliveryFailure_LeavesTheOriginalCodeWorkingAndTheAllowanceIntact()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var first = world.LastCode;

        world.Clock.Advance(TimeSpan.FromSeconds(30));
        world.Transport.FailNextSend = true;
        var result = await world.Service.ResendAsync(started.RegistrationId, default);

        Assert.False(result.Sent);
        Assert.Equal(3, result.ResendsLeft);
        Assert.Equal(0, Assert.Single(world.Store.Rows).Resends);
        Assert.Equal(
            ConfirmOutcome.Created,
            await world.Service.ConfirmAsync(started.RegistrationId, first, "A-good-password-1!", default));
    }

    [Fact]
    public async Task Cancel_RemovesEveryAttemptForTheAddress()
    {
        var world = new RegistrationWorld();
        var mine = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        await world.Service.StartAsync("riya@example.test", "Someone-elses-2!", default);

        await world.Service.CancelAsync(world.CancellationTokenFor(mine.RegistrationId), default);

        Assert.Empty(world.Store.Rows);
    }

    [Fact]
    public async Task CancelWithAnUnknownToken_DoesNothingAndDoesNotThrow()
    {
        var world = new RegistrationWorld();
        await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        await world.Service.CancelAsync("not-a-real-token", default);

        Assert.Single(world.Store.Rows);
    }

    [Fact]
    public async Task CancelLinkFromBeforeAResend_StillRemovesThePendingRows()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        var tokenBeforeResend = world.CancellationTokenFor(started.RegistrationId);

        world.Clock.Advance(TimeSpan.FromSeconds(30));
        await world.Service.ResendAsync(started.RegistrationId, default);

        await world.Service.CancelAsync(tokenBeforeResend, default);

        Assert.Empty(world.Store.Rows);
    }

    // Written straight to the store, since StartAsync would refuse this address before a row exists.
    [Fact]
    public async Task CreateAsyncFailureThatIsNotADuplicate_IsFailedRatherThanAlreadyRegistered()
    {
        var world = new RegistrationWorld();
        var pending = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            Email = "o'brien@example.test",
            NormalizedEmail = "O'BRIEN@EXAMPLE.TEST",
            PasswordHash = world.Users.PasswordHasher.HashPassword(new AppUser(), "A-good-password-1!"),
            CodeHash = VerificationCode.Hash("123456"),
            CancellationTokenHash = VerificationCode.Hash("token"),
            CreatedAt = world.Clock.GetUtcNow(),
            ExpiresAt = world.Clock.GetUtcNow() + TimeSpan.FromMinutes(10)
        };
        await world.Store.AddAsync(pending, default);

        var outcome = await world.Service.ConfirmAsync(pending.Id, "123456", "A-good-password-1!", default);

        Assert.Equal(ConfirmOutcome.Failed, outcome);
        Assert.Empty(world.Users);
        Assert.Single(world.Store.Rows);
    }

    [Fact]
    public async Task ProvisioningFailure_StillYieldsCreatedWithNoPendingRowsLeft()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);
        world.Subjects.FailNextAdd = true;

        var outcome = await world.Service.ConfirmAsync(
            started.RegistrationId,
            world.LastCode,
            "A-good-password-1!",
            default);

        Assert.Equal(ConfirmOutcome.Created, outcome);
        var user = Assert.Single(world.Users);
        Assert.True(user.EmailConfirmed);
        Assert.Empty(world.Store.Rows);
        Assert.Empty(world.Subjects.Rows);
    }

    [Fact]
    public async Task CodeFromAResend_LastsTheFullLifetimeFromWhenItWasSent()
    {
        var world = new RegistrationWorld();
        var started = await world.Service.StartAsync("riya@example.test", "A-good-password-1!", default);

        world.Clock.Advance(TimeSpan.FromMinutes(9));
        await world.Service.ResendAsync(started.RegistrationId, default);
        world.Clock.Advance(TimeSpan.FromMinutes(2));

        var outcome = await world.Service.ConfirmAsync(
            started.RegistrationId,
            world.LastCode,
            "A-good-password-1!",
            default);

        Assert.Equal(ConfirmOutcome.Created, outcome);
    }
}
