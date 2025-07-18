using System.Collections;
using System.Windows.Input;
using avallama.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace avallama.Controls;

/*
Template
 
<controls:ModelBlock
    Title='{Binding Name}'
    SizeText='{Binding Size}'
    DownloadStatus='{Binding DownloadStatus}'
    DownloadProgress='{Binding DownloadProgress}'
    DetailItemsSource='{Binding Details}'
    LabelItemsSource='{Binding Labels}'
    Command='{Binding ModelCommand}'
    CommandParameter='{Binding Index}'
    Background='{DynamicResource Primary}'
    Foreground='{DynamicResource OnPrimary}'
    LabelBackground='{DynamicResource Secondary}'
    LabelForeground='{DynamicResource OnSecondary}'/>
*/

public class ModelBlock : Control
{
    // AXAML Styled Propertyk
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ModelBlock, string>("Title");
    
    public static readonly StyledProperty<string> SizeTextProperty =
        AvaloniaProperty.Register<ModelBlock, string>("SizeText");
    
    public static readonly StyledProperty<ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.Register<ModelBlock, ModelDownloadStatus>("DownloadStatus");
    
    public static readonly StyledProperty<double?> DownloadProgressProperty =
        AvaloniaProperty.Register<ModelBlock, double?>("DownloadProgress");
    
    public static readonly StyledProperty<IBrush> BackgroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush>("Background");
    
    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush>("Foreground");
    
    public static readonly StyledProperty<IBrush> LabelBackgroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush>("LabelBackground");
    
    public static readonly StyledProperty<IBrush> LabelForegroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush>("LabelForeground");
    
    public static readonly StyledProperty<IEnumerable?> DetailItemsSourceProperty =
        AvaloniaProperty.Register<ModelBlock, IEnumerable?>("DetailItemsSource");
    
    public static readonly StyledProperty<IEnumerable?> LabelItemsSourceProperty =
        AvaloniaProperty.Register<ModelBlock, IEnumerable?>("LabelItemsSource");
    
    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelBlock, ICommand>("Command");
    
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ModelBlock, object?>("CommandParameter");

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string SizeText
    {
        get => GetValue(SizeTextProperty);
        set => SetValue(SizeTextProperty, value);
    }

    public ModelDownloadStatus DownloadStatus
    {
        get => GetValue(DownloadStatusProperty);
        set => SetValue(DownloadStatusProperty, value);
    }

    public double? DownloadProgress
    {
        get => GetValue(DownloadProgressProperty);
        set => SetValue(DownloadProgressProperty, value);
    }

    public IBrush Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }
    
    public IBrush LabelBackground
    {
        get => GetValue(LabelBackgroundProperty);
        set => SetValue(LabelBackgroundProperty, value);
    }

    public IBrush LabelForeground
    {
        get => GetValue(LabelForegroundProperty);
        set => SetValue(LabelForegroundProperty, value);
    }

    public IEnumerable? DetailItemsSource
    {
        get => GetValue(DetailItemsSourceProperty);
        set => SetValue(DetailItemsSourceProperty, value);
    }

    public IEnumerable? LabelItemsSource
    {
        get => GetValue(LabelItemsSourceProperty);
        set => SetValue(LabelItemsSourceProperty, value);
    }
    
    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
}