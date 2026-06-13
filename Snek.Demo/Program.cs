using System;
using System.Reflection;

var publicKeyToken = Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken();

Console.WriteLine(
    publicKeyToken is { Length: > 0 }
        ? $"Public key token: {Convert.ToHexStringLower(publicKeyToken)}"
        : "No public key token (assembly is not strong-named)."
);
