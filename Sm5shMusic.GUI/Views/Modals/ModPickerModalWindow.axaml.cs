﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Sm5shMusic.GUI.Views
{
    public class ModPickerModalWindow : Window
    {
        public ModPickerModalWindow()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
