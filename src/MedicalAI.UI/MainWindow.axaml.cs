using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using MedicalAI.UI.Views;

namespace MedicalAI.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (this.FindControl<NavigationView>("nav") is NavigationView nav)
            {
                // reserved
            }
        }
    }
}
