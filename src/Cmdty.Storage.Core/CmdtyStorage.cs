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
using JetBrains.Annotations;

namespace Cmdty.Storage.Core
{
    /// <summary>
    /// Represents ownership of a commodity storage facility, either virtual or physical.
    /// </summary>
    public sealed class CmdtyStorage<T> // TODO extract interface?
        where T : ITimePeriod<T>
    {
        private readonly Func<T, IInjectWithdrawConstraint> _injectWithdrawConstraints;
        private readonly Func<T, double> _maxInventory;
        private readonly Func<T, double> _minInventory;
        private readonly Func<T, double, double, double, double> _injectionCost;
        private readonly Func<T, double, double, double, double> _withdrawalCost;
        private readonly Func<double, double, double> _terminalStorageValue;

        private CmdtyStorage(T startPeriod,
                            T endPeriod,
                            Func<T, IInjectWithdrawConstraint> injectWithdrawConstraints,
                            Func<T, double> maxInventory,
                            Func<T, double> minInventory,
                            Func<T, double, double, double, double> injectionCost,
                            Func<T, double, double, double, double> withdrawalCost,
                            Func<double, double, double> terminalStorageValue)
        {
            StartPeriod = startPeriod;
            EndPeriod = endPeriod;
            _injectWithdrawConstraints = injectWithdrawConstraints;
            _maxInventory = maxInventory;
            _minInventory = minInventory;
            _injectionCost = injectionCost;
            _withdrawalCost = withdrawalCost;
            _terminalStorageValue = terminalStorageValue;
        }

        // TODO checks that number isn't requested for date or inventory which is out of bounds?
        // TODO adjust inject and withdrawal rate doesn't make inventory in next period out of the inventory bounds?

        public T StartPeriod { get; }
        public T EndPeriod { get; }
        
        public InjectWithdrawRange GetInjectWithdrawRange(T date, double inventory)
        {
            return _injectWithdrawConstraints(date).GetInjectWithdrawRange(inventory);
        }
        
        public double MaxInventory(T date)
        {
            return _maxInventory(date);
        }

        public double MinInventory(T date)
        {
            return _minInventory(date);
        }

        public double InjectionCost(T date, double inventory, double injectedVolume, double cmdtyPrice)
        {
            return _injectionCost(date, inventory, injectedVolume, cmdtyPrice);
        }

        public double WithdrawalCost(T date, double inventory, double withdrawnVolume, double cmdtyPrice)
        {
            return _withdrawalCost(date, inventory, withdrawnVolume, cmdtyPrice);
        }

        public double InventorySpaceUpperBound([NotNull] T period, double nextPeriodInventorySpaceUpperBound)
        {
            if (period == null) throw new ArgumentNullException(nameof(period));
            IInjectWithdrawConstraint injectWithdrawConstraint = _injectWithdrawConstraints(period);
            double inventorySpaceUpper =
                injectWithdrawConstraint.InventorySpaceUpperBound(nextPeriodInventorySpaceUpperBound, MinInventory(period), MaxInventory(period));
            return inventorySpaceUpper;
        }

        public double InventorySpaceLowerBound([NotNull] T period, double nextPeriodInventorySpaceLowerBound)
        {
            if (period == null) throw new ArgumentNullException(nameof(period));
            IInjectWithdrawConstraint injectWithdrawConstraint = _injectWithdrawConstraints(period);
            double inventorySpaceLower =
                injectWithdrawConstraint.InventorySpaceLowerBound(nextPeriodInventorySpaceLowerBound, MinInventory(period), MaxInventory(period));
            return inventorySpaceLower;
        }

        public double TerminalStorageValue(double cmdtyPrice, double finalInventory)
        {
            return _terminalStorageValue(cmdtyPrice, finalInventory);
        }

        public static IBuilder<T> Builder => new StorageBuilder();

        private sealed class StorageBuilder : IBuilder<T>, IAddInjectWithdrawConstraints, IAddMaxInventory, IAddMinInventory, IAddInjectionCost, 
                    IAddWithdrawalCost, IAddStorageValueAtEnd
        {
            private T _startPeriod;
            private T _endPeriod;
            private Func<T, IInjectWithdrawConstraint> _injectWithdrawConstraints;
            private Func<T, double> _maxInventory;
            private Func<T, double> _minInventory;
            private Func<T, double, double, double, double> _injectionCost;
            private Func<T, double, double, double, double> _withdrawalCost;
            private Func<double, double, double> _terminalStorageValue;

            public StorageBuilder()
            {
            }

            IAddInjectWithdrawConstraints IBuilder<T>.WithActiveTimePeriod(T start, T end)
            {
                _startPeriod = start;
                _endPeriod = end;
                return this;
            }

            IAddMaxInventory IAddInjectWithdrawConstraints.WithTimeDependentInjectWithdrawRange(Func<T, InjectWithdrawRange> injectWithdrawRangeByPeriod)
            {
                if (injectWithdrawRangeByPeriod == null) throw new ArgumentNullException(nameof(injectWithdrawRangeByPeriod));
                _injectWithdrawConstraints = period => new ConstantInjectWithdrawConstraint(injectWithdrawRangeByPeriod(period));
                return this;
            }

            IAddMaxInventory IAddInjectWithdrawConstraints.WithInjectWithdrawConstraint(IInjectWithdrawConstraint injectWithdrawConstraint)
            {
                if (injectWithdrawConstraint == null) throw new ArgumentNullException(nameof(injectWithdrawConstraint));
                _injectWithdrawConstraints = date => injectWithdrawConstraint;
                return this;
            }

            IAddMaxInventory IAddInjectWithdrawConstraints.WithInjectWithdrawConstraint(Func<T, IInjectWithdrawConstraint> injectWithdrawConstraintByPeriod)
            {
                _injectWithdrawConstraints = injectWithdrawConstraintByPeriod ?? throw new ArgumentNullException(nameof(injectWithdrawConstraintByPeriod));
                return this;
            }

            IAddMinInventory IAddMaxInventory.WithConstantMaxInventory(double maxInventory)
            {
                // TODO Check not negative
                _maxInventory = date => maxInventory;
                return this;
            }
            
            IAddMinInventory IAddMaxInventory.WithMaxInventory(Func<T, double> maxInventory)
            {
                _maxInventory = maxInventory ?? throw new ArgumentNullException(nameof(maxInventory));
                return this;
            }

            IAddInjectionCost IAddMinInventory.WithZeroMinInventory()
            {
                _minInventory = date => 0.0;
                return this;
            }

            IAddInjectionCost IAddMinInventory.WithConstantMinInventory(double minInventory)
            {
                // TODO check not negative
                _minInventory = date => minInventory;
                return this;
            }
            
            IAddInjectionCost IAddMinInventory.WithMinInventory(Func<T, double> minInventory)
            {
                _minInventory = minInventory ?? throw new ArgumentNullException(nameof(minInventory));
                return this;
            }

            IAddWithdrawalCost IAddInjectionCost.WithPerUnitInjectionCost(double injectionCost)
            {
                // TODO check for non-negative
                _injectionCost = (date, inventory, injectedVolume, cmdtyPrice) => injectionCost * injectedVolume;
                return this;
            }

            IAddWithdrawalCost IAddInjectionCost.WithTimeDependentInjectionCost(Func<T, double> injectionCost)
            {
                if (injectionCost == null) throw new ArgumentNullException(nameof(injectionCost));
                _injectionCost = (date, inventory, injectedVolume, cmdtyPrice) => injectionCost(date);
                return this;
            }

            IAddWithdrawalCost IAddInjectionCost.WithInjectedVolumeDependentInjectionCost(Func<double, double> injectionCost)
            {
                if (injectionCost == null) throw new ArgumentNullException(nameof(injectionCost));
                _injectionCost = (date, inventory, injectedVolume, cmdtyPrice) => injectionCost(injectedVolume);
                return this;
            }

            IAddWithdrawalCost IAddInjectionCost.WithInventoryDependentInjectionCost(Func<double, double> injectionCost)
            {
                if (injectionCost == null) throw new ArgumentNullException(nameof(injectionCost));
                _injectionCost = (date, inventory, injectedVolume, cmdtyPrice) => injectionCost(inventory);
                return this;
            }

            IAddWithdrawalCost IAddInjectionCost.WithPriceDependentInjectionCost(Func<double, double> injectionCost)
            {
                if (injectionCost == null) throw new ArgumentNullException(nameof(injectionCost));
                _injectionCost = (date, inventory, injectedVolume, cmdtyPrice) => injectionCost(cmdtyPrice);
                return this;
            }

            IAddWithdrawalCost IAddInjectionCost.WithInjectionCost(Func<T, double, double, double, double> injectionCost)
            {
                _injectionCost = injectionCost ?? throw new ArgumentNullException(nameof(injectionCost));
                return this;
            }

            IAddStorageValueAtEnd IAddWithdrawalCost.WithPerUnitWithdrawalCost(double withdrawalCost)
            {
                // TODO check for non-negative
                _withdrawalCost = (date, inventory, withdrawnVolume, cmdtyPrice) => withdrawalCost * Math.Abs(withdrawnVolume);
                return this;
            }

            IAddStorageValueAtEnd IAddWithdrawalCost.WithTimeDependentWithdrawalCost(Func<T, double> withdrawalCost)
            {
                if (withdrawalCost == null) throw new ArgumentNullException(nameof(withdrawalCost));
                _withdrawalCost = (date, inventory, withdrawnVolume, cmdtyPrice) => withdrawalCost(date);
                return this;
            }

            IAddStorageValueAtEnd IAddWithdrawalCost.WithWithdrawnVolumeDependentWithdrawalCost(Func<double, double> withdrawalCost)
            {
                if (withdrawalCost == null) throw new ArgumentNullException(nameof(withdrawalCost));
                _withdrawalCost = (date, inventory, withdrawnVolume, cmdtyPrice) => withdrawalCost(withdrawnVolume);
                return this;
            }

            IAddStorageValueAtEnd IAddWithdrawalCost.WithInventoryDependentWithdrawalCost(Func<double, double> withdrawalCost)
            {
                if (withdrawalCost == null) throw new ArgumentNullException(nameof(withdrawalCost));
                _withdrawalCost = (date, inventory, withdrawnVolume, cmdtyPrice) => withdrawalCost(inventory);
                return this;
            }

            IAddStorageValueAtEnd IAddWithdrawalCost.WithPriceDependentWithdrawalCost(Func<double, double> withdrawalCost)
            {
                if (withdrawalCost == null) throw new ArgumentNullException(nameof(withdrawalCost));
                _withdrawalCost = (date, inventory, withdrawnVolume, cmdtyPrice) => withdrawalCost(cmdtyPrice);
                return this;
            }

            IAddStorageValueAtEnd IAddWithdrawalCost.WithWithdrawalCost(Func<T, double, double, double, double> withdrawalCost)
            {
                _withdrawalCost = withdrawalCost ?? throw new ArgumentNullException(nameof(withdrawalCost));
                return this;
            }
            
            IBuildCmdtyStorage IAddStorageValueAtEnd.WithTerminalStorageValue([NotNull] Func<double, double, double> terminalStorageValueFunc)
            {
                _terminalStorageValue = terminalStorageValueFunc ?? throw new ArgumentNullException(nameof(terminalStorageValueFunc));
                return this;
            }

            CmdtyStorage<T> IBuildCmdtyStorage.Build()
            {
                // TODO validate inputs
                // Default terminal storage value leaves gas in storage worthless
                Func<double, double, double> terminalStorageValue =_terminalStorageValue ?? ((cmdtyPrice, finalInventory) => 0.0);

                return new CmdtyStorage<T>(_startPeriod, _endPeriod, _injectWithdrawConstraints, _maxInventory, 
                            _minInventory, _injectionCost, _withdrawalCost, terminalStorageValue);
            }

        }

        public interface IAddInjectWithdrawConstraints
        {
            IAddMaxInventory WithTimeDependentInjectWithdrawRange(Func<T, InjectWithdrawRange> injectWithdrawRangeByPeriod);
            IAddMaxInventory WithInjectWithdrawConstraint(IInjectWithdrawConstraint injectWithdrawConstraint);
            IAddMaxInventory WithInjectWithdrawConstraint(Func<T, IInjectWithdrawConstraint> injectWithdrawConstraintByPeriod);
        }

        public interface IAddMaxInventory
        {
            IAddMinInventory WithConstantMaxInventory(double maxInventory);
            IAddMinInventory WithMaxInventory(Func<T, double> maxInventory);
        }

        public interface IAddMinInventory
        {
            IAddInjectionCost WithZeroMinInventory();
            IAddInjectionCost WithConstantMinInventory(double minInventory);
            IAddInjectionCost WithMinInventory(Func<T, double> minInventory);
        }

        public interface IAddInjectionCost
        {
            // TODO method for fixed cost component for action (no matter what volume)?
            IAddWithdrawalCost WithPerUnitInjectionCost(double injectionCost);
            // TODO method for cost as percentage of cmdty price
            IAddWithdrawalCost WithTimeDependentInjectionCost(Func<T, double> injectionCost);
            IAddWithdrawalCost WithInjectedVolumeDependentInjectionCost(Func<double, double> injectionCost);
            IAddWithdrawalCost WithInventoryDependentInjectionCost(Func<double, double> injectionCost);
            IAddWithdrawalCost WithPriceDependentInjectionCost(Func<double, double> injectionCost);
            // TODO add other combinations?
            IAddWithdrawalCost WithInjectionCost(Func<T, double, double, double, double> injectionCost);
        }

        public interface IAddWithdrawalCost
        {
            // TODO method for fixed cost component for action (no matter what volume)?
            IAddStorageValueAtEnd WithPerUnitWithdrawalCost(double withdrawalCost);
            // TODO method for cost as percentage of cmdty price
            IAddStorageValueAtEnd WithTimeDependentWithdrawalCost(Func<T, double> withdrawalCost);
            IAddStorageValueAtEnd WithWithdrawnVolumeDependentWithdrawalCost(Func<double, double> withdrawalCost);
            IAddStorageValueAtEnd WithInventoryDependentWithdrawalCost(Func<double, double> withdrawalCost);
            IAddStorageValueAtEnd WithPriceDependentWithdrawalCost(Func<double, double> withdrawalCost);
            // TODO add other combinations?
            IAddStorageValueAtEnd WithWithdrawalCost(Func<T, double, double, double, double> withdrawalCost);
        }

        public interface IAddStorageValueAtEnd : IBuildCmdtyStorage
        {
            IBuildCmdtyStorage WithTerminalStorageValue(Func<double, double, double> terminalStorageValueFunc);
        }

        public interface IBuildCmdtyStorage
        {
            CmdtyStorage<T> Build();
        }

    }
    public interface IBuilder<T>
        where T : ITimePeriod<T>
    {
        CmdtyStorage<T>.IAddInjectWithdrawConstraints WithActiveTimePeriod(T start, T end);
    }

}
