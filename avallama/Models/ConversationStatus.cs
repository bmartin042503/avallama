// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.States;
using avallama.DataTemplates;

namespace avallama.Models;

public class ConversationStatus(ConversationState conversationState, string? message = null) : IStatefulTemplateItem
{
    public ConversationState ConversationState { get; set; } = conversationState;
    public string? Message { get; set; } = message;

    public string GetCurrentStateKey() => ConversationState.ToString();
}
