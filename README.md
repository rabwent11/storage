# Commodity Storage 
[![Build Status](https://dev.azure.com/cmdty/github/_apis/build/status/cmdty.storage?branchName=master)](https://dev.azure.com/cmdty/github/_build/latest?definitionId=2&branchName=master)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/cmdty/github/2)
[![NuGet](https://img.shields.io/nuget/v/cmdty.storage.svg)](https://www.nuget.org/packages/Cmdty.Storage/)

Valuation and optimisation of commodity storage. Still in early stages of development.

## Overview
A collection of models for the valuation and optimisation of commodity storage, either virtual or physical. The models can be used for any commodity, although are most suitable for natural gas storage valuation and optimisation.

Calculations take into account many of the complex features of physical storage including:
* Inventory dependent injection and withdrawal rates, otherwise known as ratchets. For physical storage it is often the case that maximum withdrawal rates will increase, and injection rates will decrease as the storage inventory increases. For natural gas, this due to the increased pressure within the storage cavern.
* Time dependent injection and withdrawal rates, including the ability to add outages when no injection or withdrawal is allowed.
* Forced injection/withdrawal, as can be enforced by regulatory or physical constraints.
* Commodity consumed on injection/withdrawal, for example where natural gas is consumed by the motors that power injection into storage.
* Time dependent minimum and maximum inventory, necessary if different notional volumes of a storage facility are leased for different consecutive years.
* Ability to constrain the storage to be empty at the end of it's life, or specify a value of commodity inventory left in storage.

## Models
Currently the following models are implemented in this repository:
* Intrinsic valuation, i.e. optimal value assuming the commodity price remains static.
* One-factor trinomial tree, with seasonal spot volatility.

Both approaches solve the optimsation problem using backward induction across a discrete inventory grid.

## Getting Started

### Installing C# API
For use from C# install the NuGet package Cmdty.Storage.
```
PM> Install-Package Cmdty.Storage -Version 0.1.0-beta1
```

### Installing Excel Add-In
Models can be called in Excel using functions packaged into an add-in using [ExcelDna](https://github.com/Excel-DNA/ExcelDna).
1. Obtain the latest build add-in from the [Azure DevOps build](https://dev.azure.com/cmdty/github/_build?definitionId=2).
Within the latest build select, Artifacts > drop, then download either Cmdty.Storage-x86.xll (if you are using 32-bit Excel) or Cmdty.Storage-x64.xll (for 64-bit Excel).
2. Save the xll file in an appropriate location on your C: drive.
3. Add the add-in to you Excel. In most versions of Excel the proces is:
    * Open the Excel Options dialogue box.
    * Select the Add-ins tab.
    * Press the button labelled "Go..." which appears next to Manage Excel Add-ins.
    * In the dialogue box that opens, press the "Browse" button.
    * In the file selector dialogue box that opens, select the add-in xll file.
    * Press the OK button to close the two dialogue boxes.

Examples of the Excel functions can be found in [samples/excel/storage_samples.xlsx](https://github.com/cmdty/storage/raw/master/samples/excel/storage_samples.xlsx).

## Building
Build scripts use [cake](https://github.com/cake-build/cake) and require [the .NET Core SDK](https://dotnet.microsoft.com/download) to be installed on the Windows machine performing the build.

Run the following commands in a PowerShell console to clone and build the project:
```
> git clone https://github.com/cmdty/storage.git

> cd storage

> .\build.ps1 -Target Pack-NuGet

```
The result of this build will be saved into the artifacts directory:
* The NuGet package.
* 32-bit and 64-bit versions of the Excel add-in.


## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
