// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using avallama.Constants.States;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public class Conversation : ObservableObject
{
    public Guid Id
    {
        get;
        set => SetProperty(ref field, value);
    } = Guid.Empty;

    public ConversationStatus Status
    {
        get;
        set => SetProperty(ref field, value);
    } = new (ConversationState.Initializing);

    public string Title
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Model
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public ObservableCollection<Message> Messages
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

    public Conversation(string title, string model)
    {
        Title = title;
        Model = model;
    }

    public Conversation(Guid guid, string title, IList<Message> messages)
    {
        Id = guid;
        Title = title;
        Messages = new ObservableCollection<Message>(messages);
    }

    public void AddMessage(Message message) => Messages.Add(message);
}
