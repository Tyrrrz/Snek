# Snek

[![Status](https://img.shields.io/badge/status-active-47c219.svg)](https://github.com/Tyrrrz/.github/blob/prime/docs/project-status.md)
[![Made in Ukraine](https://img.shields.io/badge/made_in-ukraine-ffd700.svg?labelColor=0057b7)](https://tyrrrz.me/ukraine)
[![Build](https://img.shields.io/github/actions/workflow/status/Tyrrrz/Snek/main.yml?branch=prime)](https://github.com/Tyrrrz/Snek/actions)
[![Version](https://img.shields.io/nuget/v/Snek.svg)](https://nuget.org/packages/Snek)
[![Downloads](https://img.shields.io/nuget/dt/Snek.svg)](https://nuget.org/packages/Snek)
[![Discord](https://img.shields.io/discord/869237470565392384?label=discord)](https://discord.gg/2SUWKFnHSm)
[![Fuck Russia](https://img.shields.io/badge/fuck-russia-e4181c.svg?labelColor=000000)](https://twitter.com/tyrrrz/status/1495972128977571848)

<table>
    <tr>
        <td width="99999" align="center">Development of this project is entirely funded by the community. <b><a href="https://tyrrrz.me/donate">Consider donating to support!</a></b></td>
    </tr>
</table>

<p align="center">
    <img src="favicon.png" alt="Icon" />
</p>

This is **Snek**, an MSBuild extension that automatically gives your assemblies a static strong name.
This may be useful for library developers who wish to sign their binaries solely for compatibility reasons and prefer to avoid dealing with Windows-only `sn.exe`.

## Terms of use<sup>[[?]](https://github.com/Tyrrrz/.github/blob/prime/docs/why-so-political.md)</sup>

By using this project or its source code, for any purpose and in any shape or form, you grant your **implicit agreement** to all the following statements:

- You **condemn Russia and its military aggression against Ukraine**
- You **recognize that Russia is an occupant that unlawfully invaded a sovereign state**
- You **support Ukraine's territorial integrity, including its claims over temporarily occupied territories of Crimea and Donbas**
- You **reject false narratives perpetuated by Russian state propaganda**

To learn more about the war and how you can help, [click here](https://tyrrrz.me/ukraine). Glory to Ukraine! 🇺🇦

## Install

- 📦 [NuGet](https://nuget.org/packages/Snek): `dotnet add package Snek`

## Usage

Simply install the **Snek** package as a private dependency in your project and it will automatically sign your assembly with a strong name key pair, deterministically derived from your package ID or assembly name (whichever is set):

```xml
<ItemGroup>
  <PackageReference Include="Snek" PrivateAssets="all" />
</ItemGroup>
```

You can then inspect the output assembly in **ILSpy** or a similar tool and see that it has a public key token:

```csharp
// Snek.Demo, Version=0.0.0.0, Culture=neutral, PublicKeyToken=af13e80e9703c96d
```

The public key token is derived from a seed, so it stays the same across rebuilds and machines, but differs between projects with different package IDs or assembly names.
You can also set the seed explicitly, to control the resulting public key token independently of the package ID or assembly name:

```xml
<PropertyGroup>
  <AssemblyOriginatorKeySeed>my-custom-seed</AssemblyOriginatorKeySeed>
</PropertyGroup>
```

## Etymology

**Snek** is a playful nickname for the `sn -k` command, which is used to generate a strong name key pair.
