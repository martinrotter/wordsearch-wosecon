# WoSeCon difficulty-estimator calibration

## Scope

The calibration corpus contains 195 deterministic construction runs covering
76 input configurations. Each run used the real desktop Monte Carlo generator,
including normal-mode secret-message capacity and final-board ambiguity
validation. Runs were capped at 30–180 seconds depending on the experiment.

The measurements are calibration data, not an independent validation set. The
time bands remain deliberately rough because the randomized search has a large
seed-dependent tail and the slowest observations are right-censored by their
timeouts.

## Experiment summary

| Batch | Runs | Succeeded | Timed out |
|---|---:|---:|---:|
| Phase 2A — geometry and scale | 63 | 62 | 1 |
| Phase 2B — content and message | 27 | 24 | 3 |
| Phase 2C — parallelism and mode | 18 | 18 | 0 |
| Phase 3A — message boundary | 27 | 17 | 10 |
| Phase 3B — compatibility and ambiguity | 35 | 32 | 3 |
| Phase 3C — quiz and practical stress | 25 | 20 | 5 |
| **Total** | **195** | **173** | **22** |

## Measured parameter effects

| Parameter | Practical impact | Estimator treatment |
|---|---|---|
| Matrix size | Weak by itself for sparse inputs. A larger board can even make placement easier by providing more choices. | Represented through packing and the number of legal placements, not a blanket matrix-size penalty. |
| Aspect ratio | Negligible while every word still has many legal directions; important when a long word barely fits one dimension. | Minimum legal-placement ratio. |
| Word length | Weak in roomy matrices; becomes important near a dimension limit. | Legal-placement ratio rather than raw length alone. |
| Word count | Mostly cheap at small counts, but increases search depth and exposes a long seed tail. | Base depth term plus a nonlinear word-count/density interaction. |
| Secret-message length in normal mode | Important only because those cells must remain vacant. Runtime rises abruptly once word crossings become mandatory. | Subtracted from placement capacity before packing and required crossings are calculated. |
| Required crossings | The strongest observed capacity predictor, but their difficulty depends heavily on how many actual crossing choices exist. | Absolute crossing demand plus required crossings divided by matching character-position pairs. |
| Character compatibility | Zero compatible pairs correctly identifies structurally impossible dense inputs. Merely counting compatible word pairs is too coarse for runtime. | Compatible pairs remain the impossibility bound; matching character positions drive the difficulty score. |
| Similar/repetitive words | Can produce hundreds of thousands of rejected ambiguous boards even at moderate density. | Normal-mode penalties for short repetitive words and word families sharing nearly the same alphabet. |
| Quiz mode | Ordinary quiz cases were fast. Difficulty came from question-cell capacity and the rule that question cells cannot overlap. | Question cells are included in placement length and packing, with only a small additional quiz penalty. |
| Parallel attempts | Large improvement from one to a few workers, but no reliable benefit from oversubscribing the four-logical-CPU benchmark machine. | Diminishing logarithmic benefit, capped by `Environment.ProcessorCount`. |
| Random seed | Dominant source of variance near a constraint boundary: nominally identical repetitions ranged from milliseconds to a timeout. | Conservative scoring and broad output bands; the result remains a warning, never a guarantee. |

## Secret-message boundary

This experiment used eight five-character words in a 10×10 board. Sixty cells
are enough for all words without crossing, so each additional reserved message
cell above 60 requires at least one word intersection.

| Reserved cells | Required crossings | Successes | Minimum | Median | Maximum |
|---:|---:|---:|---:|---:|---:|
| 61 | 1 | 3/3 | 2 ms | 4 ms | 30 ms |
| 62 | 2 | 3/3 | 1 ms | 3 ms | 12 ms |
| 63 | 3 | 3/3 | 1 ms | 4 ms | 4 ms |
| 64 | 4 | 3/3 | 13 ms | 106 ms | 509 ms |
| 65 | 5 | 3/3 | 171 ms | 2.35 s | 6.09 s |
| 66 | 6 | 1/3 | 62 ms | 90.51 s | 90.53 s |
| 67 | 7 | 1/3 | 103.61 s | 120.51 s | 120.52 s |
| 68 | 8 | 0/3 | 120.51 s | 120.52 s | 120.54 s |
| 69 | 9 | 0/3 | 120.52 s | 120.53 s | 120.55 s |

The boundary is probabilistic rather than sharp. Five required crossings were
still consistently successful, while six crossings produced both a 62 ms win
and two 90-second timeouts.

## Notable stress cases

| Configuration | Measurement | Old estimate | Calibrated estimate |
|---|---|---|---|
| Twelve highly similar words, 10×10 | Two millisecond wins and one 60-second timeout; 357,503 ambiguity rejections in the timed-out run | `FastInSeconds` | `FastUnderMinute` |
| Natural words, 70 reserved cells | Three 180-second timeouts | `SlowFewMinutes` | `SlowerManyMinutes` |
| Repetitive three-letter family | Two 31–60 second wins and one 60-second timeout | `FastInSeconds` | `FastUnderMinute` |
| Known-solvable dense 22-word 11×11 board | Two 180-second timeouts and about 6.1 billion tested placements per run | `SlowerManyMinutes` | `CrazySlowHours` |
| Practical Czech 30-phrase 18×18 project | Both four- and sixteen-worker runs timed out at 180 seconds | `SlowFewMinutes` | `SlowerManyMinutes` |
| Structurally incompatible quiz | 30-second timeout | `LikelyImpossible` | `LikelyImpossible` |

The known dense case demonstrates that existence of a solution does not imply
that the backtracking order can discover it quickly.

## Replay comparison

For a conservative comparison, every configuration was assigned an observed
band from its worst repetition. Timeout observations provide only a lower bound,
so exact agreement at the slow end should not be interpreted as a precise time
prediction.

| Metric across 76 configurations | Old estimator | Calibrated estimator |
|---|---:|---:|
| Exact observed-band agreement | 55 | 61 |
| Underestimates | 5 | 2 |
| Underestimates by more than one band | 2 | 0 |
| Overestimates | 16 | 13 |

The two remaining one-band underestimates are the seed-tail ambiguity cases
listed above. Both were classified `FastUnderMinute`; each had fast or
under-minute successful repetitions plus one timeout at approximately 60
seconds.

## Reproduction

The input manifests are under `test-data/benchmarks`. Raw JSON and CSV outputs
are written to the ignored `artifacts/benchmarks` directory. A batch can be
rerun with:

```powershell
dotnet run --project tools/WordSearchGenerator.Benchmarks/WordSearchGenerator.Benchmarks.csproj -c Release -- test-data/benchmarks/phase-3a-message-boundary.json artifacts/benchmarks
```

