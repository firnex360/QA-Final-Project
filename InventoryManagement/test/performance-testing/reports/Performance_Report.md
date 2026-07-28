> test info



test suite: `nbomber_default_test_suite_name`

test name: `Inventory API Performance Tests`

session id: `2026-07-27_15-28-25_e9a8b0b8`

> scenario stats



scenario: `load_test_get_products`

  - ok count: `337`

  - fail count: `0`

  - all data: `0.893` MB

  - duration: `00:00:40`

load simulations:

  - `ramping_constant`, copies: `50`, during: `00:00:10`

  - `keep_constant`, copies: `50`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `337`, ok = `337`, RPS = `8.42`|
|latency (ms)|min = `170.82`, mean = `9055.29`, max = `85402.99`, StdDev = `25069.88`|
|latency percentile (ms)|p50 = `601.6`, p75 = `829.95`, p95 = `85131.26`, p99 = `85327.87`|
|data transfer (KB)|min = `2.714`, mean = `2.714`, max = `2.714`, all = `0.893` MB|


> status codes for scenario: `load_test_get_products`



|status code|count|message|
|---|---|---|
|OK|337||


> scenario stats



scenario: `stress_test_get_products`

  - ok count: `2150`

  - fail count: `12`

  - all data: `5.725` MB

  - duration: `00:00:15`

load simulations:

  - `ramping_constant`, copies: `500`, during: `00:00:15`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `2162`, ok = `2150`, RPS = `143.33`|
|latency (ms)|min = `120.55`, mean = `8590.08`, max = `85388.56`, StdDev = `24374.19`|
|latency percentile (ms)|p50 = `636.93`, p75 = `852.99`, p95 = `85131.26`, p99 = `85262.34`|
|data transfer (KB)|min = `2.714`, mean = `2.714`, max = `2.714`, all = `5.698` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `2162`, fail = `12`, RPS = `0.8`|
|latency (ms)|min = `84480.97`, mean = `84680.29`, max = `85046.09`, StdDev = `216.67`|
|latency percentile (ms)|p50 = `84541.44`, p75 = `84738.05`, p95 = `85065.73`, p99 = `85065.73`|
|data transfer (KB)|min = `2.271`, mean = `2.271`, max = `2.271`, all = `0.027` MB|


> status codes for scenario: `stress_test_get_products`



|status code|count|message|
|---|---|---|
|OK|2150||
|InternalServerError|12||


> scenario stats



scenario: `random_spike_get_products`

  - ok count: `1265`

  - fail count: `649`

  - all data: `3.353` MB

  - duration: `00:00:30`

load simulations:

  - `inject_random`, minRate: `10`, maxRate: `200`, interval: `00:00:01`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `1914`, ok = `1265`, RPS = `42.17`|
|latency (ms)|min = `231.73`, mean = `2675.34`, max = `85260.38`, StdDev = `4594.86`|
|latency percentile (ms)|p50 = `1362.94`, p75 = `4780.03`, p95 = `6352.9`, p99 = `6582.27`|
|data transfer (KB)|min = `2.714`, mean = `2.714`, max = `2.714`, all = `3.353` MB|


|step|failures stats|
|---|---|
|name|`global information`|
|request count|all = `1914`, fail = `649`, RPS = `21.63`|
|latency (ms)|min = `1.94`, mean = `3.9`, max = `13.75`, StdDev = `1.21`|
|latency percentile (ms)|p50 = `3.59`, p75 = `4.26`, p95 = `6.26`, p99 = `7.9`|


> status codes for scenario: `random_spike_get_products`



|status code|count|message|
|---|---|---|
|OK|1265||
|-101|649|An error occurred while sending the request.|


