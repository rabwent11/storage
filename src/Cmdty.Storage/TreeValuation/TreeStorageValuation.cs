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
using System.Linq;
using Cmdty.Core.Trees;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using JetBrains.Annotations;

namespace Cmdty.Storage
{
    public sealed class TreeStorageValuation<T> : ITreeAddStartingInventory<T>, ITreeAddCurrentPeriod<T>, ITreeAddForwardCurve<T>,
        ITreeAddTreeFactory<T>, ITreeAddCmdtySettlementRule<T>, ITreeAddDiscountFactorFunc<T>, 
            ITreeAddInventoryGridCalculation<T>, ITreeAddInterpolator<T>, ITreeAddNumericalTolerance<T>, ITreeCalculate<T>
        where T : ITimePeriod<T>
    {
        private readonly ICmdtyStorage<T> _storage;
        private double _startingInventory;
        private T _currentPeriod;
        private TimeSeries<T, double> _forwardCurve;
        private Func<TimeSeries<T, double>, TimeSeries<T, IReadOnlyList<TreeNode>>> _treeFactory;
        private Func<T, Day> _settleDateRule;
        private Func<Day, Day, double> _discountFactors;
        private Func<ICmdtyStorage<T>, IDoubleStateSpaceGridCalc> _gridCalcFactory;
        private IInterpolatorFactory _interpolatorFactory;
        private double _numericalTolerance;

        private TreeStorageValuation([NotNull] ICmdtyStorage<T> storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public static ITreeAddStartingInventory<T> ForStorage([NotNull] ICmdtyStorage<T> storage)
        {
            return new TreeStorageValuation<T>(storage);
        }

        ITreeAddCurrentPeriod<T> ITreeAddStartingInventory<T>.WithStartingInventory(double inventory)
        {
            if (inventory < 0)
                throw new ArgumentException("Inventory cannot be negative", nameof(inventory));
            _startingInventory = inventory;
            return this;
        }

        ITreeAddForwardCurve<T> ITreeAddCurrentPeriod<T>.ForCurrentPeriod([NotNull] T currentPeriod)
        {
            if (currentPeriod == null)
                throw new ArgumentNullException(nameof(currentPeriod));
            _currentPeriod = currentPeriod;
            return this;
        }

        ITreeAddTreeFactory<T> ITreeAddForwardCurve<T>.WithForwardCurve([NotNull] TimeSeries<T, double> forwardCurve)
        {
            _forwardCurve = forwardCurve ?? throw new ArgumentNullException(nameof(forwardCurve));
            return this;
        }

        ITreeAddCmdtySettlementRule<T> ITreeAddTreeFactory<T>.WithTreeFactory(
            [NotNull] Func<TimeSeries<T, double>, TimeSeries<T, IReadOnlyList<TreeNode>>> treeFactory)
        {
            _treeFactory = treeFactory ?? throw new ArgumentNullException(nameof(treeFactory));
            return this;
        }

        public ITreeAddDiscountFactorFunc<T> WithCmdtySettlementRule([NotNull] Func<T, Day> settleDateRule)
        {
            _settleDateRule = settleDateRule ?? throw new ArgumentNullException(nameof(settleDateRule));
            return this;
        }

        ITreeAddInventoryGridCalculation<T> ITreeAddDiscountFactorFunc<T>.WithDiscountFactorFunc([NotNull] Func<Day, Day, double> discountFactors)
        {
            _discountFactors = discountFactors ?? throw new ArgumentNullException(nameof(discountFactors));
            return this;
        }

        ITreeAddInterpolator<T> ITreeAddInventoryGridCalculation<T>.WithStateSpaceGridCalculation(
            Func<ICmdtyStorage<T>, IDoubleStateSpaceGridCalc> gridCalcFactory)
        {
            _gridCalcFactory = gridCalcFactory ?? throw new ArgumentNullException(nameof(gridCalcFactory));
            return this;
        }

        ITreeAddNumericalTolerance<T> ITreeAddInterpolator<T>.WithInterpolatorFactory([NotNull] IInterpolatorFactory interpolatorFactory)
        {
            _interpolatorFactory = interpolatorFactory ?? throw new ArgumentNullException(nameof(interpolatorFactory));
            return this;
        }

        ITreeCalculate<T> ITreeAddNumericalTolerance<T>.WithNumericalTolerance(double numericalTolerance)
        {
            if (numericalTolerance <= 0)
                throw new ArgumentException("Numerical tolerance must be positive.", nameof(numericalTolerance));
            _numericalTolerance = numericalTolerance;
            return this;
        }

        TreeStorageValuationResults<T> ITreeCalculate<T>.Calculate()
        {
            return Calculate(_currentPeriod, _startingInventory, _forwardCurve, _treeFactory, _storage,
                _settleDateRule, _discountFactors, _gridCalcFactory,
                    _interpolatorFactory, _numericalTolerance);
        }

        (TreeStorageValuationResults<T> ValuationResults, ITreeDecisionSimulator<T> DecisionSimulator) 
                    ITreeCalculate<T>.CalculateWithDecisionSimulator()
        {
            var valuationResults = (this as ITreeCalculate<T>).Calculate();
            return (valuationResults, new DecisionSimulator(valuationResults, this));
        }

        double ITreeCalculate<T>.CalculateNpv()
        {
            return (this as ITreeCalculate<T>).Calculate().NetPresentValue;
        }

        private static TreeStorageValuationResults<T> Calculate(T currentPeriod, double startingInventory, 
            TimeSeries<T, double> forwardCurve, Func<TimeSeries<T, double>, TimeSeries<T, IReadOnlyList<TreeNode>>> treeFactory, 
            ICmdtyStorage<T> storage, Func<T, Day> settleDateRule, Func<Day, Day, double> discountFactors, 
            Func<ICmdtyStorage<T>, IDoubleStateSpaceGridCalc> gridCalcFactory, IInterpolatorFactory interpolatorFactory, 
            double numericalTolerance)
        {
            if (startingInventory < 0)
                throw new ArgumentException("Inventory cannot be negative.", nameof(startingInventory));

            if (currentPeriod.CompareTo(storage.EndPeriod) > 0)
                return TreeStorageValuationResults<T>.CreateExpiredResults();

            if (currentPeriod.Equals(storage.EndPeriod))
            {
                if (storage.MustBeEmptyAtEnd)
                {
                    if (startingInventory > 0)
                        throw new InventoryConstraintsCannotBeFulfilledException("Storage must be empty at end, but inventory is greater than zero.");
                    return TreeStorageValuationResults<T>.CreateExpiredResults();
                }
            }

            TimeSeries<T, InventoryRange> inventorySpace = StorageHelper.CalculateInventorySpace(storage, startingInventory, currentPeriod);

            // TODO think of method to put in TimeSeries class to perform the validation check below in one line
            if (forwardCurve.IsEmpty)
                throw new ArgumentException("Forward curve cannot be empty.", nameof(forwardCurve));

            if (forwardCurve.Start.CompareTo(currentPeriod) > 0)
                throw new ArgumentException("Forward curve starts too late. Must start on or before the current period.", nameof(forwardCurve));

            if (forwardCurve.End.CompareTo(inventorySpace.End) < 0)
                throw new ArgumentException("Forward curve does not extend until storage end period.", nameof(forwardCurve));

            // Perform backward induction
            int numPeriods = inventorySpace.Count + 1; // +1 as inventorySpaceGrid doesn't contain first period
            var storageValueByInventory = new Func<double, double>[numPeriods][];
            var inventorySpaceGrids = new double[numPeriods][];
            var storageNpvs = new double[numPeriods][][];
            var injectWithdrawDecisions = new double[numPeriods][][];

            TimeSeries<T, IReadOnlyList<TreeNode>> spotPriceTree = treeFactory(forwardCurve);

            // Calculate NPVs at end period
            IReadOnlyList<TreeNode> treeNodesForEndPeriod = spotPriceTree[storage.EndPeriod];

            storageValueByInventory[numPeriods - 1] = 
                                    new Func<double, double>[treeNodesForEndPeriod.Count];

            for (int i = 0; i < treeNodesForEndPeriod.Count; i++)
            {
                double cmdtyPrice = treeNodesForEndPeriod[i].Value;
                storageValueByInventory[numPeriods - 1][i] = inventory => storage.TerminalStorageNpv(cmdtyPrice, inventory);
            }

            // Calculate discount factor function
            Day dayToDiscountTo = currentPeriod.First<Day>(); // TODO IMPORTANT, this needs to change

            // Memoize the discount factor
            var discountFactorCache = new Dictionary<Day, double>(); // TODO do this in more elegant way and share with intrinsic calc
            double DiscountToCurrentDay(Day cashFlowDate)
            {
                if (!discountFactorCache.TryGetValue(cashFlowDate, out double discountFactor))
                {
                    discountFactor = discountFactors(dayToDiscountTo, cashFlowDate);
                    discountFactorCache[cashFlowDate] = discountFactor;
                }
                return discountFactor;
            }

            // Loop back through other periods
            T startActiveStorage = inventorySpace.Start.Offset(-1);
            T[] periodsForResultsTimeSeries = startActiveStorage.EnumerateTo(inventorySpace.End).ToArray();

            int backCounter = numPeriods - 2;
            IDoubleStateSpaceGridCalc gridCalc = gridCalcFactory(storage);

            foreach (T periodLoop in periodsForResultsTimeSeries.Reverse().Skip(1))
            {
                double[] inventorySpaceGrid;
                if (periodLoop.Equals(startActiveStorage))
                {
                    inventorySpaceGrid = new[] {startingInventory};
                }
                else
                {
                    (double inventorySpaceMin, double inventorySpaceMax) = inventorySpace[periodLoop];
                    inventorySpaceGrid = gridCalc.GetGridPoints(inventorySpaceMin, inventorySpaceMax)
                                                    .ToArray();
                }

                (double nextStepInventorySpaceMin, double nextStepInventorySpaceMax) = inventorySpace[periodLoop.Offset(1)];

                Func<double, double>[] continuationValueByInventory = storageValueByInventory[backCounter + 1];

                IReadOnlyList<TreeNode> thisStepTreeNodes = spotPriceTree[periodLoop];
                storageValueByInventory[backCounter] = new Func<double, double>[thisStepTreeNodes.Count];
                var storageNpvsByPriceLevelAndInventory = new double[thisStepTreeNodes.Count][];
                var decisionVolumesByPriceLevelAndInventory = new double[thisStepTreeNodes.Count][];

                Day cmdtySettlementDate = settleDateRule(periodLoop);
                double discountFactorFromCmdtySettlement = DiscountToCurrentDay(cmdtySettlementDate);
                
                for (var priceLevelIndex = 0; priceLevelIndex < thisStepTreeNodes.Count; priceLevelIndex++)
                {
                    TreeNode treeNode = thisStepTreeNodes[priceLevelIndex];
                    var storageValuesGrid = new double[inventorySpaceGrid.Length];
                    var decisionVolumesGrid = new double[inventorySpaceGrid.Length];
                    
                    for (int i = 0; i < inventorySpaceGrid.Length; i++)
                    {
                        double inventory = inventorySpaceGrid[i];
                        (storageValuesGrid[i], decisionVolumesGrid[i], _, _) = 
                                        OptimalDecisionAndValue(storage, periodLoop, inventory,
                                        nextStepInventorySpaceMin, nextStepInventorySpaceMax, treeNode,
                                        continuationValueByInventory, discountFactorFromCmdtySettlement, DiscountToCurrentDay, numericalTolerance);
                    }

                    storageValueByInventory[backCounter][priceLevelIndex] =
                        interpolatorFactory.CreateInterpolator(inventorySpaceGrid, storageValuesGrid);
                    storageNpvsByPriceLevelAndInventory[priceLevelIndex] = storageValuesGrid;
                    decisionVolumesByPriceLevelAndInventory[priceLevelIndex] = decisionVolumesGrid;
                }
                inventorySpaceGrids[backCounter] = inventorySpaceGrid;
                storageNpvs[backCounter] = storageNpvsByPriceLevelAndInventory;
                injectWithdrawDecisions[backCounter] = decisionVolumesByPriceLevelAndInventory;
                backCounter--;
            }

            // Calculate NPVs for first active period using current inventory
            double storageNpv = 0;
            IReadOnlyList<TreeNode> startTreeNodes = spotPriceTree[startActiveStorage];

            for (var i = 0; i < startTreeNodes.Count; i++)
            {
                TreeNode treeNode = startTreeNodes[i];
                storageNpv += storageNpvs[0][i][0] * treeNode.Probability;
            }

            var storageNpvByInventory =
                new TimeSeries<T, IReadOnlyList<Func<double, double>>>(periodsForResultsTimeSeries, storageValueByInventory);
            var inventorySpaceGridsTimeSeries =
                new TimeSeries<T, IReadOnlyList<double>>(periodsForResultsTimeSeries, inventorySpaceGrids);
            var storageNpvsTimeSeries =
                new TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>>(periodsForResultsTimeSeries, storageNpvs);
            var injectWithdrawDecisionsTimeSeries =
                new TimeSeries<T, IReadOnlyList<IReadOnlyList<double>>>(periodsForResultsTimeSeries, injectWithdrawDecisions);

            return new TreeStorageValuationResults<T>(storageNpv, spotPriceTree, storageNpvByInventory, 
                            inventorySpaceGridsTimeSeries, storageNpvsTimeSeries, injectWithdrawDecisionsTimeSeries,
                            inventorySpace);
        }

        // TODO create class on hold this tuple?
        private static (double StorageNpv, double OptimalInjectWithdraw, double CmdtyConsumedOnAction, double ImmediateNpv) 
            OptimalDecisionAndValue(ICmdtyStorage<T> storage, T period, double inventory,
                    double nextStepInventorySpaceMin, double nextStepInventorySpaceMax, TreeNode treeNode,
                    IReadOnlyList<Func<double, double>> continuationValueByInventories, double discountFactorFromCmdtySettlement, 
                    Func<Day, double> discountFactors, double numericalTolerance)
        {
            InjectWithdrawRange injectWithdrawRange = storage.GetInjectWithdrawRange(period, inventory);
            double inventoryLoss = storage.CmdtyInventoryPercentLoss(period) * inventory;
            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, inventory, inventoryLoss,
                                            nextStepInventorySpaceMin, nextStepInventorySpaceMax, numericalTolerance);

            var valuesForDecisions = new double[decisionSet.Length];
            var cmdtyConsumedForDecisions = new double[decisionSet.Length];
            var immediateNpvs = new double[decisionSet.Length];

            IReadOnlyList<DomesticCashFlow> inventoryCostCashFlows = storage.CmdtyInventoryCost(period, inventory);
            double inventoryCostNpv = inventoryCostCashFlows.Sum(cashFlow => cashFlow.Amount * discountFactors(cashFlow.Date));

            for (var j = 0; j < decisionSet.Length; j++)
            {
                double decisionInjectWithdraw = decisionSet[j];
                (double immediateNpv, double cmdtyConsumed) = StorageHelper.StorageImmediateNpvForDecision(storage, period, inventory,
                                                decisionInjectWithdraw, treeNode.Value, discountFactorFromCmdtySettlement, discountFactors);
                immediateNpv -= inventoryCostNpv;
                // Expected continuation value
                double inventoryAfterDecision = inventory + decisionInjectWithdraw - inventoryLoss;
                double expectedContinuationValue = 0.0;

                foreach (NodeTransition transition in treeNode.Transitions)
                {
                    int indexOfNextNode = transition.DestinationNode.ValueLevelIndex;
                    double continuationValue = continuationValueByInventories[indexOfNextNode](inventoryAfterDecision);
                    expectedContinuationValue += continuationValue * transition.Probability;
                }

                valuesForDecisions[j] = immediateNpv + expectedContinuationValue;
                cmdtyConsumedForDecisions[j] = cmdtyConsumed;
                immediateNpvs[j] = immediateNpv;
            }

            (double storageNpv, int indexOfOptimalDecision) = StorageHelper.MaxValueAndIndex(valuesForDecisions);

            return (StorageNpv: storageNpv, OptimalInjectWithdraw: decisionSet[indexOfOptimalDecision], 
                    CmdtyConsumedOnAction: cmdtyConsumedForDecisions[indexOfOptimalDecision],
                    ImmediateNpv: immediateNpvs[indexOfOptimalDecision]);
        }

        private TreeSimulationResults<T> SimulateDecisions(TreeStorageValuationResults<T> valuationResults, 
                                                TimeSeries<T, int> spotPricePath)
        {
            double inventory = valuationResults.InventorySpaceGrids[0][0];

            TimeSeries<T, IReadOnlyList<TreeNode>> tree = valuationResults.Tree;
            // TODO put method on TimeSeries class which gets rid of this 2-step validation
            if (spotPricePath.IsEmpty)
                throw new ArgumentException("spotPricePath cannot be an empty Time Series.", nameof(spotPricePath));

            if (!spotPricePath.Start.Equals(tree.Start))
                throw new ArgumentException($"spotPricePath must start on {tree.Start}, the start period of the tree used for valuation.", nameof(spotPricePath));

            if (spotPricePath.End.OffsetFrom(tree.End) < -1)
                throw new ArgumentException($"spotPricePath cannot end earlier than 1 period before {tree.End}, the end period for the tree used for valuation.", nameof(spotPricePath));

            // Calculate discount factor function
            Day dayToDiscountTo = spotPricePath.Start.First<Day>(); // TODO IMPORTANT, this needs to change
            double DiscountToCurrentDay(Day day) => _discountFactors(dayToDiscountTo, day);

            TreeNode treeNode = tree[0][0];
            var decisions = new double[valuationResults.StorageNpvByInventory.Count - 1]; // -1 because StorageNpvByInventory included the end period on which a decision can't be made
            var cmdtyVolumeConsumedArray = new double[valuationResults.StorageNpvByInventory.Count - 1];

            int i = 0;
            double storageNpv = 0.0;
            foreach (T period in tree.Indices.Take(tree.Count - 1))
            {
                if (period.CompareTo(_storage.StartPeriod) >= 0)
                {
                    if (period.Equals(_storage.EndPeriod))
                    {
                        Func<double, double> storageNpvByInventory =
                                        valuationResults.StorageNpvByInventory[period][treeNode.ValueLevelIndex];
                        storageNpv += storageNpvByInventory(inventory);
                    }
                    else
                    {
                        Day cmdtySettlementDate = _settleDateRule(period);
                        double discountFactorFromCmdtySettlement = DiscountToCurrentDay(cmdtySettlementDate);

                        T nextPeriod = period.Offset(1);
                        IReadOnlyList<Func<double, double>> continuationValueByInventory =
                            valuationResults.StorageNpvByInventory[nextPeriod];
                        (double nextStepInventorySpaceMin, double nextStepInventorySpaceMax) =
                            valuationResults.InventorySpace[nextPeriod];

                        double thisStepImmediateNpv;
                        (_, decisions[i], cmdtyVolumeConsumedArray[i], thisStepImmediateNpv) =
                            OptimalDecisionAndValue(_storage, period, inventory, nextStepInventorySpaceMin,
                                nextStepInventorySpaceMax, treeNode, continuationValueByInventory, discountFactorFromCmdtySettlement,
                                DiscountToCurrentDay, _numericalTolerance);

                        double inventoryLoss = _storage.CmdtyInventoryPercentLoss(period) * inventory;

                        storageNpv += thisStepImmediateNpv;
                        inventory += decisions[i] - inventoryLoss;
                        i++;
                    }
                }

                int transitionIndex = spotPricePath[period];
                treeNode = treeNode.Transitions[transitionIndex].DestinationNode;
            }

            // TODO once val results decision is trimmed at end, use this for results indices
            var indicesForResults = valuationResults.StorageNpvByInventory.Indices.Take(valuationResults.StorageNpvByInventory.Count - 1);
            var decisionProfile = new DoubleTimeSeries<T>(indicesForResults, decisions);
            var cmdtyConsumed = new DoubleTimeSeries<T>(indicesForResults, cmdtyVolumeConsumedArray);

            return new TreeSimulationResults<T>(storageNpv, decisionProfile, cmdtyConsumed);
        }

        public sealed class DecisionSimulator : ITreeDecisionSimulator<T>
        {
            public TreeStorageValuationResults<T> ValuationResults { get; }
            private readonly TreeStorageValuation<T> _storageValuation;

            internal DecisionSimulator(TreeStorageValuationResults<T> valuationResults, TreeStorageValuation<T> storageValuation)
            {
                ValuationResults = valuationResults;
                _storageValuation = storageValuation;
            }

            public TreeSimulationResults<T> SimulateDecisions(TimeSeries<T, int> spotPricePath)
            {
                return _storageValuation.SimulateDecisions(ValuationResults, spotPricePath);
            }

        }

    }
}
