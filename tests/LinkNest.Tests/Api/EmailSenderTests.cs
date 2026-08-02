using LinkNest.Api.Identity;
using Microsoft.Extensions.Hosting;

namespace LinkNest.Tests.Api;

public class EmailConfigurationValidatorTests
{
    [Fact]
    public void GetConfigurationErrors_WithValidSettings_ReturnsEmpty()
    {
        var errors = EmailConfigurationValidator.GetConfigurationErrors(new SmtpOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            FromAddress = "noreply@example.com"
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void GetConfigurationErrors_WithMissingHost_ReturnsError()
    {
        var errors = EmailConfigurationValidator.GetConfigurationErrors(new SmtpOptions
        {
            Port = 587,
            FromAddress = "noreply@example.com"
        });

        Assert.Contains(errors, error => error.Contains("Host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetConfigurationErrors_WithMissingFromAddress_ReturnsError()
    {
        var errors = EmailConfigurationValidator.GetConfigurationErrors(new SmtpOptions
        {
            Host = "smtp.example.com",
            Port = 587
        });

        Assert.Contains(errors, error => error.Contains("FromAddress", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_InProductionWithMissingHost_Fails()
    {
        var validator = new EmailConfigurationValidator(new FakeHostEnvironment
        {
            EnvironmentName = Environments.Production
        });

        var result = validator.Validate(null, new EmailOptions
        {
            Smtp = new SmtpOptions { Port = 587, FromAddress = "noreply@example.com" }
        });

        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
    }

    [Fact]
    public void Validate_InTesting_SucceedsEvenWhenMisconfigured()
    {
        var validator = new EmailConfigurationValidator(new FakeHostEnvironment
        {
            EnvironmentName = "Testing"
        });

        var result = validator.Validate(null, new EmailOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_InDevelopmentWithUseSmtp_FailsWhenMisconfigured()
    {
        var validator = new EmailConfigurationValidator(new FakeHostEnvironment
        {
            EnvironmentName = Environments.Development
        });

        var result = validator.Validate(null, new EmailOptions { UseSmtp = true });

        Assert.False(result.Succeeded);
    }

    private sealed class FakeHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Test";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}

public class SmtpEmailSenderTests
{
    [Fact]
    public void BuildMessage_SetsFromToSubjectAndHtmlBody()
    {
        var message = SmtpEmailSender.BuildMessage(
            new SmtpOptions
            {
                FromAddress = "noreply@linknest.app",
                FromName = "LinkNest"
            },
            "user@example.com",
            "Test subject",
            "<p>Hello</p>");

        Assert.Equal("Test subject", message.Subject);
        Assert.Equal("user@example.com", message.To.Mailboxes.First().Address);
        Assert.Equal("noreply@linknest.app", message.From.Mailboxes.First().Address);
        Assert.Equal("LinkNest", message.From.Mailboxes.First().Name);
        Assert.NotNull(message.Body);
    }

    [Fact]
    public async Task SendEmailAsync_WithEmptyRecipient_Throws()
    {
        var sender = new SmtpEmailSender(
            Microsoft.Extensions.Options.Options.Create(new EmailOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SmtpEmailSender>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sender.SendEmailAsync(string.Empty, "Subject", "<p>Body</p>"));
    }
}
