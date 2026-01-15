// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public class Conversation : ObservableObject
{
    private string _title = string.Empty;
    private string _model = string.Empty;
    private ObservableCollection<Message> _messages = [];
    private Guid _conversationId = Guid.Empty;

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

    public Guid ConversationId
    {
        get => _conversationId;
        set => SetProperty(ref _conversationId, value);
    }

    public Conversation(string title, string model)
    {
        Title = title;
        Model = model;
    }

    public Conversation(Guid guid, string title, IList<Message> messages)
    {
        ConversationId = guid;
        Title = title;
        Messages = new ObservableCollection<Message>(messages);
    }

    public void AddMessage(Message message) => Messages.Add(message);
}
