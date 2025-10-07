using System;
using System.Windows;
using ComicReader.Models;

namespace ComicReader.Views
{
    public class CollectionDialog
    {
        public string CollectionName { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string SelectedColor { get; private set; } = "#FF6B6B";
        public bool? DialogResult { get; private set; }

        private readonly string[] _colors = new[] 
        { 
            "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", 
            "#FFEAA7", "#DDA0DD", "#F39C12", "#E17055" 
        };

        public CollectionDialog(string title, ComicCollection? existingCollection = null)
        {
            ShowDialog(title, existingCollection);
        }

        private void ShowDialog(string title, ComicCollection? existingCollection)
        {
            try
            {
                // Crear ventana simple
                var window = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ResizeMode = ResizeMode.NoResize
                };

                var stack = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20)
                };

                // Campo nombre
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = "Nombre:" });
                var nameBox = new System.Windows.Controls.TextBox 
                { 
                    Text = existingCollection?.Name ?? "",
                    Margin = new Thickness(0, 5, 0, 15)
                };
                stack.Children.Add(nameBox);

                // Campo descripción
                stack.Children.Add(new System.Windows.Controls.TextBlock { Text = "Descripción:" });
                var descBox = new System.Windows.Controls.TextBox 
                { 
                    Text = existingCollection?.Description ?? "",
                    Margin = new Thickness(0, 5, 0, 15)
                };
                stack.Children.Add(descBox);

                // Botones
                var buttonPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var cancelBtn = new System.Windows.Controls.Button
                {
                    Content = "Cancelar",
                    Margin = new Thickness(0, 0, 10, 0),
                    Padding = new Thickness(15, 5, 15, 5),
                    MinWidth = 80
                };
                cancelBtn.Click += (s, e) => { window.DialogResult = false; window.Close(); };

                var okBtn = new System.Windows.Controls.Button
                {
                    Content = "Aceptar",
                    Padding = new Thickness(15, 5, 15, 5),
                    MinWidth = 80
                };
                okBtn.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        MessageBox.Show("El nombre no puede estar vacío.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    window.DialogResult = true;
                    window.Close();
                };

                buttonPanel.Children.Add(cancelBtn);
                buttonPanel.Children.Add(okBtn);
                stack.Children.Add(buttonPanel);

                window.Content = stack;
                
                var result = window.ShowDialog();
                
                if (result == true)
                {
                    CollectionName = nameBox.Text.Trim();
                    Description = descBox.Text.Trim();
                    SelectedColor = existingCollection?.Color ?? "#FF6B6B";
                    DialogResult = true;
                }
                else
                {
                    DialogResult = false;
                }
            }
            catch
            {
                DialogResult = false;
            }
        }

        public bool? ShowDialog()
        {
            return DialogResult;
        }
    }
}