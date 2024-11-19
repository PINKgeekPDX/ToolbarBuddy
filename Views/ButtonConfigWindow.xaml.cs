// ButtonConfigWindow.xaml.cs
using System;
using System.Windows;
using ToolBarApp.Models;

namespace ToolBarApp.Views
{
    /// <summary>
    /// Interaction logic for ButtonConfigWindow.xaml
    /// </summary>
    public partial class ButtonConfigWindow : Window
    {
        public ButtonConfig ButtonConfig { get; private set; }

        public ButtonConfigWindow(ButtonConfig buttonConfig)
        {
            InitializeComponent();
            ButtonConfig = buttonConfig;

            // Initialize fields with existing button configuration
            LabelTextBox.Text = ButtonConfig.Label;
            TypeComboBox.SelectedItem = GetComboBoxItem(ButtonConfig.Type);
            TooltipTextBox.Text = ButtonConfig.Tooltip;

            // TODO: Initialize additional fields based on Type
        }

        private ComboBoxItem GetComboBoxItem(string type)
        {
            foreach (ComboBoxItem item in TypeComboBox.Items)
            {
                if (item.Content.ToString().Equals(type, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate and save the configuration
            ButtonConfig.Label = LabelTextBox.Text;
            if (TypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                ButtonConfig.Type = selectedItem.Content.ToString().ToLower();
            }
            ButtonConfig.Tooltip = TooltipTextBox.Text;

            // TODO: Save additional configurations based on Type

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
