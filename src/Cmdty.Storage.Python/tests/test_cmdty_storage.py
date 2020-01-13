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

class TestCmdtyStorage(unittest.TestCase):

    def test_initializer(self):

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

        storage = CmdtyStorage('D', storage_start, storage_end, constraints, constant_injection_cost,
                                constant_withdrawal_cost, constant_pcnt_consumed_inject, constant_pcnt_consumed_withdraw,
                                terminal_storage_npv=terminal_npv_calc,
                                inventory_loss=constant_pcnt_inventory_loss, inventory_cost=constant_pcnt_inventory_cost)
        
        self.assertEqual(False, storage.must_be_empty_at_end)

        self.assertEqual('D', storage.freq)

        self.assertEqual(pd.Period(storage_start, freq='D'), storage.start_period)

        self.assertEqual(pd.Period(storage_end, freq='D'), storage.end_period)

        # Inventory half way between pillars, so assert against mean of min/max inject/withdraw at the pillars
        min_dec, max_dec = storage.inject_withdraw_range(date(2019, 8, 29), 1000.0)
        self.assertEqual(-175.0, min_dec)
        self.assertEqual((255.2 + 175.0)/2.0, max_dec)

        min_inventory = storage.min_inventory(date(2019, 8, 29))
        self.assertEqual(0.0, min_inventory)

        max_inventory = storage.max_inventory(date(2019, 9, 10))
        self.assertEqual(1800.0, max_inventory)

        injected_volume = 58.74
        injection_cost = storage.injection_cost(pd.Period(date(2019, 9, 25), freq='D'), 485.5, injected_volume)
        self.assertEqual(injected_volume * constant_injection_cost, injection_cost)

        cmdty_consumed_inject = storage.cmdty_consumed_inject(pd.Period(date(2019, 9, 25), freq='D'), 485.5, injected_volume)
        self.assertEqual(injected_volume * constant_pcnt_consumed_inject, cmdty_consumed_inject)

        withdrawn_volume = 12.05
        injection_cost = storage.withdrawal_cost(pd.Period(date(2019, 9, 2), freq='D'), 135.67, withdrawn_volume)
        self.assertEqual(withdrawn_volume * constant_withdrawal_cost, injection_cost)

        cmdty_consumed_withdraw = storage.cmdty_consumed_withdraw(pd.Period(date(2019, 9, 25), freq='D'), 485.5, withdrawn_volume)
        self.assertEqual(withdrawn_volume * constant_pcnt_consumed_withdraw, cmdty_consumed_withdraw)

        terminal_npv = storage.terminal_storage_npv(65.78, 250.0);
        self.assertEqual(terminal_npv_calc(65.78, 250.0), terminal_npv)

        inventory_loss = storage.inventory_loss(date(2019, 9, 2), 250.0)
        self.assertEqual(constant_pcnt_inventory_loss * 250.0, inventory_loss)

        inventory_cost = storage.inventory_cost(date(2019, 9, 2), 250.0)
        self.assertEqual(constant_pcnt_inventory_cost * 250.0, inventory_cost)


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

        cmdty_storage = CmdtyStorage('D', storage_start, storage_end, constraints, constant_injection_cost,
                                constant_withdrawal_cost, constant_pcnt_consumed_inject, constant_pcnt_consumed_withdraw,
                                terminal_storage_npv=terminal_npv_calc,
                                inventory_loss=constant_pcnt_inventory_loss, inventory_cost=constant_pcnt_inventory_cost)

        inventory = 650.0
        val_date = date(2019, 9, 2)

        # TODO helper function for created piecewise flat curve (done in a better way than below)
        forward_curve = pd.Series(data={
            pd.Period(val_date, freq='D') : 58.89,\
            pd.Period(date(2019, 9, 12), freq='D') : 61.41, 
            pd.Period(date(2019, 9, 18), freq='D') : 59.89, 
            pd.Period(storage_end, freq='D') : 59.89, }).resample('D').fillna('pad')
        
        intrinsic_value(cmdty_storage, val_date, inventory, forward_curve, settlement_dates=None, interest_rates=None)


if __name__ == '__main__':
    unittest.main()
