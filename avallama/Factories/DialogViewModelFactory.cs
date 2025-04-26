// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Constants;
using avallama.ViewModels;

namespace avallama.Factories;

public class DialogViewModelFactory(Func<ApplicationDialog, DialogViewModel> factory)
{
    public DialogViewModel GetDialogViewModel(ApplicationDialog dialog) => factory.Invoke(dialog);
}