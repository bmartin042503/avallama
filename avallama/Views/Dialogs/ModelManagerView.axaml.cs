// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace avallama.Views.Dialogs;

public partial class ModelManagerView : UserControl
{

    private UniformGrid? _downloadedModelsGrid;
    private UniformGrid? _popularModelsGrid;
    
    public ModelManagerView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // lekérjük a két uniform gridet amik az itemscontrolokon belül vannak
        var downloadedModelsGrid = DownloadedModelsItemsControl.FindDescendantOfType<UniformGrid>();
        if (downloadedModelsGrid is not null)
            _downloadedModelsGrid = downloadedModelsGrid;

        var popularModelsGrid = PopularModelsItemsControl.FindDescendantOfType<UniformGrid>();
        if (popularModelsGrid is not null)
            _popularModelsGrid = popularModelsGrid;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        // reszponzív beállítás, hogy mennyi oszlopot jelenítsen meg mindkét uniformgrid a szélesség alapján
        if (_downloadedModelsGrid == null || _popularModelsGrid == null) return;
        switch (e.NewSize.Width)
        {
            case < 600:
                _downloadedModelsGrid.Columns = 1;
                _popularModelsGrid.Columns = 1;
                break;
            case < 950:
                _downloadedModelsGrid.Columns = 2;
                _popularModelsGrid.Columns = 2;
                break;
            case < 1400:
                _downloadedModelsGrid.Columns = 3;
                _popularModelsGrid.Columns = 3;
                break;
            default:
                _downloadedModelsGrid.Columns = 4;
                _popularModelsGrid.Columns = 4;
                break;
        }
    }
}