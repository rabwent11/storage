# Copyright(c) 2020 Jake Fowler
#
# Permission is hereby granted, free of charge, to any person
# obtaining a copy of this software and associated documentation
# files (the "Software"), to deal in the Software without
# restriction, including without limitation the rights to use, 
# copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the
# Software is furnished to do so, subject to the following
# conditions:
#
# The above copyright notice and this permission notice shall be
# included in all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
# EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
# NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
# HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
# WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
# FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
# OTHER DEALINGS IN THE SOFTWARE.

import clr
import System as dotnet
from cmdty_storage import utils, CmdtyStorage
from pathlib import Path
clr.AddReference(str(Path('cmdty_storage/lib/Cmdty.Storage')))
import Cmdty.Storage as net_cs


def trinomial_value(cmdty_storage: CmdtyStorage, val_date, inventory, forward_curve,
                    spot_volatility, mean_reversion, time_step, interest_rates, settlement_rule,
                    num_inventory_grid_points=100, numerical_tolerance=1E-12):
    """
    Calculates the value of commodity storage using a one-factor trinomial tree.

    Args:
        settlement_rule (callable): Mapping function from pandas.Period type to the date on which the cmdty delivered in
            this period is settled. The pandas.Period parameter will have freq equal to the cmdty_storage parameter's freq property.
    """
    if cmdty_storage.freq != forward_curve.index.freqstr:
        raise ValueError("cmdty_storage and forward_curve have different frequencies.")
    if cmdty_storage.freq != spot_volatility.index.freqstr:
        raise ValueError("cmdty_storage and spot_volatility have different frequencies.")
    time_period_type = utils.FREQ_TO_PERIOD_TYPE[cmdty_storage.freq]

    trinomial_calc = net_cs.TreeStorageValuation[time_period_type].ForStorage(cmdty_storage.net_storage)
    net_cs.ITreeAddStartingInventory[time_period_type](trinomial_calc).WithStartingInventory(inventory)

    current_period = utils.from_datetime_like(val_date, time_period_type)
    net_cs.ITreeAddCurrentPeriod[time_period_type](trinomial_calc).ForCurrentPeriod(current_period)

    net_forward_curve = utils.series_to_double_time_series(forward_curve, time_period_type)
    net_cs.ITreeAddForwardCurve[time_period_type](trinomial_calc).WithForwardCurve(net_forward_curve)

    net_spot_volatility = utils.series_to_double_time_series(spot_volatility, time_period_type)
    net_cs.TreeStorageValuationExtensions.WithOneFactorTrinomialTree[time_period_type](
                        trinomial_calc, net_spot_volatility, mean_reversion, time_step)

    net_settlement_rule = utils.wrap_settle_for_dotnet(settlement_rule, cmdty_storage.freq)
    net_cs.ITreeAddCmdtySettlementRule[time_period_type](trinomial_calc).WithCmdtySettlementRule(net_settlement_rule)

    interest_rate_time_series = utils.series_to_double_time_series(interest_rates, utils.FREQ_TO_PERIOD_TYPE['D'])
    net_cs.TreeStorageValuationExtensions.WithAct365ContinuouslyCompoundedInterestRateCurve[time_period_type](
                                    trinomial_calc, interest_rate_time_series)

    net_cs.TreeStorageValuationExtensions.WithFixedNumberOfPointsOnGlobalInventoryRange[time_period_type](
                                    trinomial_calc, num_inventory_grid_points)
    net_cs.TreeStorageValuationExtensions.WithLinearInventorySpaceInterpolation[time_period_type](trinomial_calc)
    net_cs.ITreeAddNumericalTolerance[time_period_type](trinomial_calc).WithNumericalTolerance(numerical_tolerance)
    npv = net_cs.ITreeCalculate[time_period_type](trinomial_calc).Calculate()
    return npv
