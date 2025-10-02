using Xunit;
using FluentAssertions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MedicalAI.UI;

namespace MedicalAI.UI.Tests
{
    public class SmokeTests
    {
        [Fact]
        public void App_Initializes()
        {
            var app = new App();
            app.Should().NotBeNull();
        }
    }
}
