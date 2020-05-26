## Overview
A collection of models for the valuation and optimisation of commodity storage, either virtual or physical. The models can be used for any commodity, although are most suitable for natural gas storage valuation and optimisation.

Calculations take into account many of the complex features of physical storage including:
* Inventory dependent injection and withdrawal rates, otherwise known as ratchets. For physical storage it is often the case that maximum withdrawal rates will increase, and injection rates will decrease as the storage inventory increases. For natural gas, this due to the increased pressure within the storage cavern.
* Time dependent injection and withdrawal rates, including the ability to add outages when no injection or withdrawal is allowed.
* Forced injection/withdrawal, as can be enforced by regulatory or physical constraints.
* Commodity consumed on injection/withdrawal, for example where natural gas is consumed by the motors that power injection into storage.
* Time dependent minimum and maximum inventory, necessary if different notional volumes of a storage facility are leased for different consecutive years.
* Optional time and inventory dependent loss of commodity in storage. For example this assumption is necessary for electricity storage which isn't 100% efficient.
* Ability to constrain the storage to be empty at the end of it's life, or specify a value of commodity inventory left in storage.

## Models
### Currently Implemented
Currently the following models are implemented in this repository:
* Intrinsic valuation, i.e. optimal value assuming the commodity price remains static.
* One-factor trinomial tree, with seasonal spot volatility.

Both approaches solve the optimsation problem using backward induction across a discrete inventory grid.

### Planned Implementations
Implemenations using the following techniques are planned in the near future:
* Least-Squares Monte Carlo with a multi-factor price process.
* Rolling Intrinsic.

## Examples
### Creating the Storage Object
The first step is to create an instance of the class CmdtyStorage which
represents the storage facility. Many of the parameters of the CmdtyStorage
initializer in the example below are optional and can also be of type
pandas.Series in order to be time dependent. Better documentation on this
is to follow.

```python
from datetime import date, timedelta
import pandas as pd
from cmdty_storage import CmdtyStorage, intrinsic_value

def create_piecewise_flat_series(data, dt_index, freq):
    period_index = pd.PeriodIndex([pd.Period(dt, freq=freq) for dt in dt_index])
    return pd.Series(data, period_index).resample(freq).fillna('pad')

constraints =   [
                    (date(2019, 8, 28), 
                                [
                                    (0.0, -150.0, 255.2),
                                    (2000.0, -200.0, 175.0),
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

cmdty_storage = CmdtyStorage('D', storage_start, storage_end, constant_injection_cost, constant_withdrawal_cost, constraints, 
                        cmdty_consumed_inject=constant_pcnt_consumed_inject, cmdty_consumed_withdraw=constant_pcnt_consumed_withdraw,
                        inventory_loss=constant_pcnt_inventory_loss, inventory_cost=constant_pcnt_inventory_cost)
```


### Calculation of Intrinsic NPV
The following example shows how to calculate the intrinsic NPV, the 
value assuming that the commodity forward curve is static.


```python
inventory = 650.0
val_date = date(2019, 9, 2)

forward_curve = create_piecewise_flat_series([58.89, 61.41, 59.89, 59.89], 
                          [val_date, date(2019, 9, 12), date(2019, 9, 18), storage_end], freq='D')

flat_interest_rate = 0.03
interest_rate_curve = pd.Series(index = pd.period_range(val_date, storage_end + timedelta(days=50), freq='D'))
interest_rate_curve[:] = flat_interest_rate

twentieth_of_next_month = lambda period: period.asfreq('M').asfreq('D', 'end') + 20
intrinsic_results = intrinsic_value(cmdty_storage, val_date, inventory, forward_curve, 
                settlement_rule=twentieth_of_next_month, interest_rates=interest_rate_curve, 
                                    num_inventory_grid_points=100)

print("Storage NPV")
print("{:,.2f}".format(intrinsic_results.npv) )
print()
print(intrinsic_results.profile.applymap("{0:.2f}".format))
```

Prints the following:
```
Storage NPV
40,419.45

           inventory inject_withdraw_volume cmdty_consumed inventory_loss net_position
2019-09-02    483.10                -166.25           0.01           0.65       166.24
2019-09-03    320.54                -162.08           0.01           0.48       162.06
2019-09-04    320.22                   0.00           0.00           0.32        -0.00
2019-09-05    562.26                 242.36           0.02           0.32      -242.38
2019-09-06    794.35                 232.65           0.02           0.56      -232.68
2019-09-07   1016.90                 223.35           0.02           0.79      -223.37
2019-09-08   1230.31                 214.42           0.02           1.02      -214.44
2019-09-09   1434.94                 205.86           0.02           1.23      -205.89
2019-09-10   1616.69                 183.18           0.02           1.43      -183.20
2019-09-11   1793.91                 178.84           0.02           1.62      -178.85
2019-09-12   1601.67                -190.44           0.02           1.79       190.43
2019-09-13   1411.43                -188.64           0.02           1.60       188.63
2019-09-14   1223.16                -186.86           0.02           1.41       186.85
2019-09-15   1036.83                -185.10           0.02           1.22       185.08
2019-09-16    852.44                -183.35           0.02           1.04       183.34
2019-09-17    669.96                -181.63           0.02           0.85       181.61
2019-09-18    489.51                -179.78           0.02           0.67       179.77
2019-09-19    311.74                -177.28           0.02           0.49       177.27
2019-09-20    136.61                -174.82           0.02           0.31       174.80
2019-09-21      0.00                -136.47           0.01           0.14       136.46
2019-09-22      0.00                   0.00           0.00           0.00        -0.00
2019-09-23      0.00                   0.00           0.00           0.00        -0.00
2019-09-24      0.00                   0.00           0.00           0.00        -0.00
```

### Calculation of NPV With One-Factor Trinomial Tree Model
The following example shows how to calculate the storage NPV using a 
trinomial tree model. This assumes that the commodity spot price follows
a one-factor mean-reverting process and the result includes the extrinsic
option value of the storage.

```python
from cmdty_storage import trinomial_value

# Trinomial Tree parameters
mean_reversion = 14.5
spot_volatility = create_piecewise_flat_series([1.35, 1.13, 1.24, 1.24],
                           [val_date, date(2019, 9, 12), date(2019, 9, 18), storage_end], freq='D')
time_step = 1.0/365.0

trinomial_value = trinomial_value(cmdty_storage, val_date, inventory, forward_curve,
                spot_volatility, mean_reversion, time_step,
                 settlement_rule=twentieth_of_next_month,
                interest_rates=interest_rate_curve, num_inventory_grid_points=100)

print("Storage Trinomial Tree Model NPV")
print("{:,.2f}".format(trinomial_value) )
```

Which prints the following:
```python
Storage Trinomial Tree NPV
42,844.28
```
