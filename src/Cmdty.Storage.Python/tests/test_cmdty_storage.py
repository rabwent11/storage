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

import unittest
from cmdty_storage import CmdtyStorage, InjectWithdrawByInventoryAndPeriod, InjectWithdrawByInventory, intrinsic_value
from datetime import date
import pandas as pd

import clr
from pathlib import Path
clr.AddReference(str(Path("cmdty_storage/lib/Cmdty.TimePeriodValueTypes")))
from Cmdty.TimePeriodValueTypes import Day

def _create_piecewise_flat_series(data, dt_index, freq):
    period_index = pd.PeriodIndex([pd.Period(dt, freq=freq) for dt in dt_index])
    return pd.Series(data, period_index).resample(freq).fillna('pad')


class TestCmdtyStorage(unittest.TestCase):

    _default_freq='D'
    _default_constraints =   [
                            InjectWithdrawByInventoryAndPeriod(date(2019, 8, 28), 
                                        [
                                            InjectWithdrawByInventory(0.0, -150.0, 255.2),
                                            InjectWithdrawByInventory(2000.0, -200.0, 175.0),
                                        ]),
                            (date(2019, 9, 10), 
                                     [
                                         (0.0, -170.5, 235.8),
                                         (700.0, -180.2, 200.77),
                                         (1800.0, -190.5, 174.45),
                                    ])
                ]

    _constant_min_inventory = 2.54;
    _constant_max_inventory = 1234.56
    _constant_max_injection_rate = 65.64
    _constant_max_withdrawal_rate = 107.07

    _series_min_inventory = _create_piecewise_flat_series([2.4, 1.2, 0.0, 0.0], 
                            [date(2019, 8, 28), date(2019, 9, 1), date(2019, 9, 10), date(2019, 9, 25)], 'D')
    _series_max_inventory = _create_piecewise_flat_series([1250.5, 1358.5, 54.5, 54.5], 
                            [date(2019, 8, 28), date(2019, 9, 1), date(2019, 9, 10), date(2019, 9, 25)], 'D')
    _series_max_injection_rate = []
    _series_max_withrawal_rate = []

    _default_storage_start = date(2019, 8, 28)
    _default_storage_end = date(2019, 9, 25)
    _default_injection_cost = 0.015
    _default_cmdty_consumed_inject = 0.0001
    _default_withdrawal_cost = 0.02
    _default_cmdty_consumed_withdraw = 0.000088
    _default_inventory_loss = 0.001;
    _default_inventory_cost = 0.002;

    _default_terminal_npv_calc = lambda price, inventory: price * inventory - 15.4 # Some arbitrary calculation
    
    def _create_storage(cls, freq=_default_freq, storage_start=_default_storage_start, storage_end=_default_storage_end, 
                   constraints=_default_constraints, 
                   min_inventory=None, max_inventory=None, max_injection_rate=None, max_withdrawal_rate=None,
                   injection_cost=_default_injection_cost, 
                   withdrawal_cost=_default_withdrawal_cost, cmdty_consumed_inject=_default_cmdty_consumed_inject, 
                   cmdty_consumed_withdraw=_default_cmdty_consumed_withdraw, terminal_storage_npv=_default_terminal_npv_calc,
                   inventory_loss=_default_inventory_loss, inventory_cost=_default_inventory_cost):

        return CmdtyStorage(freq, storage_start, storage_end, injection_cost,
                                withdrawal_cost, constraints=constraints, 
                                min_inventory=min_inventory, max_inventory=max_inventory, 
                                max_injection_rate=max_injection_rate, max_withdrawal_rate=max_withdrawal_rate,
                                cmdty_consumed_inject=cmdty_consumed_inject, 
                                cmdty_consumed_withdraw=cmdty_consumed_withdraw,
                                terminal_storage_npv=terminal_storage_npv, inventory_loss=inventory_loss, 
                                inventory_cost=inventory_cost)

    def test_start_property(self):
        storage = self._create_storage()
        self.assertEqual(pd.Period(self._default_storage_start, freq='D'), storage.start)
        
    def test_end_property(self):
        storage = self._create_storage()
        self.assertEqual(pd.Period(self._default_storage_end, freq='D'), storage.end)
        
    def test_freq_property(self):
        storage = self._create_storage()
        self.assertEqual(self._default_freq, storage.freq)
        
    def test_empty_at_end_true_when_terminal_storage_npv_none(self):
        storage = self._create_storage(terminal_storage_npv=None)
        self.assertEqual(True, storage.empty_at_end)
        
    def test_empty_at_end_false_when_terminal_storage_npv_not_none(self):
        storage = self._create_storage()
        self.assertEqual(False, storage.empty_at_end)

    def test_terminal_storage_npv_always_zero_when_terminal_storage_npv_none(self):
        storage = self._create_storage(terminal_storage_npv=None)
        for cmdty_price in [0.0, 23.85, 75.9, 100.22]:
            for terminal_inventory in [0.0, 500.58, 1268.65, 1800.0]:
                self.assertEqual(0.0, storage.terminal_storage_npv(cmdty_price, terminal_inventory))

    def test_terminal_storage_npv_evaluates_to_function_specified(self):
        storage = self._create_storage()
        for cmdty_price in [0.0, 23.85, 75.9, 100.22]:
            for terminal_inventory in [0.0, 500.58, 1268.65, 1800.0]:
                self.assertEqual(TestCmdtyStorage._default_terminal_npv_calc(cmdty_price, terminal_inventory), 
                                 storage.terminal_storage_npv(cmdty_price, terminal_inventory))

    def test_inject_withdraw_range_linearly_interpolated(self):
        storage = self._create_storage()
        # Inventory half way between pillars, so assert against mean of min/max inject/withdraw at the pillars
        min_dec, max_dec = storage.inject_withdraw_range(date(2019, 8, 29), 1000.0)
        self.assertEqual(-175.0, min_dec)
        self.assertEqual((255.2 + 175.0)/2.0, max_dec)

    def test_inject_withdraw_range_from_float_init_parameters(self):
        storage = self._create_storage(constraints=None, min_inventory=self._constant_min_inventory,
                        max_inventory=self._constant_max_inventory, max_injection_rate=self._constant_max_injection_rate, 
                        max_withdrawal_rate=self._constant_max_withdrawal_rate)
        
        for inventory in [2.54, 500.58, 1234.56]:
            for dt in [date(2019, 8, 28), date(2019, 9, 1), date(2019, 9, 20)]:
                min_dec, max_dec = storage.inject_withdraw_range(dt, inventory)
                self.assertEqual(-self._constant_max_withdrawal_rate, min_dec)
                self.assertEqual(self._constant_max_injection_rate, max_dec)

    def test_inject_withdraw_range_from_int_init_parameters(self):
        int_max_injection_rate = int(self._constant_max_injection_rate)
        int_max_withdrawal_rate = int(self._constant_max_withdrawal_rate)
        storage = self._create_storage(constraints=None, min_inventory=self._constant_min_inventory,
                        max_inventory=self._constant_max_inventory, max_injection_rate=int_max_injection_rate, 
                        max_withdrawal_rate=int_max_withdrawal_rate)
        
        for inventory in [2.54, 500.58, 1234.56]:
            for dt in [date(2019, 8, 28), date(2019, 9, 1), date(2019, 9, 20)]:
                min_dec, max_dec = storage.inject_withdraw_range(dt, inventory)
                self.assertEqual(-int_max_withdrawal_rate, min_dec)
                self.assertEqual(int_max_injection_rate, max_dec)

    def test_min_inventory_property_from_constraints_table(self):
        storage = self._create_storage()
        self.assertEqual(0.0, storage.min_inventory(date(2019, 8, 29)))
        self.assertEqual(0.0, storage.min_inventory(date(2019, 9, 11)))

    def test_min_inventory_property_from_float_init_param(self):
        storage = self._create_storage(constraints=None, min_inventory=self._constant_min_inventory,
                        max_inventory=self._constant_max_inventory, max_injection_rate=self._constant_max_injection_rate, 
                        max_withdrawal_rate=self._constant_max_withdrawal_rate)
        self.assertEqual(self._constant_min_inventory, storage.min_inventory(date(2019, 8, 29)))
        self.assertEqual(self._constant_min_inventory, storage.min_inventory(date(2019, 9, 11)))
        
    def test_min_inventory_property_from_series_init_param(self):
        storage = self._create_storage(constraints=None, min_inventory=self._series_min_inventory,
                        max_inventory=self._series_max_inventory, max_injection_rate=self._constant_max_injection_rate, 
                        max_withdrawal_rate=self._constant_max_withdrawal_rate)
        self.assertEqual(2.4, storage.min_inventory(date(2019, 8, 29)))
        self.assertEqual(1.2, storage.min_inventory(date(2019, 9, 1)))
        self.assertEqual(0.0, storage.min_inventory(date(2019, 9, 11)))

    def test_max_inventory_property_from_float_init_param(self):
        storage = self._create_storage(constraints=None, min_inventory=self._constant_min_inventory,
                        max_inventory=self._constant_max_inventory, max_injection_rate=self._constant_max_injection_rate, 
                        max_withdrawal_rate=self._constant_max_withdrawal_rate)
        self.assertEqual(self._constant_max_inventory, storage.max_inventory(date(2019, 8, 29)))
        self.assertEqual(self._constant_max_inventory, storage.max_inventory(date(2019, 9, 11)))

    def test_max_inventory_property_from_series_init_param(self):
        storage = self._create_storage(constraints=None, min_inventory=self._series_min_inventory,
                        max_inventory=self._series_max_inventory, max_injection_rate=self._constant_max_injection_rate, 
                        max_withdrawal_rate=self._constant_max_withdrawal_rate)
        self.assertEqual(1250.5, storage.max_inventory(date(2019, 8, 29)))
        self.assertEqual(1358.5, storage.max_inventory(date(2019, 9, 1)))
        self.assertEqual(54.5, storage.max_inventory(date(2019, 9, 11)))
        


    def test_max_inventory_property_from_constraints_table(self):
        storage = self._create_storage()
        self.assertEqual(2000.0, storage.max_inventory(date(2019, 8, 29)))
        self.assertEqual(1800.0, storage.max_inventory(date(2019, 9, 11)))

    def test_injection_cost(self):
        storage = self._create_storage()
        injected_volume = 58.74
        injection_cost = storage.injection_cost(pd.Period(date(2019, 9, 25), freq='D'), 485.5, injected_volume)
        self.assertEqual(injected_volume * self._default_injection_cost, injection_cost)

    def test_cmdty_consumed_inject(self):
        storage = self._create_storage()
        injected_volume = 58.74
        cmdty_consumed_inject = storage.cmdty_consumed_inject(pd.Period(date(2019, 9, 25), freq='D'), 485.5, injected_volume)
        self.assertEqual(injected_volume * self._default_cmdty_consumed_inject, cmdty_consumed_inject)

    def test_withdrawal_cost(self):
        storage = self._create_storage()
        withdrawn_volume = 12.05
        injection_cost = storage.withdrawal_cost(pd.Period(date(2019, 9, 2), freq='D'), 135.67, withdrawn_volume)
        self.assertEqual(withdrawn_volume * self._default_withdrawal_cost, injection_cost)

    def test_cmdty_consumed_withdraw(self):
        storage = self._create_storage()
        withdrawn_volume = 12.05
        cmdty_consumed_withdraw = storage.cmdty_consumed_withdraw(pd.Period(date(2019, 9, 25), freq='D'), 485.5, withdrawn_volume)
        self.assertEqual(withdrawn_volume * self._default_cmdty_consumed_withdraw, cmdty_consumed_withdraw)

    def test_inventory_pcnt_loss(self):
        storage = self._create_storage()
        inventory_loss = storage.inventory_pcnt_loss(date(2019, 9, 2))
        self.assertEqual(self._default_inventory_loss, inventory_loss)

    def test_inventory_cost(self):
        storage = self._create_storage()
        inventory_cost = storage.inventory_cost(date(2019, 9, 2), 250.0)
        self.assertEqual(self._default_inventory_cost * 250.0, inventory_cost)


