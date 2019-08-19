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

using System;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using JetBrains.Annotations;

namespace Cmdty.Storage.Core
{
    public static class StorageHelper
    {

        public static TimeSeries<T, InventoryRange> CalculateInventorySpace<T>(CmdtyStorage<T> storage, double startingInventory, T currentPeriod)
            where T : ITimePeriod<T>
        {
            if (currentPeriod.CompareTo(storage.EndPeriod) > 0)
                throw new ArgumentException("Storage has expired");// TODO change to return empty TimeSeries?

            T startActiveStorage = storage.StartPeriod.CompareTo(currentPeriod) > 0 ? storage.StartPeriod : currentPeriod;

            int numPeriods = storage.EndPeriod.OffsetFrom(startActiveStorage);

            // Calculate the inventory space range going forward

            var forwardCalcMaxInventory = new double[numPeriods];
            var forwardCalcMinInventory = new double[numPeriods];

            double minInventoryForwardCalc = startingInventory;
            double maxInventoryForwardCalc = startingInventory;

            for (int i = 0; i < numPeriods; i++)
            {
                T periodLoop = startActiveStorage.Offset(i);
                T nextPeriod = periodLoop.Offset(1);

                double injectWithdrawMin = storage.GetInjectWithdrawRange(periodLoop, minInventoryForwardCalc).MinInjectWithdrawRate;
                double storageMin = storage.MinInventory(nextPeriod);
                minInventoryForwardCalc = Math.Max(minInventoryForwardCalc + injectWithdrawMin, storageMin);
                forwardCalcMinInventory[i] = minInventoryForwardCalc;

                double injectWithdrawMax = storage.GetInjectWithdrawRange(periodLoop, maxInventoryForwardCalc).MaxInjectWithdrawRate;
                double storageMax = storage.MaxInventory(nextPeriod);
                maxInventoryForwardCalc = Math.Min(maxInventoryForwardCalc + injectWithdrawMax, storageMax);
                forwardCalcMaxInventory[i] = maxInventoryForwardCalc;
            }

            // Calculate the inventory space range going backwards
            var backwardCalcMaxInventory = new double[numPeriods];

            var backwardCalcMinInventory = new double[numPeriods];

            T periodBackLoop = storage.EndPeriod;
            backwardCalcMaxInventory[numPeriods - 1] = storage.MustBeEmptyAtEnd ? 0 : storage.MaxInventory(storage.EndPeriod);
            backwardCalcMinInventory[numPeriods - 1] = storage.MustBeEmptyAtEnd ? 0 : storage.MinInventory(storage.EndPeriod);

            for (int i = numPeriods - 2; i >= 0; i--)
            {
                periodBackLoop = periodBackLoop.Offset(-1);
                backwardCalcMaxInventory[i] = storage.InventorySpaceUpperBound(periodBackLoop, backwardCalcMaxInventory[i + 1]);
                backwardCalcMinInventory[i] = storage.InventorySpaceLowerBound(periodBackLoop, backwardCalcMinInventory[i + 1]);
            }

            // Calculate overall inventory space and check for consistency

            var inventoryRanges = new InventoryRange[numPeriods];

            for (int i = 0; i < numPeriods; i++)
            {
                double inventorySpaceMax = Math.Min(forwardCalcMaxInventory[i], backwardCalcMaxInventory[i]);
                double inventorySpaceMin = Math.Max(forwardCalcMinInventory[i], backwardCalcMinInventory[i]);
                if (inventorySpaceMin > inventorySpaceMax)
                    throw new InventoryConstraintsCannotBeFulfilledException();
                inventoryRanges[i] = new InventoryRange(inventorySpaceMin, inventorySpaceMax);
            }

            return new TimeSeries<T, InventoryRange>(startActiveStorage.Offset(1), inventoryRanges);
        }

        public static double[] CalculateBangBangDecisionSet(InjectWithdrawRange injectWithdrawRange, double currentInventory,
                                        double nextStepMinInventory, double nextStepMaxInventory)
        {
            // TODO have tolerance for floating point numerical error?
            if (nextStepMinInventory > nextStepMaxInventory)
                throw new ArgumentException($"Parameter {nameof(nextStepMinInventory)} value cannot be higher than parameter {nameof(nextStepMaxInventory)} value");

            double inventoryAfterMaxInjection = injectWithdrawRange.MaxInjectWithdrawRate + currentInventory;
            if (inventoryAfterMaxInjection < nextStepMinInventory) // Max injection still below next step min inventory constraint
                throw new ArgumentException("Inventory constraints cannot be fulfilled"); // TODO better exception message

            double inventoryAfterMaxWithdrawal = injectWithdrawRange.MinInjectWithdrawRate + currentInventory;
            if (inventoryAfterMaxWithdrawal > nextStepMaxInventory) // Max withdrawal still above next step max inventory
                throw new ArgumentException("Inventory constraints cannot be fulfilled"); // TODO better exception message

            double yieldedWithdrawalRate;
            if (inventoryAfterMaxWithdrawal > nextStepMinInventory)
            {
                yieldedWithdrawalRate = injectWithdrawRange.MinInjectWithdrawRate; // Unconstrained withdrawal
            }
            else
            {
                yieldedWithdrawalRate = nextStepMinInventory - currentInventory; //constrained withdrawal (could be made positive to injection)
            }

            double yieldedInjectionRate;
            if (inventoryAfterMaxInjection < nextStepMaxInventory)
            {
                yieldedInjectionRate = injectWithdrawRange.MaxInjectWithdrawRate; // Unconstrained injection
            }
            else
            {
                yieldedInjectionRate = nextStepMaxInventory - currentInventory; // Constrained injection (could be made negative to withdrawal)
            }

            double[] decisionSet;
            if (yieldedWithdrawalRate >= 0.0 || yieldedInjectionRate <= 0.0) // No zero decision
            {
                decisionSet = new double[] {yieldedWithdrawalRate, yieldedInjectionRate};
            }
            else
            {
                decisionSet = new double[] { yieldedWithdrawalRate, 0.0, yieldedInjectionRate };
            }

            return decisionSet;

            // TODO case of yieldedWithdrawalRate equals to yieldedInjectionRate?
        }

        public static (double Max, int IndexOfMax) MaxValueAndIndex([NotNull] double[] array) // TODO move to Cmdty.Core?
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (array.Length == 0)
                throw new ArgumentException($"Parameter {nameof(array)} cannot have zero length", nameof(array));

            double max = array[0];
            int indexOfMax = 0;

            for (int i = 1; i < array.Length; i++)
            {
                double val = array[i];
                if (val > max)
                {
                    max = val;
                    indexOfMax = i;
                }
            }

            return (max, indexOfMax);
        }
    }
}
