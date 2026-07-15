> test info



test suite: `nbomber_default_test_suite_name`

test name: `Inventory API Performance Tests`

session id: `2026-07-15_23-18-11_2d99001`

> scenario stats



scenario: `load_test_get_products`

  - ok count: `112865`

  - fail count: `0`

  - all data: `119.799` MB

  - duration: `00:00:40`

load simulations:

  - `ramping_constant`, copies: `50`, during: `00:00:10`

  - `keep_constant`, copies: `50`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `112865`, ok = `112865`, RPS = `2821.62`|
|latency (ms)|min = `2.09`, mean = `15.12`, max = `473.92`, StdDev = `21.53`|
|latency percentile (ms)|p50 = `10.16`, p75 = `14.17`, p95 = `35.17`, p99 = `111.23`|
|data transfer (KB)|min = `1.087`, mean = `1.087`, max = `1.087`, all = `119.799` MB|


> status codes for scenario: `load_test_get_products`



|status code|count|message|
|---|---|---|
|OK|112865||


> scenario stats



scenario: `stress_test_get_products`

  - ok count: `48100`

  - fail count: `0`

  - all data: `51.055` MB

  - duration: `00:00:15`

load simulations:

  - `ramping_constant`, copies: `500`, during: `00:00:15`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `48100`, ok = `48100`, RPS = `3206.67`|
|latency (ms)|min = `1.79`, mean = `69.69`, max = `611.78`, StdDev = `65.7`|
|latency percentile (ms)|p50 = `52.67`, p75 = `94.34`, p95 = `193.66`, p99 = `335.87`|
|data transfer (KB)|min = `1.087`, mean = `1.087`, max = `1.087`, all = `51.055` MB|


> status codes for scenario: `stress_test_get_products`



|status code|count|message|
|---|---|---|
|OK|48100||


> scenario stats



scenario: `random_spike_get_products`

  - ok count: `3178`

  - fail count: `0`

  - all data: `3.373` MB

  - duration: `00:00:30`

load simulations:

  - `inject_random`, minRate: `10`, maxRate: `200`, interval: `00:00:01`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `3178`, ok = `3178`, RPS = `105.93`|
|latency (ms)|min = `2.11`, mean = `48.41`, max = `662.9`, StdDev = `76.77`|
|latency percentile (ms)|p50 = `16.78`, p75 = `44.48`, p95 = `191.62`, p99 = `392.7`|
|data transfer (KB)|min = `1.087`, mean = `1.087`, max = `1.087`, all = `3.373` MB|


> status codes for scenario: `random_spike_get_products`



|status code|count|message|
|---|---|---|
|OK|3178||


