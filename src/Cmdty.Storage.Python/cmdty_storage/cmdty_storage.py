# Copyright(c) 2019 Jake Fowler
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
from System import DateTime, Func, Double
from System.Collections.Generic import List
from pathlib import Path
clr.AddReference(str(Path("cmdty_storage/lib/Cmdty.TimePeriodValueTypes")))
from Cmdty.TimePeriodValueTypes import QuarterHour, HalfHour, Hour, Day, Month, Quarter, TimePeriodFactory

clr.AddReference(str(Path('cmdty_storage/lib/Cmdty.Storage')))
from Cmdty.Storage import IBuilder, IAddInjectWithdrawConstraints, InjectWithdrawRangeByInventoryAndPeriod, InjectWithdrawRangeByInventory, InjectWithdrawRange, CmdtyStorageBuilderExtensions, IAddInjectionCost, IAddCmdtyConsumedOnInject, IAddWithdrawalCost, IAddCmdtyConsumedOnWithdraw, IAddCmdtyInventoryLoss, IAddCmdtyInventoryCost, IAddTerminalStorageState, IBuildCmdtyStorage
from Cmdty.Storage import CmdtyStorage as NetCmdtyStorage

from collections import namedtuple
from datetime import datetime
import pandas as pd

ValuationResults = namedtuple('ValuationResults', 'npv, decision_profile')

InjectWithdrawByInventory = namedtuple('InjectWithdrawByInventory', 'inventory, min_rate, max_rate')

InjectWithdrawByInventoryAndPeriod = namedtuple('InjectWithdrawByInventoryPeriod', 'period, rates_by_inventory')

def _from_datetime_like(datetime_like, time_period_type):
    """ Converts either a pandas Period, datetime or date to a .NET Time Period"""

    if (hasattr(datetime_like, 'hour')):
        time_args = (datetime_like.hour, datetime_like.minute, datetime_like.second)
    else:
        time_args = (0, 0, 0)

    date_time = DateTime(datetime_like.year, datetime_like.month, datetime_like.day, *time_args)
    return TimePeriodFactory.FromDateTime[time_period_type](date_time)


def _net_datetime_to_py_datetime(net_datetime):
    return datetime(net_datetime.Year, net_datetime.Month, net_datetime.Day, net_datetime.Hour, net_datetime.Minute, net_datetime.Second, net_datetime.Millisecond * 1000)


def _net_time_period_to_pandas_period(net_time_period, freq):
    start_datetime = _net_datetime_to_py_datetime(net_time_period.Start)
    return pd.Period(start_datetime, freq=freq)


FREQ_TO_PERIOD_TYPE = {
        "15min" : QuarterHour,
        "30min" : HalfHour,
        "H" : Hour,
        "D" : Day,
        "M" : Month,
        "Q" : Quarter
    }
""" dict of str: .NET time period type.
Each item describes an allowable granularity of curves constructed, as specified by the 
freq parameter in the curves public methods.

The keys represent the pandas Offset Alias which describe the granularity, and will generally be used
    as the freq of the pandas Series objects returned by the curve construction methods.
The values are the associated .NET time period types used in behind-the-scenes calculations.
"""


