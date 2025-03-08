using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public class Conversation : ObservableObject
{
    private string _title = string.Empty;
    private string _model = string.Empty;
    private ObservableCollection<Message> _messages = [];

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public ObservableCollection<Message> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }
    
    public Conversation(string title, string model)
    {
        Title = title;
        Model = model;
    }

    public Conversation(string title, string model, IList<Message> messages)
    {
        Title = title;
        Model = model;
        Messages = new ObservableCollection<Message>(messages);
    }
    
    public void AddMessage(Message message) => Messages.Add(message);
}