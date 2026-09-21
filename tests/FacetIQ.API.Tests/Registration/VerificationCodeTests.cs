using FacetIQ.API.Registration;

namespace FacetIQ.API.Tests.Registration;

public class VerificationCodeTests
{
    [Fact]
    public void Generate_IsSixDigits()
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var code = VerificationCode.Generate();

            Assert.Equal(6, code.Length);
            Assert.All(code, character => Assert.True(char.IsAsciiDigit(character)));
        }
    }

    // Not a randomness test, which a unit test cannot do. It catches a constant or an off by one
    // that leaves a digit position always the same.
    [Fact]
    public void Generate_UsesTheWholeRange()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => VerificationCode.Generate()).ToList();

        // Each digit position must have variation to catch hardcoded positions.
        for (var position = 0; position < 6; position++)
        {
            var digitsAtPosition = codes.Select(code => code[position]).Distinct().Count();
            Assert.True(digitsAtPosition > 1, $"Position {position} has no variation");
        }
    }

    [Fact]
    public void Matches_IsTrueOnlyForTheCodeThatWasHashed()
    {
        var code = VerificationCode.Generate();
        var hash = VerificationCode.Hash(code);

        Assert.True(VerificationCode.Matches(code, hash));
        Assert.False(VerificationCode.Matches("000000", hash));
    }

    [Fact]
    public void CancellationToken_IsSafeInAUrl()
    {
        // Base64url must not contain +, /, or = which corrupt in query strings.
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var token = VerificationCode.GenerateCancellationToken();

            Assert.NotEmpty(token);
            Assert.DoesNotContain("+", token);
            Assert.DoesNotContain("/", token);
            Assert.DoesNotContain("=", token);
        }
    }
}
