# PaleCheck

[![ci](https://github.com/d3osaju/palecheck/actions/workflows/ci.yml/badge.svg)](https://github.com/d3osaju/palecheck/actions/workflows/ci.yml)

**A phone, a ₹1 printed card, and an honest look at eyelid pallor.** Live: **https://palecheck.zetalabs.in**
· One-tap demo: [/check?sample=4](https://palecheck.zetalabs.in/check?sample=4)
· Benchmark Lab: [/benchmark?run=1](https://palecheck.zetalabs.in/benchmark?run=1)
· Technical report: [PDF](https://palecheck.zetalabs.in/palecheck-report.pdf)

Nurses screen for anaemia by pulling down the lower eyelid and judging how pale it is. PaleCheck measures
that colour with a phone camera and cancels the room light using a printed white card. It runs entirely
offline in the browser; photos never leave the device.

While building it we found that the data and the light bulb matter more than any AI model:

| Finding | Number |
|---|---|
| CP-AnemiC (largest public eyelid dataset): patient records vs distinct photographs | **710 records, 308 photographs** |
| Images that are a copy of another record's photograph | **571 (80%)**: 312 pixel-identical + 12 re-encoded + 247 re-cropped or mirrored |
| A memoriser (1-nearest-neighbour) under random 5-fold CV: published → all copies removed | **AUC 0.88 → 0.59** (colour model: 0.55 → 0.68) |
| Honest accuracy on the 131 independent images (nested CV) | **AUC 0.65** (95% CI 0.54–0.73) |
| Warm bulb's shift in eyelid redness vs the healthy-vs-anaemic gap | **3.8×** |
| Photo-first screening of 1,000 Indian girls (59.1% anaemic), cautious cut-off | saves 107 blood tests, **misses 51 anaemic girls** |

So PaleCheck reports a **pallor index** with an explicit *overlap zone*, never a diagnosis; it never replaces the
free Anaemia Mukt Bharat blood test, it gets people to it.

## What's in the app

| Page | What it does |
|---|---|
| **Check** | Photo → card found automatically → one tap on the eyelid (Smart select) → pallor index (averaged over up to 5 photos), your eyelid colour beside the healthy and anaemic averages, where you sit among 131 reference eyes, symptom checklist, free-test referral, read aloud. English, Malayalam, Hindi. |
| **Benchmark Lab** | Cross-validates a memoriser and the colour model on the published images, live, at each cleaning level and split. |
| **Science** | The three-layer audit, protocol v1 → v3, accuracy with confidence intervals, lighting, and a screening calculator (per 1,000 people, any prevalence and cut-off). |
| **Lighting Lab** | Photograph the same target under different lights; see redness before and after card correction. |
| **DupeScope** | Finds identical and near-identical copies, with metadata conflicts, in *any* image dataset, in the browser. |
| **Report** | A technical report generated from the metrics, also as a PDF. |
| **Card / History** | Printable cards with a QR code; results stored only on the device, CSV export. |

## Run it

```bash
dotnet run --project src/PaleCheck.Web --launch-profile http
```

Open http://localhost:5049. Production build: `bash build.sh` (publishes to `dist/` and adds a hashed CSP).
Deployed on Vercel from `main`. Posters, screenshots and the PDF: `tools/render-posters.ps1` (app running).

## Reproduce the research

```bash
python -m pip install -r model/requirements.txt
python model/get_data.py           # downloads CP-AnemiC (8.3 MB, CC BY 4.0), checks SHA-256
python model/audit_duplicates.py   # pixel-identical groups and their conflicting records
python model/train.py              # 3-layer audit, protocol v3, lighting, leakage demo; writes model + metrics into the app
dotnet test                        # C# matches Python to 1e-9; Benchmark Lab reproduces the leakage results
```

CI does all of this on every push and fails if any headline number drifts (`model/check_metrics.py`).

**Audit.** (1) Pixel-identical: MD5 of decoded pixels. (2) Near-identical: difference hash ≤ 6 bits, aspect within 5%,
mean pixel difference ≤ 6/255. (3) Re-cropped: for each image's 8 nearest colour neighbours, slide one cut-out over the
other (FFT cross-correlation, both orientations) and accept an RMS difference ≤ 4 over ≥ 50% overlap. Control: the same
pairs with one image mirrored only ever match true mirror-image copies below the threshold.

**Protocol v3** (deterministic, seed 20261001): exclude all copies → hold out 8 demo images → 70/30 development/lockbox
→ feature set and regularisation by nested CV on development → lockbox evaluated once → nested CV, leave-one-hospital-out,
subgroups, bootstrap CIs. v1 (identical only) and v2 (+ near-identical) are kept in `metrics.json` → `protocol_history`.

## Layout

```
model/                 Python: get_data, audit_duplicates, near_duplicates (incl. re-crop detector), features, train, check_metrics
model/out/             model.json + metrics.json (also copied into the app)
data/clean/            Manifests: 139 reliable images (with split) and 571 excluded images grouped by photograph
src/PaleCheck.Core/    C#: colour maths, features, model, quality checks, Benchmark Lab engine, screening maths
src/PaleCheck.Web/     Blazor WebAssembly PWA
tests/                 xUnit: Python parity, Benchmark reproduction on real data, quality gates, screening, colour
tools/                 CSP injection for the published site, poster/screenshot/PDF renderer
docs/                  Devpost write-up, video script, protocols, technical report PDF, gallery images
```

## Limitations

- Only 139 independent photographs survive the audit; every AUC has a wide interval.
- The re-crop search compares each image with its nearest colour neighbours only; some copies may remain.
- Reference eyes are children aged 6–59 months in Ghana (anaemia: Hb < 11 g/dL), photographed without a colour reference.
- Lighting results are simulated or synthetic; real-camera results come from the Lighting Lab protocol. Not a medical device.

## Credits

- Data: Appiahene P., Chaturvedi K., Asare J.W., Donkoh E.T., Prasad M. (2023). *CP-AnemiC: A conjunctival pallor dataset
  and benchmark for anemia detection in children.* Medicine in Novel Technology and Devices. Mendeley Data,
  DOI [10.17632/m53vz6b7fx.1](https://data.mendeley.com/datasets/m53vz6b7fx/1), CC BY 4.0. Changes: copies excluded.
- Prevalence: NFHS-5 (2019–21), via Scott et al., *Maternal & Child Nutrition* (2022).
- Planckian locus fit: Kang et al. (2002), *J. Korean Physical Society*.
- Code and text were developed with the help of an AI coding assistant; every number is generated by the scripts above.
