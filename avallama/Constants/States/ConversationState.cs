// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Constants.States;

public enum ConversationState
{
    Initializing,
    Idle,
    ProcessingMessage,
    StreamingResponse,
    Failed
}