class CmdtyStorage:

    def __init__(self, freq, storage_start, storage_end, constraints,
                   injection_cost, withdrawal_cost, 
                   cmdty_consumed_inject=None, cmdty_consumed_withdraw=None,
                   terminal_storage_npv=None,
                   inventory_loss=None, inventory_cost=None):
                 
        if freq not in FREQ_TO_PERIOD_TYPE:
            raise ValueError("freq parameter value of '{}' not supported. The allowable values can be found in the keys of the dict curves.FREQ_TO_PERIOD_TYPE.".format(freq))

        time_period_type = FREQ_TO_PERIOD_TYPE[freq]

        start_period = _from_datetime_like(storage_start, time_period_type)
        end_period = _from_datetime_like(storage_end, time_period_type)

        builder = IBuilder[time_period_type](NetCmdtyStorage[time_period_type].Builder)
    
        builder = builder.WithActiveTimePeriod(start_period, end_period)

        net_constraints = List[InjectWithdrawRangeByInventoryAndPeriod[time_period_type]]()

        for period, rates_by_inventory in constraints:
            net_period = _from_datetime_like(period, time_period_type)
            net_rates_by_inventory = List[InjectWithdrawRangeByInventory]()
            for inventory, min_rate, max_rate in rates_by_inventory:
                net_rates_by_inventory.Add(InjectWithdrawRangeByInventory(inventory, InjectWithdrawRange(min_rate, max_rate)))
            net_constraints.Add(InjectWithdrawRangeByInventoryAndPeriod[time_period_type](net_period, net_rates_by_inventory))
    
        builder = IAddInjectWithdrawConstraints[time_period_type](builder)

        CmdtyStorageBuilderExtensions.WithTimeAndInventoryVaryingInjectWithdrawRatesPiecewiseLinear[time_period_type](builder, net_constraints)

        first_day_func = Func[time_period_type, Day](lambda dt: dt.First[Day]())

        IAddInjectionCost[time_period_type](builder).WithPerUnitInjectionCost(injection_cost, first_day_func)
    
        builder = IAddCmdtyConsumedOnInject[time_period_type](builder)

        if cmdty_consumed_inject is not None:
            builder.WithFixedPercentCmdtyConsumedOnInject(cmdty_consumed_inject)
        else:
            builder.WithNoCmdtyConsumedOnInject()

        IAddWithdrawalCost[time_period_type](builder).WithPerUnitWithdrawalCost(withdrawal_cost, first_day_func)

        builder = IAddCmdtyConsumedOnWithdraw[time_period_type](builder)

        if cmdty_consumed_withdraw is not None:
            builder.WithFixedPercentCmdtyConsumedOnWithdraw(cmdty_consumed_withdraw)
        else:
            builder.WithNoCmdtyConsumedOnWithdraw()
        
        builder = IAddCmdtyInventoryLoss[time_period_type](builder)
        if inventory_loss is not None:
            # TODO add unit test for this block executing
            # TODO test if inventory_loss is function and handle
            builder.WithFixedPercentCmdtyInventoryLoss(inventory_loss)
        else:
            builder.WithNoCmdtyInventoryLoss()

        builder = IAddCmdtyInventoryCost[time_period_type](builder)
        if inventory_cost is not None:
            # TODO handle if inventory_cost is function
            builder.WithFixedPerUnitCost(inventory_cost)
        else:
            builder.WithNoCmdtyInventoryCost()

        builder = IAddTerminalStorageState[time_period_type](builder)
        
        if terminal_storage_npv is None:
            builder.MustBeEmptyAtEnd()
        else:
            builder.WithTerminalInventoryNpv(Func[Double, Double, Double](terminal_storage_npv))

        self._net_storage = IBuildCmdtyStorage[time_period_type](builder).Build()
        self._freq = freq

    def _net_time_period(self, period):
        time_period_type = FREQ_TO_PERIOD_TYPE[self._freq]
        return _from_datetime_like(period, time_period_type)
    
    @property
    def freq(self):
        return self._freq

    @property
    def must_be_empty_at_end(self):
        return self._net_storage.MustBeEmptyAtEnd
    
    @property
    def start_period(self):
        return _net_time_period_to_pandas_period(self._net_storage.StartPeriod, self._freq)

    @property
    def end_period(self):
        return _net_time_period_to_pandas_period(self._net_storage.EndPeriod, self._freq)

    def inject_withdraw_range(self, period, inventory):

        net_time_period = self._net_time_period(period)
        net_inject_withdraw = self._net_storage.GetInjectWithdrawRange(net_time_period, inventory)
        
        return (net_inject_withdraw.MinInjectWithdrawRate, net_inject_withdraw.MaxInjectWithdrawRate)

    def min_inventory(self, period):
        net_time_period = self._net_time_period(period)
        return self._net_storage.MinInventory(net_time_period)

    def max_inventory(self, period):
        net_time_period = self._net_time_period(period)
        return self._net_storage.MaxInventory(net_time_period)

    def injection_cost(self, period, inventory, injected_volume):
        net_time_period = self._net_time_period(period)
        net_inject_costs = self._net_storage.InjectionCost(net_time_period, inventory, injected_volume)
        if net_inject_costs.Length > 0:
            return net_inject_costs[0].Amount
        return 0.0

    def cmdty_consumed_inject(self, period, inventory, injected_volume):
        net_time_period = self._net_time_period(period)
        return self._net_storage.CmdtyVolumeConsumedOnInject(net_time_period, inventory, injected_volume)
    
    def withdrawal_cost(self, period, inventory, withdrawn_volume):
        net_time_period = self._net_time_period(period)
        net_withdrawal_costs = self._net_storage.WithdrawalCost(net_time_period, inventory, withdrawn_volume)
        if net_withdrawal_costs.Length > 0:
            return net_withdrawal_costs[0].Amount
        return 0.0

    def cmdty_consumed_withdraw(self, period, inventory, withdrawn_volume):
        net_time_period = self._net_time_period(period)
        return self._net_storage.CmdtyVolumeConsumedOnWithdraw(net_time_period, inventory, withdrawn_volume)

    def terminal_storage_npv(self, cmdty_price, terminal_inventory):
        return self._net_storage.TerminalStorageNpv(cmdty_price, terminal_inventory)

    def inventory_loss(self, period, inventory):
        net_time_period = self._net_time_period(period)
        return self._net_storage.CmdtyInventoryLoss(net_time_period, inventory)

    def inventory_cost(self, period, inventory):
        net_time_period = self._net_time_period(period)
        net_inventory_cost = self._net_storage.CmdtyInventoryCost(net_time_period, inventory)
        if net_inventory_cost.Length > 0:
            return net_inventory_cost[0].Amount
        return 0.0

