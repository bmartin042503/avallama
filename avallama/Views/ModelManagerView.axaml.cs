// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace avallama.Views;

public partial class ModelManagerView : UserControl
{
    public ModelManagerView()
    {
        InitializeComponent();
        
        // traffic light window gombok miatt lentebb kell tolni macen
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            PageTitle.Margin = new Thickness(0,10,0,20);
        }
    }
}