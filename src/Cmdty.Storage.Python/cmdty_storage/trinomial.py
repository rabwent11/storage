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
from cmdty_storage import utils
from pathlib import Path
clr.AddReference(str(Path('cmdty_storage/lib/Cmdty.Storage')))


def trinomial_value(cmdty_storage, val_date, inventory, forward_curve, 
                    spot_volatility, mean_reversion,
                    interest_rates, settlement_rule, 
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