class TestIntrinsicValue(unittest.TestCase):

    def test_intrinsic_value_runs(self):

        constraints =   [
                            InjectWithdrawByInventoryAndPeriod(date(2019, 8, 28), 
                                        [
                                            InjectWithdrawByInventory(0.0, -150.0, 255.2),
                                            InjectWithdrawByInventory(2000.0, -200.0, 175.0),
                                        ]),
                            (date(2019, 9, 10), 
                                     [
                                         (0.0, -170.5, 235.8),
                                         (700.0, -180.2, 200.77),
                                         (1800.0, -190.5, 174.45),
                                    ])
                ]

        storage_start = date(2019, 8, 28)
        storage_end = date(2019, 9, 25)
        constant_injection_cost = 0.015
        constant_pcnt_consumed_inject = 0.0001
        constant_withdrawal_cost = 0.02
        constant_pcnt_consumed_withdraw = 0.000088
        constant_pcnt_inventory_loss = 0.001;
        constant_pcnt_inventory_cost = 0.002;

        def terminal_npv_calc(price, inventory):
            return price * inventory - 15.4 # Some arbitrary calculation

        cmdty_storage = CmdtyStorage('D', storage_start, storage_end, constant_injection_cost, constant_withdrawal_cost, constraints, 
                                cmdty_consumed_inject=constant_pcnt_consumed_inject, cmdty_consumed_withdraw=constant_pcnt_consumed_withdraw,
                                terminal_storage_npv=terminal_npv_calc,
                                inventory_loss=constant_pcnt_inventory_loss, inventory_cost=constant_pcnt_inventory_cost)

        inventory = 650.0
        val_date = date(2019, 9, 2)

        # TODO helper function for created piecewise flat curve (done in a better way than below)
        forward_curve = pd.Series(data={
            pd.Period(val_date, freq='D') : 58.89,
            pd.Period(date(2019, 9, 12), freq='D') : 61.41, 
            pd.Period(date(2019, 9, 18), freq='D') : 59.89, 
            pd.Period(storage_end, freq='D') : 59.89, }).resample('D').fillna('pad')
        
        # TODO test with proper interest rate curve
        flat_interest_rate = 0.03
        interest_rate_curve = pd.Series(index = pd.period_range(val_date, storage_end, freq='D'))
        interest_rate_curve[:] = flat_interest_rate

        # TODO more realistic settlement rule
        first_day_rule = lambda period: period.First[Day]()
        intrinsic_results = intrinsic_value(cmdty_storage, val_date, inventory, forward_curve, settlement_rule=first_day_rule, 
                        interest_rates=interest_rate_curve, num_inventory_grid_points=100)



if __name__ == '__main__':
    unittest.main()
