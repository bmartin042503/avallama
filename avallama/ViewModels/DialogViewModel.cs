// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.Application;

namespace avallama.ViewModels;

public class DialogViewModel : ViewModelBase
{
    public ApplicationDialog DialogType { get; set; }
}
