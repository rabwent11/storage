#region License
// Copyright (c) 2019 Jake Fowler
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without 
// restriction, including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following 
// conditions:
//
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

namespace Cmdty.Storage.Excel
{
    internal static class ExcelArg
    {
        internal static class ValDate
        {
            public const string Name = "Val_date";
            public const string Description = "Current date assumed for valuation.";
        }

        internal static class StorageStart
        {
            public const string Name = "Storage_start";
            public const string Description = "Date-time on which storage facility first becomes active.";
        }

        internal static class StorageEnd
        {
            public const string Name = "Storage_end";
            public const string Description = "Date-time on which storage facility ceases being active. Injections/withdrawal are only allowed on time periods before the end.";
        }

        internal static class StorageConstraints
        {
            public const string Name = "Storage_constraints";
            public const string Description = "Table of time-dependent injection, withdrawal and inventory constraints. Range with 4 columns; date-time, inventory, injection rate and withdrawal rate. Withdrawal rates are expressed as a negative numbers.";
        }

        internal static class InjectWithdrawInterpolation
        {
            public const string Name = "Inject_withdraw_interpolation";
            public const string Description = "Text which determines how injection/withdrawal rates are interpolated by inventory. Must be either 'PiecewiseLinear' or 'Polynomial'.";
        }

        internal static class InjectionCost
        {
            public const string Name = "Inject_cost";
            public const string Description = "The cost of injecting commodity into the storage, for every unit of quantity injected.";
        }

        internal static class CmdtyConsumedInject
        {
            public const string Name = "Inject_cmdty_consume";
            public const string Description = "The quantity of commodity consumed upon injection, expressed as a percentage of quantity injected.";
        }

        internal static class WithdrawalCost
        {
            public const string Name = "Withdraw_cost";
            public const string Description = "The cost of withdrawing commodity out of the storage, for every unit of quantity withdrawn.";
        }

        internal static class CmdtyConsumedWithdraw
        {
            public const string Name = "Withdraw_cmdty_consume";
            public const string Description = "The quantity of commodity consumed upon withdrawal, expressed as a percentage of quantity withdrawn.";
        }
        
        internal static class Inventory
        {
            public const string Name = "Inventory";
            public const string Description = "The quantity of commodity currently being stored.";
        }

        internal static class ForwardCurve
        {
            public const string Name = "Forward_curve";
            public const string Description = "Forward, swap, or futures curve for the underlying stored commodity. Should consist of a two column range, with date-times in the first column (the delivery date), and numbers in the second column (the forward price).";
        }

        internal static class SpotVolCurve
        {
            public const string Name = "Spot_vol_curve";
            public const string Description = "Time-dependent volatility for one-factor spot price process. Should consist of a two column range, with date-times in the first column (the delivery date), and numbers in the second column (the spot vol).";
        }

        internal static class MeanReversion
        {
            public const string Name = "Mean_reversion";
            public const string Description = "Mean reversion rate of one-factor spot price process.";
        }

        internal static class InterestRateCurve
        {
            public const string Name = "Ir_curve";
            public const string Description = "Interest rate curve used to discount cash flows to present value, following Act/365 day count and continuous compounding. Any gaps in the curve are linearly interpolated.";
        }

        internal static class NumGridPoints
        {
            public const string Name = "[Num_grid_points]";
            public const string Description = "Optional parameter specifying the number of points in the inventory space grid used for backward induction. A higher value generally gives a more accurate valuation, but a longer running time. Defaults to 100 if omitted.";
        }

        internal static class NumericalTolerance
        {
            public const string Name = "[Numerical_tolerance]";
            public const string Description = "Optional parameter specifying the numerical tolerance. This should be small number that is used as a tolerance in numerical routines when comparing two floating point numbers. Defaults to 1E-10 if omitted.";
        }

    }
}
