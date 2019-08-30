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
using System.Collections.Generic;
using Cmdty.Core.Trees;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;

namespace Cmdty.Storage
{
    public sealed class TreeStorageValuationResults<T>
        where T : ITimePeriod<T>
    {
        public double NetPresentValue { get; }
        public TimeSeries<T, IReadOnlyList<TreeNode>> Tree { get; }
        public TimeSeries<T, IReadOnlyList<Func<double, double>>> StorageNpvByInventory { get; }
        public TimeSeries<T, IReadOnlyList<double>> InventorySpaceGrids { get; }
        public TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>> StorageNpvs { get; }
        public TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>> InjectWithdrawDecisions { get; }

        public TreeStorageValuationResults(double netPresentValue, TimeSeries<T, IReadOnlyList<TreeNode>> tree,
                                TimeSeries<T, IReadOnlyList<Func<double, double>>> storageNpvByInventory,
                                TimeSeries<T, IReadOnlyList<double>> inventorySpaceGrids,
                                TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>> storageNpvs,
                                TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>> injectWithdrawDecisions)
        {
            NetPresentValue = netPresentValue;
            Tree = tree;
            StorageNpvByInventory = storageNpvByInventory;
            InventorySpaceGrids = inventorySpaceGrids;
            StorageNpvs = storageNpvs;
            InjectWithdrawDecisions = injectWithdrawDecisions;
        }

        // TODO ToString override
        // TODO Deconstruct method

        public static TreeStorageValuationResults<T> CreateExpiredResults()
        {
            return new TreeStorageValuationResults<T>(0.0, TimeSeries<T, IReadOnlyList<TreeNode>>.Empty, 
                TimeSeries<T, IReadOnlyList<Func<double, double>>>.Empty, TimeSeries<T, IReadOnlyList<double>>.Empty,
                TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>>.Empty, 
                TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>>.Empty);
        }

    }
}
