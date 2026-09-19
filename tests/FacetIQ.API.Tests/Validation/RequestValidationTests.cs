using System.ComponentModel.DataAnnotations;
using FacetIQ.Contracts.Attributes;
using FacetIQ.Contracts.Disclosure;
using FacetIQ.Contracts.Norms;
using FacetIQ.Contracts.Standings;
using FacetIQ.Contracts.Validation;

namespace FacetIQ.API.Tests.Validation;

public class RequestValidationTests
{
    [Theory]
    [InlineData("Amara‮anidihc")]
    [InlineData("colle​ague")]
    [InlineData("﻿Amara")]
    [InlineData("two\nlines")]
    [InlineData("tab\there")]
    public void HiddenOrControlCharacters_AreRefused(string text)
    {
        Assert.False(new PlainTextAttribute().IsValid(text));
    }

    [Theory]
    [InlineData("Amara Chidinma Nwosu")]
    [InlineData("O'Brien-Smith")]
    [InlineData("阿玛拉")]
    [InlineData("می‌خواهم")]
    [InlineData("\U0001F469‍⚕️")]
    public void ScriptsPunctuationAndJoiners_AreAccepted(string text)
    {
        Assert.True(new PlainTextAttribute().IsValid(text));
    }

    [Theory]
    [MemberData(nameof(RequestsWithOneBadField))]
    public void EveryFreeTextFieldAndKey_IsChecked(object request, string field)
    {
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, result => result.MemberNames.Contains(field));
    }

    public static TheoryData<object, string> RequestsWithOneBadField()
    {
        const string hidden = "colle​ague";

        return new TheoryData<object, string>
        {
            { new CreateAttributeRequest { Key = "name", Value = hidden }, nameof(CreateAttributeRequest.Value) },
            { new CreateAttributeRequest { Key = "name", Value = "Amara", Label = hidden }, nameof(CreateAttributeRequest.Label) },
            { new CreateAttributeRequest { Key = "date of birth", Value = "Amara" }, nameof(CreateAttributeRequest.Key) },
            { new IssueStandingRequest { Email = "sam@example.com", Value = hidden }, nameof(IssueStandingRequest.Value) },
            { new CreateNormRequest { AttributeId = Guid.NewGuid(), Relationship = hidden, JustifyingPrinciple = "Because." }, nameof(CreateNormRequest.Relationship) },
            { new CreateNormRequest { AttributeId = Guid.NewGuid(), TransformParameter = hidden, JustifyingPrinciple = "Because." }, nameof(CreateNormRequest.TransformParameter) },
            { new CreateNormRequest { AttributeId = Guid.NewGuid(), JustifyingPrinciple = hidden }, nameof(CreateNormRequest.JustifyingPrinciple) },
            { new DisclosureRequestDto { SubjectEmail = "sam@example.com", AttributeKey = "<b>name</b>", Purpose = "Social" }, nameof(DisclosureRequestDto.AttributeKey) },
        };
    }
}
