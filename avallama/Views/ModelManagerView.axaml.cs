// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Runtime.InteropServices;
using avallama.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace avallama.Views;

public partial class ModelManagerView : UserControl
{
    public ModelManagerView()
    {
        InitializeComponent();

        // needs to be pushed down on macOS because of the traffic light window buttons
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            PageTitle.Margin = new Thickness(0,10,0,20);
        }
    }
}
