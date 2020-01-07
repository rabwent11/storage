# Commodity Storage 
[![Build Status](https://dev.azure.com/cmdty/github/_apis/build/status/cmdty.storage?branchName=master)](https://dev.azure.com/cmdty/github/_build/latest?definitionId=2&branchName=master)
![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/cmdty/github/2)
[![NuGet](https://img.shields.io/nuget/v/cmdty.storage.svg)](https://www.nuget.org/packages/Cmdty.Storage/)

Valuation and optimisation of commodity storage.

### Table of Contents
* [Overview](#overview)
* [Models](#models)
* [Getting Started](#getting-started)
    * [Installing C# API](#installing-c-api)
    * [Installing Excel Add-In](#installing-excel-add-in)
* [Using the C# API](#using-the-c-api)
    * [Creating the Storage Object](#creating-the-storage-object)
        * [Storage with Constant Parameters](#storage-with-constant-parameters)
        * [Storage with Time and Inventory Varying Inject/Withdraw Rates](#storage-with-time-and-inventory-varying-injectwithdraw-rates)
    * [Calculating the Intrinsic Value](#calculating-the-intrinsic-value)
* [Building](#building)
* [One Factor Trinomial Tree Model](#one-factor-trinomial-tree-method-critique-and-rationale)
* [License](#license)

## Overview
A collection of models for the valuation and optimisation of commodity storage, either virtual or physical. The models can be used for any commodity, although are most suitable for natural gas storage valuation and optimisation.

Calculations take into account many of the complex features of physical storage including:
* Inventory dependent injection and withdrawal rates, otherwise known as ratchets. For physical storage it is often the case that maximum withdrawal rates will increase, and injection rates will decrease as the storage inventory increases. For natural gas, this due to the increased pressure within the storage cavern.
* Time dependent injection and withdrawal rates, including the ability to add outages when no injection or withdrawal is allowed.
* Forced injection/withdrawal, as can be enforced by regulatory or physical constraints.
* Commodity consumed on injection/withdrawal, for example where natural gas is consumed by the motors that power injection into storage.
* Time dependent minimum and maximum inventory, necessary if different notional volumes of a storage facility are leased for different consecutive years.
* Optional time and inventory dependent loss of commodity in storage. For example this assumption is necessary for electricity storage which isn't 100% efficient.
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
PM> Install-Package Cmdty.Storage -Version 0.1.0-beta2
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

## Using the C# API

### Creating the Storage Object
In order for storage capacity to be valued, first an instance of the class CmdtyStorage 
needs to be created. The code samples below shows how the fluent builder API can be used
to achieve this. Once the Cmdty.Storage package has been installed,
a good way to discover the flexibility in the API is to look at the IntelliSense suggestions in
Visual Studio.

#### Storage with Constant Parameters
The code below shows simple storage facility with constant parameters.

``` c#
const double constantMaxInjectRate = 5.26;
const double constantMaxWithdrawRate = 14.74;
const double constantMaxInventory = 1100.74;
const double constantMinInventory = 0.0;
const double constantInjectionCost = 0.48;
const double constantWithdrawalCost = 0.74;

CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
    .WithActiveTimePeriod(new Day(2019, 9, 1), new Day(2019, 10, 1))
    .WithConstantInjectWithdrawRange(-constantMaxWithdrawRate, constantMaxInjectRate)
    .WithConstantMinInventory(constantMinInventory)
    .WithConstantMaxInventory(constantMaxInventory)
    .WithPerUnitInjectionCost(constantInjectionCost, injectionDate => injectionDate)
    .WithNoCmdtyConsumedOnInject()
    .WithPerUnitWithdrawalCost(constantWithdrawalCost, withdrawalDate => withdrawalDate)
    .WithNoCmdtyConsumedOnWithdraw()
    .WithNoCmdtyInventoryLoss()
    .WithNoCmdtyInventoryCost()
    .MustBeEmptyAtEnd()
    .Build();
```

#### Storage with Time and Inventory Varying Inject/Withdraw Rates
The code below shows how to create a more complicated storage object with injection/withdrawal 
rates being dependent on time and the inventory level.This is much more respresentative of real 
physical storage capacity.

``` c#
const double constantInjectionCost = 0.48;
const double constantWithdrawalCost = 0.74;

var injectWithdrawConstraints = new List<InjectWithdrawRangeByInventoryAndPeriod<Day>>
{
    (period: new Day(2019, 9, 1), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
    {
        (inventory: 0.0, (minInjectWithdrawRate: -44.85, maxInjectWithdrawRate: 56.8)), // Inventory empty, highest injection rate
        (inventory: 100.0, (minInjectWithdrawRate: -45.01, maxInjectWithdrawRate: 54.5)),
        (inventory: 300.0, (minInjectWithdrawRate: -45.78, maxInjectWithdrawRate: 52.01)),
        (inventory: 600.0, (minInjectWithdrawRate: -46.17, maxInjectWithdrawRate: 51.9)),
        (inventory: 800.0, (minInjectWithdrawRate: -46.99, maxInjectWithdrawRate: 50.8)),
        (inventory: 1000.0, (minInjectWithdrawRate: -47.12, maxInjectWithdrawRate: 50.01)) // Inventory full, highest withdrawal rate
    }),
    (period: new Day(2019, 9, 20), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
    {
        (inventory: 0.0, (minInjectWithdrawRate: -31.41, maxInjectWithdrawRate: 48.33)), // Inventory empty, highest injection rate
        (inventory: 100.0, (minInjectWithdrawRate: -31.85, maxInjectWithdrawRate: 43.05)),
        (inventory: 300.0, (minInjectWithdrawRate: -31.68, maxInjectWithdrawRate: 41.22)),
        (inventory: 600.0, (minInjectWithdrawRate: -32.78, maxInjectWithdrawRate: 40.08)),
        (inventory: 800.0, (minInjectWithdrawRate: -33.05, maxInjectWithdrawRate: 39.74)),
        (inventory: 1000.0, (minInjectWithdrawRate: -34.80, maxInjectWithdrawRate: 38.51)) // Inventory full, highest withdrawal rate
    })
};

CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
    .WithActiveTimePeriod(new Day(2019, 9, 1), new Day(2019, 10, 1))
    .WithTimeAndInventoryVaryingInjectWithdrawRates(injectWithdrawConstraints)
    .WithPerUnitInjectionCost(constantInjectionCost, injectionDate => injectionDate)
    .WithNoCmdtyConsumedOnInject()
    .WithPerUnitWithdrawalCost(constantWithdrawalCost, withdrawalDate => withdrawalDate)
    .WithNoCmdtyConsumedOnWithdraw()
    .WithNoCmdtyInventoryLoss()
    .WithNoCmdtyInventoryCost()
    .MustBeEmptyAtEnd()
    .Build();
```

### Calculating the Intrinsic Value
The following example shows how to calculate the intrinsic value of the storage, including
the optimal intrinsic inject/withdraw decision profile.

``` c#
var currentPeriod = new Day(2019, 9, 15);
const double lowerForwardPrice = 56.6;
const double forwardSpread = 87.81;
double higherForwardPrice = lowerForwardPrice + forwardSpread;

var forwardCurveBuilder = new TimeSeries<Day, double>.Builder();

foreach (var day in new Day(2019, 9, 15).EnumerateTo(new Day(2019, 9, 22)))
{
    forwardCurveBuilder.Add(day, lowerForwardPrice);
}

foreach (var day in new Day(2019, 9, 23).EnumerateTo(new Day(2019, 10, 1)))
{
    forwardCurveBuilder.Add(day, higherForwardPrice);
}

const double startingInventory = 50.0;

IntrinsicStorageValuationResults<Day> valuationResults = IntrinsicStorageValuation<Day>
    .ForStorage(storage)
    .WithStartingInventory(startingInventory)
    .ForCurrentPeriod(currentPeriod)
    .WithForwardCurve(forwardCurveBuilder.Build())
    .WithCmdtySettlementRule(day => day.First<Month>().Offset(1).First<Day>().Offset(5))
    .WithDiscountFactorFunc(day => 1.0)
    .WithFixedGridSpacing(10.0)
    .WithLinearInventorySpaceInterpolation()
    .WithNumericalTolerance(1E-12)
    .Calculate();

Console.WriteLine("Calculated intrinsic storage NPV: " + valuationResults.NetPresentValue.ToString("F2"));
Console.WriteLine();
Console.WriteLine("Decision profile:");
Console.WriteLine(valuationResults.DecisionProfile.FormatData("F2", -1));
```

When run, the above code prints the following to the console.

```
Calculated intrinsic storage NPV: 10827.21

Decision profile:
Count = 16
2019-09-15  5.26
2019-09-16  5.26
2019-09-17  5.26
2019-09-18  5.26
2019-09-19  5.26
2019-09-20  5.26
2019-09-21  5.26
2019-09-22  5.26
2019-09-23  -14.74
2019-09-24  -14.74
2019-09-25  0.00
2019-09-26  -14.74
2019-09-27  -14.74
2019-09-28  -14.74
2019-09-29  -14.74
2019-09-30  -3.64
```

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

## One-Factor Trinomial Tree Method: Critique and Rationale
Currently this library only contains one model to calculate the extrinsic value of storage, the one-factor trinomial tree model. However, the author is aware thof the many shortcomings of this approach such as:
* Modeling commodity price dynamics using a one-factor process does not imply volatilities and correlations that are particularly realistic. For example the one-factor process implies a correlation of 1 between all points on the forward curve. This is of particular concern for a product like storage, whose extrinsic value is derived from the relative movement of different parts of the forward curve.
* Even if we accept a one-factor price process, using a trinomial tree is equivalent to an explicit finite difference method. This offers inferior stability compared to an implicit finite difference scheme.

If the one-factor trinomial tree approach has so many shortcomings then why implement it? One reason is that the development of more sophisticated models, such as least-squares Monte Carlo against a multi-factor price process, will be made much easier by the presence of an existing trinomial tree model. Reasons for this include:
* The results from implementing a multi-factor model configured to have the same dynamics as a single-factor model (for example by setting all but one factor volatilities to zero) should be equal to those generated by the one-factor trinomial tree model. This will greatly help testing.
* Many classes and methods developed for the trinomial tree storage model can be reused for more sophisticated models. For example the class which is used to represent the actual storage facility can be reused without any modification.


## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
