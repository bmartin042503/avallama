// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public class Conversation : ObservableObject
{
    private string _title = string.Empty;
    private string _model = string.Empty;
    private int _messageCountToRegenerateTitle;
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

    public int MessageCountToRegenerateTitle
    {
        get => _messageCountToRegenerateTitle;
        set => SetProperty(ref _messageCountToRegenerateTitle, value);
    }
    
    public Conversation(string title, string model)
    {
        Title = title;
        Model = model;
        MessageCountToRegenerateTitle = 0;
    }

    public Conversation(string title, string model, IList<Message> messages)
    {
        Title = title;
        Model = model;
        Messages = new ObservableCollection<Message>(messages);
        MessageCountToRegenerateTitle = 0;
    }
    
    public void AddMessage(Message message) => Messages.Add(message);
}