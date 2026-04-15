# Momentum Capabilities

This file compresses the full theory matrix into `mm` breakpoint bands that can
be consumed directly by the planner.

| family | subfamily | movement_mode | qualifiers | mm_range | reach |
| --- | --- | --- | --- | --- | --- |
| ceiling | headhitter | sprint | ceil=1.8125 | 0..1 | max_gap=none |
| ceiling | headhitter | sprint | ceil=1.8125 | 2..12 | max_gap=1 |
| ceiling | headhitter | sprint | ceil=2.0 | 0..12 | max_gap=1 |
| ceiling | headhitter | sprint | ceil=2.5 | 0..7 | max_gap=2 |
| ceiling | headhitter | sprint | ceil=2.5 | 8..12 | max_gap=3 |
| ceiling | headhitter | sprint | ceil=3.0 | 0..1 | max_gap=3 |
| ceiling | headhitter | sprint | ceil=3.0 | 2..12 | max_gap=4 |
| ceiling | headhitter | sprint | ceil=4.0 | 0..0 | max_gap=3 |
| ceiling | headhitter | sprint | ceil=4.0 | 1..12 | max_gap=4 |
| linear | ascend | sprint | dy=1.0 | 0..0 | max_gap=2 |
| linear | ascend | sprint | dy=1.0 | 1..12 | max_gap=3 |
| linear | ascend | walk | dy=1.0 | 0..0 | max_gap=1 |
| linear | ascend | walk | dy=1.0 | 1..12 | max_gap=2 |
| linear | descend | sprint | dy=-2.0 | 0..0 | max_gap=4 |
| linear | descend | sprint | dy=-2.0 | 1..12 | max_gap=5 |
| linear | descend | sprint | dy=-1.0 | 0..1 | max_gap=4 |
| linear | descend | sprint | dy=-1.0 | 2..12 | max_gap=5 |
| linear | descend | walk | dy=-2.0 | 0..2 | max_gap=3 |
| linear | descend | walk | dy=-2.0 | 3..12 | max_gap=4 |
| linear | descend | walk | dy=-1.0 | 0..0 | max_gap=2 |
| linear | descend | walk | dy=-1.0 | 1..12 | max_gap=3 |
| linear | flat | sprint | dy=0.0 | 0..0 | max_gap=3 |
| linear | flat | sprint | dy=0.0 | 1..12 | max_gap=4 |
| linear | flat | walk | dy=0.0 | 0..1 | max_gap=2 |
| linear | flat | walk | dy=0.0 | 2..12 | max_gap=3 |
| neo | neo | sprint | - | 0..1 | max_wall_width=3 |
| neo | neo | sprint | - | 2..12 | max_wall_width=4 |
| neo | neo | walk | - | 0..0 | max_wall_width=1 |
| neo | neo | walk | - | 1..5 | max_wall_width=2 |
| neo | neo | walk | - | 6..12 | max_wall_width=3 |
| sidewall | ascend | sprint | dy=1.0, wo=0 | 0..0 | max_gap=2 |
| sidewall | ascend | sprint | dy=1.0, wo=0 | 1..12 | max_gap=3 |
| sidewall | ascend | sprint | dy=1.0, wo=1 | 0..0 | max_gap=2 |
| sidewall | ascend | sprint | dy=1.0, wo=1 | 1..12 | max_gap=3 |
| sidewall | ascend | walk | dy=1.0, wo=0 | 0..0 | max_gap=1 |
| sidewall | ascend | walk | dy=1.0, wo=0 | 1..12 | max_gap=2 |
| sidewall | ascend | walk | dy=1.0, wo=1 | 0..0 | max_gap=1 |
| sidewall | ascend | walk | dy=1.0, wo=1 | 1..12 | max_gap=2 |
| sidewall | descend | sprint | dy=-2.0, wo=0 | 0..0 | max_gap=4 |
| sidewall | descend | sprint | dy=-2.0, wo=0 | 1..12 | max_gap=5 |
| sidewall | descend | sprint | dy=-2.0, wo=1 | 0..0 | max_gap=4 |
| sidewall | descend | sprint | dy=-2.0, wo=1 | 1..12 | max_gap=5 |
| sidewall | descend | sprint | dy=-1.0, wo=0 | 0..1 | max_gap=4 |
| sidewall | descend | sprint | dy=-1.0, wo=0 | 2..12 | max_gap=5 |
| sidewall | descend | sprint | dy=-1.0, wo=1 | 0..1 | max_gap=4 |
| sidewall | descend | sprint | dy=-1.0, wo=1 | 2..12 | max_gap=5 |
| sidewall | descend | walk | dy=-2.0, wo=0 | 0..0 | max_gap=2 |
| sidewall | descend | walk | dy=-2.0, wo=0 | 1..2 | max_gap=3 |
| sidewall | descend | walk | dy=-2.0, wo=0 | 3..12 | max_gap=4 |
| sidewall | descend | walk | dy=-2.0, wo=1 | 0..0 | max_gap=2 |
| sidewall | descend | walk | dy=-2.0, wo=1 | 1..2 | max_gap=3 |
| sidewall | descend | walk | dy=-2.0, wo=1 | 3..12 | max_gap=4 |
| sidewall | descend | walk | dy=-1.0, wo=0 | 0..0 | max_gap=2 |
| sidewall | descend | walk | dy=-1.0, wo=0 | 1..12 | max_gap=3 |
| sidewall | descend | walk | dy=-1.0, wo=1 | 0..0 | max_gap=2 |
| sidewall | descend | walk | dy=-1.0, wo=1 | 1..12 | max_gap=3 |
| sidewall | flat | sprint | dy=0.0, wo=0 | 0..0 | max_gap=3 |
| sidewall | flat | sprint | dy=0.0, wo=0 | 1..12 | max_gap=4 |
| sidewall | flat | sprint | dy=0.0, wo=1 | 0..0 | max_gap=3 |
| sidewall | flat | sprint | dy=0.0, wo=1 | 1..12 | max_gap=4 |
| sidewall | flat | walk | dy=0.0, wo=0 | 0..1 | max_gap=2 |
| sidewall | flat | walk | dy=0.0, wo=0 | 2..12 | max_gap=3 |
| sidewall | flat | walk | dy=0.0, wo=1 | 0..1 | max_gap=2 |
| sidewall | flat | walk | dy=0.0, wo=1 | 2..12 | max_gap=3 |
