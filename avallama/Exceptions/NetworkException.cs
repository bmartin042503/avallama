// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace avallama.Exceptions;

public abstract class NetworkException(string message, Exception? inner = null) : AvallamaException(message, inner);

public class NoInternetConnectionException(Exception? inner = null) : NetworkException("No internet connection.", inner);
public class LostInternetConnectionException(Exception? inner = null) : NetworkException("Internet connection lost.", inner);
